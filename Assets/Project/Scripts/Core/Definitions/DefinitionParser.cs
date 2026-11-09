using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Kitchen.Core.Data;
using Newtonsoft.Json;

namespace Kitchen.Core.Definitions
{
	/// <summary>
	/// 定義資料 JSON → Core 純資料（技術文件 2.3、3.3、4.1、4.2）。
	/// JSON 先進內部 DTO，驗證後再轉為不可變的 Core 資料類別。
	/// 使用自有的 <see cref="JsonSerializer"/>，不受全域 <see cref="JsonConvert.DefaultSettings"/> 影響。
	/// </summary>
	public static class DefinitionParser
	{
		// Public
		/// <summary>解析食材定義陣列（ingredients.json）。格式或內容錯誤時拋出 <see cref="DefinitionException"/>。</summary>
		public static IReadOnlyList<IngredientData> ParseIngredients(string json)
		{
			List<IngredientDto> dtos = DeserializeArray<IngredientDto>(json, IngredientContext);

			var result = new List<IngredientData>(dtos.Count);
			var seenIds = new Dictionary<string, int>(StringComparer.Ordinal);

			for (int i = 0; i < dtos.Count; i++)
			{
				IngredientDto dto = dtos[i];
				string id = CheckEntryAndId(IngredientContext, dto, dto?.Id, i, seenIds);

				if (string.IsNullOrWhiteSpace(dto.NameKey)) throw MissingField(IngredientContext, i, id, "name_key");
				if (dto.Categories == null) throw MissingField(IngredientContext, i, id, "categories");
				for (int c = 0; c < dto.Categories.Count; c++)
				{
					if (string.IsNullOrWhiteSpace(dto.Categories[c]))
					{
						throw Error(IngredientContext, i, id, $"field 'categories[{c}]' is null or empty.");
					}
				}
				if (dto.Flavor == null) throw MissingField(IngredientContext, i, id, "flavor");
				FlavorVector flavor = ToFlavor(dto.Flavor, i, id);
				if (!dto.Rarity.HasValue) throw MissingField(IngredientContext, i, id, "rarity");
				if (!dto.BuyPrice.HasValue) throw MissingField(IngredientContext, i, id, "buy_price");
				if (!dto.SellPrice.HasValue) throw MissingField(IngredientContext, i, id, "sell_price");
				if (string.IsNullOrWhiteSpace(dto.Sprite)) throw MissingField(IngredientContext, i, id, "sprite");

				result.Add(new IngredientData(
					id,
					dto.NameKey,
					dto.Categories,
					flavor,
					dto.Rarity.Value,
					dto.BuyPrice.Value,
					dto.SellPrice.Value,
					dto.Sprite));
			}

			return new ReadOnlyCollection<IngredientData>(result);
		}

		/// <summary>
		/// 解析菜餚定義陣列（dishes.json）。格式或內容錯誤時拋出 <see cref="DefinitionException"/>。
		/// 含路線驗證：路線名合法、route_caps 與 allowed_routes 一致、授權 balanced 時 active 軸 ≥ 2（5.4.4）。
		/// </summary>
		public static IReadOnlyList<DishData> ParseDishes(string json)
		{
			List<DishDto> dtos = DeserializeArray<DishDto>(json, DishContext);

			var result = new List<DishData>(dtos.Count);
			var seenIds = new Dictionary<string, int>(StringComparer.Ordinal);

			for (int i = 0; i < dtos.Count; i++)
			{
				DishDto dto = dtos[i];
				string id = CheckEntryAndId(DishContext, dto, dto?.Id, i, seenIds);

				if (string.IsNullOrWhiteSpace(dto.NameKey)) throw MissingField(DishContext, i, id, "name_key");

				ValidateAnchors(dto.Anchors, i, id);
				ValidateRoutes(dto.AllowedRoutes, dto.RouteCaps, i, id);
				FloatRange band = ToBand(dto.BalancedBand, i, id);

				if (!dto.BasePrice.HasValue) throw MissingField(DishContext, i, id, "base_price");
				if (!dto.Specificity.HasValue) throw MissingField(DishContext, i, id, "specificity");

				result.Add(new DishData(
					id,
					dto.NameKey,
					dto.Anchors,
					dto.AllowedRoutes,
					dto.RouteCaps,
					band,
					dto.BasePrice.Value,
					dto.Specificity.Value));
			}

			return new ReadOnlyCollection<DishData>(result);
		}

		// Private
		private const string IngredientContext = "Ingredient definitions";
		private const string DishContext = "Dish definitions";
		private const char Utf8Bom = '\uFEFF';

		/// <summary>授權 balanced 時，特化路線（= active 軸）的最少數量（技術文件 5.4.4）。</summary>
		private const int MinActiveAxesForBalanced = 2;

		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			MissingMemberHandling = MissingMemberHandling.Ignore,
			NullValueHandling = NullValueHandling.Include,
			// JsonConvert.DeserializeObject 預設會檢查尾端多餘內容；改用自有 serializer 後需明確開啟
			CheckAdditionalContent = true,
		};

