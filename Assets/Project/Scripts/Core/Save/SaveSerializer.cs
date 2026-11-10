using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Kitchen.Core.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kitchen.Core.Save
{
	/// <summary>
	/// <see cref="SaveData"/> ↔ 存檔 JSON（技術文件 4.4、5.7）。key 為 snake_case。
	/// <para>
	/// 做法：JSON 先讀成 <see cref="JObject"/> → 依 version 執行 migration → 綁定內部 DTO → 驗證後轉為 Core 資料
	/// （<see cref="DishRecord"/> / <see cref="Recipe"/> 等不可變類別經由其建構子重建，沿用建構子的驗證）。
	/// 使用自有的 <see cref="JsonSerializer"/>，不受全域 <see cref="JsonConvert.DefaultSettings"/> 影響；數值格式與文化設定無關。
	/// </para>
	/// <para>
	/// 對稱性：<see cref="Serialize"/> 與 <see cref="Deserialize"/> 共用同一組驗證，載入會拒絕的狀態在存檔時就拒絕（拋 <see cref="SaveException"/>），
	/// 避免記憶體中的髒資料把好存檔覆寫成無法讀取的檔案。
	/// </para>
	/// <para>
	/// 缺欄位：頂層缺少（或為 null）的欄位只補結構性空值（0 / false / null / "" / 空集合；缺 version 視為第 1 版），
	/// 不套用遊戲起始值。由 Runtime 的 SaveSystem 在載入後依 GameConfig 補預設：
	/// fridge_capacity &lt;= 0 → 套用 GameConfig 的初始容量；last_active == 0 → 不結算離線進度。
	/// records 內的每筆紀錄則必須欄位齊全（見 DishRecordDto 的 Required），避免損毀紀錄被默默讀成失敗料理。
	/// </para>
	/// <para>
	/// 已知限制（尚未檢查，留給遊戲層或日後補上）：shelf 索引是否落在 records 範圍內、active_recipe 是否落在 recipes 範圍內、
	/// fridge_inventory / upgrades / stats 的負值、rank 字串是否為合法 Rank。
	/// </para>
	/// </summary>
	public static class SaveSerializer
	{
		/// <summary>目前的存檔 schema 版本（= <see cref="SaveData.CurrentVersion"/>）。schema 變動時在 <see cref="TryMigrateStep"/> 補上轉換。</summary>
		public const int CurrentVersion = SaveData.CurrentVersion;

		// Public
		/// <summary>序列化存檔。資料含載入時會被拒絕的狀態（null 元素、空白 id、非法路線等）時拋出 <see cref="SaveException"/>。</summary>
		public static string Serialize(SaveData data)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			SaveDto dto = ToDto(data);
			JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);

			var builder = new StringBuilder();
			using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
			using (var jsonWriter = new JsonTextWriter(stringWriter))
			{
				serializer.Serialize(jsonWriter, dto);
			}

			return builder.ToString();
		}

		/// <summary>
		/// 反序列化存檔。缺少的頂層欄位補結構性空值（見類別說明）。
		/// 任何失敗（非法 JSON、內容不合法、版本高於 <see cref="CurrentVersion"/>、數值溢位等）一律拋出 <see cref="SaveException"/>。
		/// </summary>
		public static SaveData Deserialize(string json)
		{
			if (json != null && json.Length > 0 && json[0] == Utf8Bom)
			{
				json = json.Substring(1);
			}

			if (string.IsNullOrWhiteSpace(json))
			{
				throw new SaveException($"{Context}: JSON text is null or empty.");
			}

			JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);

			// 第一階段：文字 → JSON 樹。此階段的失敗代表 JSON 本身格式錯誤
			JToken token;
			try
			{
				using (var stringReader = new StringReader(json))
				using (var jsonReader = new JsonTextReader(stringReader))
				{
					// 不把看起來像日期的字串（例：暱稱）轉成 DateTime
					jsonReader.DateParseHandling = DateParseHandling.None;
					token = serializer.Deserialize<JToken>(jsonReader);
				}
			}
			catch (JsonException ex)
			{
				throw new SaveException($"{Context}: invalid JSON. {ex.Message}", ex);
			}

			// 第二階段：JSON 樹 → 存檔模型。此階段的失敗代表 JSON 格式正確但內容不合法（缺必填欄位、型別不符、溢位等）
			try
			{
				if (!(token is JObject root))
				{
					throw new SaveException($"{Context}: top-level value must be a JSON object.");
				}

				int version = ReadVersion(root);
				Migrate(root, version);
				SaveDto dto = root.ToObject<SaveDto>(serializer);
				if (dto == null)
				{
					throw new SaveException($"{Context}: top-level value must be a JSON object.");
				}

				return ToSaveData(dto);
			}
			catch (Exception ex) when (IsDataError(ex))
			{
				throw new SaveException($"{Context}: could not be read. {ex.Message}", ex);
			}
		}

		/// <summary>
		/// 由存檔內容造成的例外（包成 <see cref="SaveException"/>）。程式錯誤（NullReferenceException 等）
		/// 與執行環境錯誤（OutOfMemoryException 等）不在此列，照原樣拋出以免被當成壞存檔掩蓋。
		/// </summary>
		private static bool IsDataError(Exception ex)
		{
			return ex is JsonException
				|| ex is OverflowException
				|| ex is InvalidCastException
				|| ex is FormatException
				|| ex is ArgumentException;
		}

		// Private
		private const string Context = "Save data";
		private const char Utf8Bom = (char)0xFEFF;

		/// <summary>第一版 schema；缺 version 欄位的存檔視為此版。</summary>
		private const int FirstVersion = 1;

		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			MissingMemberHandling = MissingMemberHandling.Ignore,
			NullValueHandling = NullValueHandling.Include,
			DateParseHandling = DateParseHandling.None,
			Culture = CultureInfo.InvariantCulture,
			Formatting = Formatting.Indented,
			// 自有 serializer 需明確開啟尾端多餘內容檢查
			CheckAdditionalContent = true,
			// 不解讀 $id / $ref / $type 等 metadata，讓以 $ 開頭的字典 key（例：食材 id）原樣往返
			MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
		};

		private static int ReadVersion(JObject root)
		{
			JToken token = root["version"];
			if (token == null || token.Type == JTokenType.Null)
			{
				return FirstVersion;
			}

			if (token.Type != JTokenType.Integer)
			{
				throw new SaveException($"{Context}: field 'version' must be an integer (got {token.Type}).");
			}

			long version;
			try
			{
				version = (long)token;
			}
			catch (Exception ex) when (ex is OverflowException || ex is InvalidCastException || ex is FormatException)
			{
				throw new SaveException($"{Context}: field 'version' is out of range.", ex);
			}

			if (version > CurrentVersion)
			{
				throw new SaveException(
					$"{Context}: version {version} is newer than the supported version {CurrentVersion}. " +
					"This save was written by a newer build of the game and cannot be loaded.");
			}
			if (version < FirstVersion)
			{
				throw new SaveException($"{Context}: version {version} is invalid (must be between {FirstVersion} and {CurrentVersion}).");
			}

			return (int)version;
		}

		/// <summary>Migration 入口：從 <paramref name="fromVersion"/> 逐版升到 <see cref="CurrentVersion"/>，直接改寫 JSON 樹。</summary>
		private static void Migrate(JObject root, int fromVersion)
		{
			int version = fromVersion;
			while (version < CurrentVersion)
			{
				if (!TryMigrateStep(root, version))
				{
					throw new SaveException($"{Context}: no migration defined from version {version} to {version + 1}.");
				}
				version++;
				root["version"] = version;
			}
		}

		/// <summary>單步 migration：version → version + 1。v1 為第一版，尚無轉換。</summary>
		private static bool TryMigrateStep(JObject root, int version)
		{
			switch (version)
			{
				// 新版本時加入，例：case 1: MigrateV1ToV2(root); return true;
				default:
					return false;
			}
		}

		// ---- 共用驗證（載入與存檔兩個方向都使用）----

		private static SaveException NullEntry(string field, int index)
		{
			return new SaveException($"{Context}: field '{field}[{index}]' must not be null.");
		}

		/// <summary>食材 id 清單：元素不得為 null 或空白。</summary>
		private static void ValidateIdList(IList<string> ids, string field)
		{
			for (int i = 0; i < ids.Count; i++)
			{
				if (string.IsNullOrWhiteSpace(ids[i]))
				{
					throw new SaveException($"{Context}: field '{field}[{i}]' must not be null or empty.");
				}
			}
		}

		/// <summary>進度值必須是有限數（NaN / Infinity 無法可靠往返）。</summary>
		private static void ValidateProgress(float value, string field)
		{
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				throw new SaveException(FormattableString.Invariant($"{Context}: field '{field}' must be a finite number (got {value})."));
			}
		}

		/// <summary>圖鑑一道菜：菜 id 不得空白、內層不得為 null、路線須為合法 <see cref="DishRoutes"/>（與 DishRecord 一致）、Rank 不得空白。</summary>
		private static void ValidateCompendiumEntry(string dishId, IDictionary<string, string> routes)
		{
			if (string.IsNullOrWhiteSpace(dishId))
			{
				throw new SaveException($"{Context}: field 'compendium' has a null or empty dish id.");
			}
			if (routes == null)
			{
				throw new SaveException($"{Context}: field 'compendium.{dishId}' must be an object, not null.");
			}

			foreach (KeyValuePair<string, string> route in routes)
			{
				if (!DishRoutes.IsValid(route.Key))
				{
					throw new SaveException($"{Context}: field 'compendium.{dishId}' has unknown route '{route.Key}'.");
				}
				if (string.IsNullOrWhiteSpace(route.Value))
				{
					throw new SaveException($"{Context}: field 'compendium.{dishId}.{route.Key}' must be a non-empty rank string.");
				}
			}
		}

		// ---- DTO → SaveData ----

		private static SaveData ToSaveData(SaveDto dto)
		{
			var data = new SaveData
			{
				LastActive = dto.LastActive ?? 0L,
				Nickname = dto.Nickname ?? string.Empty,
				Gold = dto.Gold ?? 0L,
				FridgeCapacity = dto.FridgeCapacity ?? 0,
				ActiveRecipe = dto.ActiveRecipe,
				AutoCookEnabled = dto.AutoCookEnabled ?? false,
			};

			CopyIntMap(dto.FridgeInventory, data.FridgeInventory);
			CopyIntMap(dto.Upgrades, data.Upgrades);

			if (dto.FridgeSlots != null)
			{
				for (int i = 0; i < dto.FridgeSlots.Count; i++)
				{
					FridgeSlotDto slot = dto.FridgeSlots[i];
					if (slot == null) throw NullEntry("fridge_slots", i);
					data.FridgeSlots.Add(Build("fridge_slots", i, () => new FridgeSlotState(slot.Type, slot.Value)));
				}
			}

			if (dto.Records != null)
			{
				for (int i = 0; i < dto.Records.Count; i++)
				{
					DishRecordDto r = dto.Records[i];
					if (r == null) throw NullEntry("records", i);
					data.Records.Add(Build("records", i, () => new DishRecord(
						r.DishId,
						r.Route,
						r.Rank,
						r.Tier,
						r.Affix == null ? null : new DishAffix(r.Affix.Type, r.Affix.Value),
						r.Maker,
						r.Timestamp,
						r.Score)));
				}
			}

			if (dto.Compendium != null)
			{
				foreach (KeyValuePair<string, Dictionary<string, string>> dish in dto.Compendium)
				{
					ValidateCompendiumEntry(dish.Key, dish.Value);
					data.Compendium.Add(dish.Key, new Dictionary<string, string>(dish.Value, StringComparer.Ordinal));
				}
			}

			if (dto.Shelf != null)
			{
				data.Shelf.AddRange(dto.Shelf);
			}

			if (dto.Recipes != null)
			{
				for (int i = 0; i < dto.Recipes.Count; i++)
				{
					RecipeDto recipe = dto.Recipes[i];
					if (recipe == null) throw NullEntry("recipes", i);
					data.Recipes.Add(Build("recipes", i, () => new Recipe(recipe.Name, recipe.Items)));
				}
			}

			if (dto.ChainState != null)
			{
				ChainStateDto c = dto.ChainState;
				if (c.OrderRemaining != null)
				{
					ValidateIdList(c.OrderRemaining, OrderRemainingField);
					data.ChainState.OrderRemaining.AddRange(c.OrderRemaining);
				}
				if (c.PotContents != null)
				{
					ValidateIdList(c.PotContents, PotContentsField);
					data.ChainState.PotContents.AddRange(c.PotContents);
				}
				data.ChainState.BoardProgress = c.BoardProgress ?? 0f;
				data.ChainState.CookProgress = c.CookProgress ?? 0f;
				ValidateProgress(data.ChainState.BoardProgress, BoardProgressField);
				ValidateProgress(data.ChainState.CookProgress, CookProgressField);
			}

			if (dto.Stats != null)
			{
				data.Stats.TotalDishes = dto.Stats.TotalDishes ?? 0;
				data.Stats.SsCount = dto.Stats.SsCount ?? 0;
			}

			return data;
		}

		private const string OrderRemainingField = "chain_state.order_remaining";
		private const string PotContentsField = "chain_state.pot_contents";
		private const string BoardProgressField = "chain_state.board_progress";
		private const string CookProgressField = "chain_state.cook_progress";

		private static void CopyIntMap(Dictionary<string, int> source, Dictionary<string, int> target)
		{
			if (source == null) return;
			foreach (KeyValuePair<string, int> pair in source)
			{
				target.Add(pair.Key, pair.Value);
			}
		}

		/// <summary>呼叫不可變類別的建構子；其驗證失敗（ArgumentException）時包成 <see cref="SaveException"/>。</summary>
		private static T Build<T>(string field, int index, Func<T> factory)
		{
			try
			{
				return factory();
			}
			catch (ArgumentException ex)
			{
				throw new SaveException($"{Context}: field '{field}[{index}]' is invalid. {ex.Message}", ex);
			}
		}

		// ---- SaveData → DTO ----

		private static SaveDto ToDto(SaveData data)
		{
			ValidateIdList(data.ChainState.OrderRemaining, OrderRemainingField);
			ValidateIdList(data.ChainState.PotContents, PotContentsField);
			ValidateProgress(data.ChainState.BoardProgress, BoardProgressField);
			ValidateProgress(data.ChainState.CookProgress, CookProgressField);

			var dto = new SaveDto
			{
				Version = data.Version,
				LastActive = data.LastActive,
				Nickname = data.Nickname ?? string.Empty,
				Gold = data.Gold,
				FridgeInventory = new Dictionary<string, int>(data.FridgeInventory, StringComparer.Ordinal),
				FridgeCapacity = data.FridgeCapacity,
				Upgrades = new Dictionary<string, int>(data.Upgrades, StringComparer.Ordinal),
				FridgeSlots = new List<FridgeSlotDto>(data.FridgeSlots.Count),
				Records = new List<DishRecordDto>(data.Records.Count),
				Compendium = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal),
				Shelf = new List<int?>(data.Shelf),
				Recipes = new List<RecipeDto>(data.Recipes.Count),
				ActiveRecipe = data.ActiveRecipe,
				AutoCookEnabled = data.AutoCookEnabled,
				ChainState = new ChainStateDto
				{
					OrderRemaining = new List<string>(data.ChainState.OrderRemaining),
					BoardProgress = data.ChainState.BoardProgress,
					PotContents = new List<string>(data.ChainState.PotContents),
					CookProgress = data.ChainState.CookProgress,
				},
				Stats = new StatsDto
				{
					TotalDishes = data.Stats.TotalDishes,
					SsCount = data.Stats.SsCount,
				},
			};

			for (int i = 0; i < data.FridgeSlots.Count; i++)
			{
				FridgeSlotState slot = data.FridgeSlots[i];
				if (slot == null) throw NullEntry("fridge_slots", i);
				dto.FridgeSlots.Add(new FridgeSlotDto { Type = slot.Type, Value = slot.Value });
			}

			for (int i = 0; i < data.Records.Count; i++)
			{
				DishRecord r = data.Records[i];
				if (r == null) throw NullEntry("records", i);
				dto.Records.Add(new DishRecordDto
				{
					DishId = r.DishId,
					Route = r.Route,
					Rank = r.Rank,
					Tier = r.Tier,
					Affix = r.Affix == null ? null : new AffixDto { Type = r.Affix.Type, Value = r.Affix.Value },
					Maker = r.Maker,
					Timestamp = r.Timestamp,
					Score = r.Score,
				});
			}

			foreach (KeyValuePair<string, Dictionary<string, string>> dish in data.Compendium)
			{
				ValidateCompendiumEntry(dish.Key, dish.Value);
				dto.Compendium.Add(dish.Key, new Dictionary<string, string>(dish.Value, StringComparer.Ordinal));
			}

			for (int i = 0; i < data.Recipes.Count; i++)
			{
				Recipe recipe = data.Recipes[i];
				if (recipe == null) throw NullEntry("recipes", i);
				dto.Recipes.Add(new RecipeDto { Name = recipe.Name, Items = new List<string>(recipe.Items) });
			}

			return dto;
		}

		// DTO（僅供 JSON 轉換；頂層可空型別用來偵測欄位缺漏並補空值）
		internal sealed class SaveDto
		{
			[JsonProperty("version")] public int? Version { get; set; }
			[JsonProperty("last_active")] public long? LastActive { get; set; }
			[JsonProperty("nickname")] public string Nickname { get; set; }
			[JsonProperty("gold")] public long? Gold { get; set; }
			[JsonProperty("fridge_inventory")] public Dictionary<string, int> FridgeInventory { get; set; }
			[JsonProperty("fridge_capacity")] public int? FridgeCapacity { get; set; }
			[JsonProperty("upgrades")] public Dictionary<string, int> Upgrades { get; set; }
			[JsonProperty("fridge_slots")] public List<FridgeSlotDto> FridgeSlots { get; set; }
			[JsonProperty("records")] public List<DishRecordDto> Records { get; set; }
			[JsonProperty("compendium")] public Dictionary<string, Dictionary<string, string>> Compendium { get; set; }
			[JsonProperty("shelf")] public List<int?> Shelf { get; set; }
			[JsonProperty("recipes")] public List<RecipeDto> Recipes { get; set; }
			[JsonProperty("active_recipe")] public int? ActiveRecipe { get; set; }
			[JsonProperty("auto_cook_enabled")] public bool? AutoCookEnabled { get; set; }
			[JsonProperty("chain_state")] public ChainStateDto ChainState { get; set; }
			[JsonProperty("stats")] public StatsDto Stats { get; set; }
		}

		internal sealed class FridgeSlotDto
		{
			[JsonProperty("type")] public string Type { get; set; }
			// 無參數的格子（例：random）不輸出 value，與 4.4 範例一致
			[JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)] public string Value { get; set; }
		}

		/// <summary>
		/// 每筆紀錄的欄位都必須出現：可為 null 的欄位（dish_id / route / rank / affix）用 AllowNull，
		/// 其餘用 Always。避免缺 dish_id 的損毀紀錄被默默讀成失敗料理、缺 score 被讀成 0 分。
		/// </summary>
		internal sealed class DishRecordDto
		{
			[JsonProperty("dish_id", Required = Required.AllowNull)] public string DishId { get; set; }
			[JsonProperty("route", Required = Required.AllowNull)] public string Route { get; set; }
			[JsonProperty("rank", Required = Required.AllowNull)] public string Rank { get; set; }
			[JsonProperty("tier", Required = Required.Always)] public int Tier { get; set; }
			[JsonProperty("affix", Required = Required.AllowNull)] public AffixDto Affix { get; set; }
			[JsonProperty("maker", Required = Required.Always)] public string Maker { get; set; }
			[JsonProperty("timestamp", Required = Required.Always)] public long Timestamp { get; set; }
			[JsonProperty("score", Required = Required.Always)] public float Score { get; set; }
		}

		internal sealed class AffixDto
		{
			[JsonProperty("type")] public string Type { get; set; }
			[JsonProperty("value")] public float Value { get; set; }
		}

		internal sealed class RecipeDto
		{
			[JsonProperty("name")] public string Name { get; set; }
			[JsonProperty("items")] public List<string> Items { get; set; }
		}

		internal sealed class ChainStateDto
		{
			[JsonProperty("order_remaining")] public List<string> OrderRemaining { get; set; }
			[JsonProperty("board_progress")] public float? BoardProgress { get; set; }
			[JsonProperty("pot_contents")] public List<string> PotContents { get; set; }
			[JsonProperty("cook_progress")] public float? CookProgress { get; set; }
		}

		internal sealed class StatsDto
		{
			[JsonProperty("total_dishes")] public int? TotalDishes { get; set; }
			[JsonProperty("ss_count")] public int? SsCount { get; set; }
		}
	}
}
