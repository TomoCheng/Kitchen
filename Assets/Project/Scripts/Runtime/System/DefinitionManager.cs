using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kitchen.Core.Data;
using Kitchen.Core.Definitions;
using Newtonsoft.Json;
using Tomo.Asset;
using Tomo.Core;
using UnityEngine;

namespace Kitchen.Systems
{
	/// <summary>
	/// 定義資料載入（技術文件 2.3、4.1、4.2）。
	/// 以 Addressables 把食材 / 菜餚 / 字串表 JSON 載為字串，交給 Core 的 <see cref="DefinitionParser"/> 解析成純資料。
	/// 載入或解析失敗只記錄錯誤、對應清單維持為空，不讓整個初始化流程中斷。
	/// 必須排在 GameManager._managerList 中 AssetManager 之後。
	/// </summary>
	public class DefinitionManager : Singleton<DefinitionManager>
	{
		// Properties
		public IReadOnlyList<IngredientData> Ingredients => _ingredients;
		public IReadOnlyList<DishData> Dishes => _dishes;

		// Public
		public override async UniTask Initialize()
		{
			await base.Initialize();

			ClearAll();
			CancellationToken token = this.GetCancellationTokenOnDestroy();

			string ingredientsJson = await LoadText(IngredientsAddress, token);
			if (token.IsCancellationRequested) return;
			string dishesJson = await LoadText(DishesAddress, token);
			if (token.IsCancellationRequested) return;
			string stringsJson = await LoadText(StringsAddress, token);
			if (token.IsCancellationRequested) return;

			if (ingredientsJson != null)
			{
				_ingredients = Parse<IReadOnlyList<IngredientData>>(IngredientsAddress, ingredientsJson, DefinitionParser.ParseIngredients) ?? Array.Empty<IngredientData>();
			}
			if (dishesJson != null)
			{
				_dishes = Parse<IReadOnlyList<DishData>>(DishesAddress, dishesJson, DefinitionParser.ParseDishes) ?? Array.Empty<DishData>();
			}
			if (stringsJson != null)
			{
				_strings = Parse<Dictionary<string, string>>(StringsAddress, stringsJson, ParseStringTable);
			}

			BuildLookups();
			LogSummary();
		}

		public bool TryGetIngredient(string id, out IngredientData ingredient)
		{
			if (id == null)
			{
				ingredient = null;
				return false;
			}
			return _ingredientMap.TryGetValue(id, out ingredient);
		}

		public bool TryGetDish(string id, out DishData dish)
		{
			if (id == null)
			{
				dish = null;
				return false;
			}
			return _dishMap.TryGetValue(id, out dish);
		}

		/// <summary>依在地化 key 取顯示文字；找不到時回傳 key 本身，同一個 key 只警告一次。</summary>
		public string GetText(string key)
		{
			if (string.IsNullOrEmpty(key)) return string.Empty;

			if (_strings != null && _strings.TryGetValue(key, out string text) && text != null)
			{
				return text;
			}

			if (_warnedMissingKeys.Add(key))
			{
				Debug.LogWarning($"[DefinitionManager] Missing string key '{key}' in '{StringsAddress}'; showing the key instead.");
			}
			return key;
		}

		// Private
		private const string IngredientsAddress = "ingredients";
		private const string DishesAddress = "dishes";
		private const string StringsAddress = "zh-Hant";
		private const char Utf8Bom = (char)0xFEFF;

		/// <summary>字串表專用設定；以 JsonSerializer.Create 建立，不受全域 JsonConvert.DefaultSettings 影響（同 Core 做法）。</summary>
		private static readonly JsonSerializerSettings StringTableSettings = new JsonSerializerSettings
		{
			CheckAdditionalContent = true,
		};

		/// <summary>載入 TextAsset、取出文字後立即釋放；失敗回傳 null 並記錄錯誤（取消時不記錄）。</summary>
		private async UniTask<string> LoadText(string address, CancellationToken token)
		{
			TextAsset asset = await AssetManager.Instance.LoadAsset<TextAsset>(address, token);
			if (asset == null)
			{
				if (!token.IsCancellationRequested)
				{
					Debug.LogError($"[DefinitionManager] Failed to load TextAsset at Addressables address '{address}'. Check that it is registered in the Data group.");
				}
				return null;
			}

			string text = asset.text;
			AssetManager.Instance.Release(asset);
			return text;
		}

		/// <summary>執行解析；失敗時記錄含 address 的錯誤並回傳 null。</summary>
		private static T Parse<T>(string address, string json, Func<string, T> parser) where T : class
		{
			try
			{
				return parser(json);
			}
			catch (DefinitionException ex)
			{
				Debug.LogError($"[DefinitionManager] Failed to parse '{address}': {ex.Message}");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[DefinitionManager] Unexpected error while parsing '{address}': {ex.GetType().Name}: {ex.Message}");
				Debug.LogException(ex);
			}
			return null;
		}