		/// <summary>
		/// 用 parser 自有的 serializer 反序列化頂層陣列。
		/// 刻意不用 JsonConvert.DeserializeObject / JsonSerializer.CreateDefault，以免吃到全域 DefaultSettings。
		/// </summary>
		private static List<T> DeserializeArray<T>(string json, string context)
		{
			if (json != null && json.Length > 0 && json[0] == Utf8Bom)
			{
				json = json.Substring(1);
			}

			if (string.IsNullOrWhiteSpace(json))
			{
				throw new DefinitionException($"{context}: JSON text is null or empty.");
			}

			List<T> dtos;
			try
			{
				JsonSerializer serializer = JsonSerializer.Create(SerializerSettings);
				using (var stringReader = new StringReader(json))
				using (var jsonReader = new JsonTextReader(stringReader))
				{
					dtos = serializer.Deserialize<List<T>>(jsonReader);
				}
			}
			catch (JsonException ex)
			{
				throw new DefinitionException($"{context}: invalid JSON. {ex.Message}", ex);
			}

			if (dtos == null)
			{
				throw new DefinitionException($"{context}: top-level value must be a JSON array.");
			}

			return dtos;
		}

		/// <summary>檢查項目非 null、id 存在且不重複；回傳 id。</summary>
		private static string CheckEntryAndId(string context, object dto, string id, int index, Dictionary<string, int> seenIds)
		{
			if (dto == null)
			{
				throw Error(context, index, null, "entry is null.");
			}

			if (string.IsNullOrWhiteSpace(id))
			{
				throw MissingField(context, index, null, "id");
			}

			if (seenIds.TryGetValue(id, out int firstIndex))
			{
				throw Error(context, index, id, $"duplicate id (first defined at entry {firstIndex + 1}).");
			}
			seenIds.Add(id, index);

			return id;
		}

		private static FlavorVector ToFlavor(FlavorDto f, int index, string id)
		{
			if (!f.Sour.HasValue) throw MissingField(IngredientContext, index, id, "flavor.sour");
			if (!f.Sweet.HasValue) throw MissingField(IngredientContext, index, id, "flavor.sweet");
			if (!f.Bitter.HasValue) throw MissingField(IngredientContext, index, id, "flavor.bitter");
			if (!f.Salty.HasValue) throw MissingField(IngredientContext, index, id, "flavor.salty");
			if (!f.Spicy.HasValue) throw MissingField(IngredientContext, index, id, "flavor.spicy");
			return new FlavorVector(f.Sour.Value, f.Sweet.Value, f.Bitter.Value, f.Salty.Value, f.Spicy.Value);
		}

		private static void ValidateAnchors(List<List<string>> anchors, int index, string id)
		{
			if (anchors == null) throw MissingField(DishContext, index, id, "anchors");
			if (anchors.Count == 0) throw Error(DishContext, index, id, "field 'anchors' must contain at least one group.");

			for (int g = 0; g < anchors.Count; g++)
			{
				List<string> group = anchors[g];
				if (group == null || group.Count == 0)
				{
					throw Error(DishContext, index, id, $"field 'anchors[{g}]' must be a non-empty array.");
				}
				for (int c = 0; c < group.Count; c++)
				{
					if (string.IsNullOrWhiteSpace(group[c]))
					{
						throw Error(DishContext, index, id, $"field 'anchors[{g}][{c}]' is null or empty.");
					}
				}
			}
		}

		private static void ValidateRoutes(List<string> allowedRoutes, Dictionary<string, int> routeCaps, int index, string id)
		{
			if (allowedRoutes == null) throw MissingField(DishContext, index, id, "allowed_routes");
			if (allowedRoutes.Count == 0) throw Error(DishContext, index, id, "field 'allowed_routes' must not be empty.");
			if (routeCaps == null) throw MissingField(DishContext, index, id, "route_caps");

			var seenRoutes = new HashSet<string>(StringComparer.Ordinal);
			bool allowsBalanced = false;
			int specializedCount = 0;

			for (int r = 0; r < allowedRoutes.Count; r++)
			{
				string route = allowedRoutes[r];
				if (!DishRoutes.IsValid(route))
				{
					throw Error(DishContext, index, id, $"field 'allowed_routes[{r}]' has unknown route '{route}' (valid: {ValidRouteList}).");
				}
				if (!seenRoutes.Add(route))
				{
					throw Error(DishContext, index, id, $"field 'allowed_routes' lists route '{route}' more than once.");
				}

				if (DishRoutes.IsSpecialized(route))
				{
					specializedCount++;
					// 特化路線計分需要 route_cap（5.4.3 強度 = D / route_cap）
					if (!routeCaps.ContainsKey(route))
					{
						throw Error(DishContext, index, id, $"field 'route_caps' is missing a cap for allowed route '{route}'.");
					}
				}
				else
				{
					allowsBalanced = true;
				}
			}

			foreach (KeyValuePair<string, int> pair in routeCaps)
			{
				if (!DishRoutes.IsValid(pair.Key))
				{
					throw Error(DishContext, index, id, $"field 'route_caps' has unknown route '{pair.Key}' (valid: {ValidRouteList}).");
				}
				if (string.Equals(pair.Key, DishRoutes.Balanced, StringComparison.Ordinal))
				{
					// 均衡系不用 cap（5.4.4 用 balanced_band）；RouteCaps 只允許特化路線
					throw Error(DishContext, index, id, "field 'route_caps' must not contain 'balanced' (balanced has no cap; use 'balanced_band').");
				}
				if (!seenRoutes.Contains(pair.Key))
				{
					throw Error(DishContext, index, id, $"field 'route_caps' key '{pair.Key}' is not in 'allowed_routes'.");
				}
				if (pair.Value <= 0)
				{
					throw Error(DishContext, index, id, $"field 'route_caps.{pair.Key}' must be greater than 0 (got {pair.Value}).");
				}
			}

			// 5.4.4：單一 active 軸時均勻度恆為 1，均衡失去意義 → 授權 balanced 需 ≥ 2 條特化路線
			if (allowsBalanced && specializedCount < MinActiveAxesForBalanced)
			{
				throw Error(DishContext, index, id,
					$"route 'balanced' requires at least {MinActiveAxesForBalanced} specialized routes (active axes) in 'allowed_routes', found {specializedCount}.");
			}
		}

		private static FloatRange ToBand(float[] band, int index, string id)
		{
			if (band == null) throw MissingField(DishContext, index, id, "balanced_band");
			if (band.Length != 2)
			{
				throw Error(DishContext, index, id, $"field 'balanced_band' must have exactly 2 values [lo, hi] (got {band.Length}).");
			}

			float lo = band[0];
			float hi = band[1];
			if (float.IsNaN(lo) || float.IsInfinity(lo) || float.IsNaN(hi) || float.IsInfinity(hi))
			{
				throw Error(DishContext, index, id, "field 'balanced_band' values must be finite numbers.");
			}
			if (lo > hi)
			{
				throw Error(DishContext, index, id, FormattableString.Invariant($"field 'balanced_band' requires lo <= hi (got [{lo}, {hi}])."));
			}

			return new FloatRange(lo, hi);
		}

		private static readonly string ValidRouteList = string.Join(", ", new[]
		{
			DishRoutes.Sour, DishRoutes.Sweet, DishRoutes.Salty, DishRoutes.Spicy, DishRoutes.Balanced,
		});

		private static DefinitionException MissingField(string context, int index, string id, string field)
		{
			return Error(context, index, id, $"missing required field '{field}'.");
		}

		/// <summary>錯誤訊息格式：「{context}: entry {n} (id 'x'): {detail}」，n 從 1 開始。</summary>
		private static DefinitionException Error(string context, int index, string id, string detail)
		{
			string idPart = id == null ? "id <none>" : $"id '{id}'";
			return new DefinitionException($"{context}: entry {index + 1} ({idPart}): {detail}");
		}

		// DTO（僅供反序列化；可空型別用來偵測欄位缺漏）
		internal sealed class IngredientDto
		{
			[JsonProperty("id")] public string Id { get; set; }
			[JsonProperty("name_key")] public string NameKey { get; set; }
			[JsonProperty("categories")] public List<string> Categories { get; set; }
			[JsonProperty("flavor")] public FlavorDto Flavor { get; set; }
			[JsonProperty("rarity")] public int? Rarity { get; set; }
			[JsonProperty("buy_price")] public int? BuyPrice { get; set; }
			[JsonProperty("sell_price")] public int? SellPrice { get; set; }
			[JsonProperty("sprite")] public string Sprite { get; set; }
		}

		internal sealed class FlavorDto
		{
			[JsonProperty("sour")] public int? Sour { get; set; }
			[JsonProperty("sweet")] public int? Sweet { get; set; }
			[JsonProperty("bitter")] public int? Bitter { get; set; }
			[JsonProperty("salty")] public int? Salty { get; set; }
			[JsonProperty("spicy")] public int? Spicy { get; set; }
		}

		internal sealed class DishDto
		{
			[JsonProperty("id")] public string Id { get; set; }
			[JsonProperty("name_key")] public string NameKey { get; set; }
			[JsonProperty("anchors")] public List<List<string>> Anchors { get; set; }
			[JsonProperty("allowed_routes")] public List<string> AllowedRoutes { get; set; }
			[JsonProperty("route_caps")] public Dictionary<string, int> RouteCaps { get; set; }
			[JsonProperty("balanced_band")] public float[] BalancedBand { get; set; }
			[JsonProperty("base_price")] public int? BasePrice { get; set; }
			[JsonProperty("specificity")] public int? Specificity { get; set; }
		}
	}
}