		/// <summary>字串表：頂層為 { key: string } 物件。格式錯誤時拋出 DefinitionException。</summary>
		private static Dictionary<string, string> ParseStringTable(string json)
		{
			if (json != null && json.Length > 0 && json[0] == Utf8Bom)
			{
				json = json.Substring(1);
			}
			if (string.IsNullOrWhiteSpace(json))
			{
				throw new DefinitionException("String table: JSON text is null or empty.");
			}

			Dictionary<string, string> table;
			try
			{
				JsonSerializer serializer = JsonSerializer.Create(StringTableSettings);
				using (var stringReader = new StringReader(json))
				using (var jsonReader = new JsonTextReader(stringReader))
				{
					table = serializer.Deserialize<Dictionary<string, string>>(jsonReader);
				}
			}
			catch (JsonException ex)
			{
				throw new DefinitionException($"String table: invalid JSON. {ex.Message}", ex);
			}

			if (table == null)
			{
				throw new DefinitionException("String table: top-level value must be a JSON object.");
			}
			return new Dictionary<string, string>(table, StringComparer.Ordinal);
		}

		private void ClearAll()
		{
			_ingredients = Array.Empty<IngredientData>();
			_dishes = Array.Empty<DishData>();
			_strings = null;
			_ingredientMap.Clear();
			_dishMap.Clear();
			_warnedMissingKeys.Clear();
		}

		private void BuildLookups()
		{
			// 解析器已保證 id 不重複，此處直接建表
			for (int i = 0; i < _ingredients.Count; i++)
			{
				_ingredientMap[_ingredients[i].Id] = _ingredients[i];
			}
			for (int i = 0; i < _dishes.Count; i++)
			{
				_dishMap[_dishes[i].Id] = _dishes[i];
			}
		}

		/// <summary>M0 判準「印得出食材」：列出所有食材與菜餚。</summary>
		private void LogSummary()
		{
			var sb = new StringBuilder();
			sb.Append("[DefinitionManager] Loaded ")
				.Append(_ingredients.Count).Append(" ingredients, ")
				.Append(_dishes.Count).Append(" dishes, ")
				.Append(_strings != null ? _strings.Count : 0).Append(" strings.");

			sb.Append("\n-- Ingredients (id / name / sour sweet bitter salty spicy) --");
			for (int i = 0; i < _ingredients.Count; i++)
			{
				IngredientData data = _ingredients[i];
				FlavorVector f = data.Flavor;
				sb.Append("\n  ").Append(data.Id)
					.Append(" / ").Append(GetText(data.NameKey))
					.Append(" / sour=").Append(f.Sour)
					.Append(" sweet=").Append(f.Sweet)
					.Append(" bitter=").Append(f.Bitter)
					.Append(" salty=").Append(f.Salty)
					.Append(" spicy=").Append(f.Spicy);
			}

			// 4.2：錨定內容不對玩家公開；Debug.Log 會寫進玩家看得到的 Player.log，故只在 Editor / 開發版印 anchors
#if UNITY_EDITOR || DEVELOPMENT_BUILD
			sb.Append("\n-- Dishes (id / name / anchors / allowed_routes) --");
#else
			sb.Append("\n-- Dishes (id / name / allowed_routes) --");
#endif
			for (int i = 0; i < _dishes.Count; i++)
			{
				DishData data = _dishes[i];
				sb.Append("\n  ").Append(data.Id)
					.Append(" / ").Append(GetText(data.NameKey));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				sb.Append(" / anchors=");
				AppendAnchors(sb, data.Anchors);
#endif
				sb.Append(" / routes=[").Append(string.Join(", ", data.AllowedRoutes)).Append(']');
			}

			Debug.Log(sb.ToString());
		}

#if UNITY_EDITOR || DEVELOPMENT_BUILD
		private static void AppendAnchors(StringBuilder sb, IReadOnlyList<IReadOnlyList<string>> anchors)
		{
			sb.Append('[');
			for (int g = 0; g < anchors.Count; g++)
			{
				if (g > 0) sb.Append(", ");
				sb.Append('[').Append(string.Join(" | ", anchors[g])).Append(']');
			}
			sb.Append(']');
		}
#endif

		// Variable
		private IReadOnlyList<IngredientData> _ingredients = Array.Empty<IngredientData>();
		private IReadOnlyList<DishData> _dishes = Array.Empty<DishData>();
		private Dictionary<string, string> _strings;
		private readonly Dictionary<string, IngredientData> _ingredientMap = new Dictionary<string, IngredientData>(StringComparer.Ordinal);
		private readonly Dictionary<string, DishData> _dishMap = new Dictionary<string, DishData>(StringComparer.Ordinal);
		private readonly HashSet<string> _warnedMissingKeys = new HashSet<string>(StringComparer.Ordinal);
	}
}
