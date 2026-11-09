using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kitchen.Core.Data;
using Kitchen.Core.Definitions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	/// <summary>
	/// 實檔對拍（M0 S7）：Assets/Project/Data 下的定義資料必須與技術文件附錄 B 一致、可被解析、彼此引用完整。
	/// EditMode 測試的工作目錄為專案根目錄。
	/// </summary>
	public class DefinitionDataFileTests
	{
		private static readonly string DataDir = Path.Combine("Assets", "Project", "Data");
		private static readonly string IngredientsPath = Path.Combine(DataDir, "ingredients.json");
		private static readonly string DishesPath = Path.Combine(DataDir, "dishes.json");
		private static readonly string StringsPath = Path.Combine(DataDir, "strings", "zh-Hant.json");

		private IReadOnlyList<IngredientData> _ingredients;
		private IReadOnlyList<DishData> _dishes;
		private Dictionary<string, string> _strings;

		[OneTimeSetUp]
		public void LoadFiles()
		{
			_ingredients = DefinitionParser.ParseIngredients(ReadText(IngredientsPath));
			_dishes = DefinitionParser.ParseDishes(ReadText(DishesPath));

			// 字串表用獨立 serializer，不受全域 DefaultSettings 影響
			var serializer = JsonSerializer.Create(new JsonSerializerSettings());
			using (var reader = new JsonTextReader(new StringReader(ReadText(StringsPath))))
			{
				_strings = serializer.Deserialize<Dictionary<string, string>>(reader);
			}
			Assert.IsNotNull(_strings, $"String table '{StringsPath}' must be a JSON object.");
		}

		private static string ReadText(string relativePath)
		{
			if (!File.Exists(relativePath))
			{
				Assert.Fail($"Data file not found: '{relativePath}' (resolved to '{Path.GetFullPath(relativePath)}'). " +
					"EditMode tests expect the working directory to be the Unity project root.");
			}
			return File.ReadAllText(relativePath);
		}

		private IngredientData FindIngredient(string id)
		{
			IngredientData found = _ingredients.FirstOrDefault(x => x.Id == id);
			Assert.IsNotNull(found, $"Ingredient '{id}' not found in {IngredientsPath}.");
			return found;
		}

		private DishData FindDish(string id)
		{
			DishData found = _dishes.FirstOrDefault(x => x.Id == id);
			Assert.IsNotNull(found, $"Dish '{id}' not found in {DishesPath}.");
			return found;
		}

		// ---- 食材（附錄 B.1）----

		private static readonly string[] ExpectedIngredientIds =
		{
			"tomato", "egg", "sugar", "salt", "lemon", "soy_sauce", "onion", "chili", "bitter_melon",
		};

		[Test]
		public void Ingredients_ContainExactlyNineAppendixIds()
		{
			CollectionAssert.AreEquivalent(ExpectedIngredientIds, _ingredients.Select(x => x.Id).ToArray());
		}

		// 欄位：id、酸、甜、苦、鹹、辣、categories（逗號分隔，順序須一致）、買、賣
		[TestCase("tomato", 2, 1, 0, 0, 0, "vegetable,tomato_family,sour_family", 12, 3)]
		[TestCase("egg", 0, 0, 0, 0, 0, "protein,egg_family", 10, 3)]
		[TestCase("sugar", 0, 2, 0, 0, 0, "seasoning,sugar_family,sweet_family", 8, 2)]
		[TestCase("salt", 0, 0, 0, 2, 0, "seasoning,salt_family,salty_family", 8, 2)]
		[TestCase("lemon", 3, 0, 0, 0, 0, "fruit,lemon_family,sour_family", 16, 4)]
		[TestCase("soy_sauce", 0, 1, 0, 2, 0, "seasoning,soy_family,salty_family", 14, 4)]
		[TestCase("onion", 0, 1, 0, 0, 1, "vegetable,onion_family", 12, 3)]
		[TestCase("chili", 0, 0, 0, 0, 2, "vegetable,chili_family,spicy_family", 14, 4)]
		[TestCase("bitter_melon", 0, 0, 2, 0, 0, "vegetable,bittermelon_family", 10, 3)]
		public void Ingredient_MatchesAppendixB1(
			string id, int sour, int sweet, int bitter, int salty, int spicy,
			string categories, int buyPrice, int sellPrice)
		{
			IngredientData data = FindIngredient(id);

			Assert.AreEqual(new FlavorVector(sour, sweet, bitter, salty, spicy), data.Flavor, $"{id}: flavor");
			CollectionAssert.AreEqual(categories.Split(','), data.Categories, $"{id}: categories");
			Assert.AreEqual(buyPrice, data.BuyPrice, $"{id}: buy_price");
			Assert.AreEqual(sellPrice, data.SellPrice, $"{id}: sell_price");
			Assert.AreEqual($"ingredient.{id}.name", data.NameKey, $"{id}: name_key naming rule (4.1)");
		}

		// ---- 菜餚（附錄 B.2）----

		[Test]
		public void Dishes_ContainExactlyThreeAppendixIds()
		{
			CollectionAssert.AreEquivalent(
				new[] { "tomato_egg_stirfry", "spicy_egg_stirfry", "sweet_onion_egg" },
				_dishes.Select(x => x.Id).ToArray());
		}

		// 欄位：id、anchors（組以 '|' 分隔、組內以 ',' 分隔）、allowed_routes、route_caps（route:cap）、band lo/hi、base_price、specificity
		[TestCase("tomato_egg_stirfry", "egg_family|tomato_family", "sour,salty,balanced", "sour:10,salty:10", 2.5f, 6.5f, 50, 2)]
		[TestCase("spicy_egg_stirfry", "egg_family|chili_family", "spicy,salty,balanced", "spicy:10,salty:10", 2.5f, 6.5f, 55, 2)]
		[TestCase("sweet_onion_egg", "egg_family|onion_family", "sweet,spicy,balanced", "sweet:10,spicy:10", 2.5f, 6.5f, 55, 2)]
		public void Dish_MatchesAppendixB2(
			string id, string anchors, string routes, string caps,
			float bandLo, float bandHi, int basePrice, int specificity)
		{
			DishData dish = FindDish(id);

			string[] expectedGroups = anchors.Split('|');
			Assert.AreEqual(expectedGroups.Length, dish.Anchors.Count, $"{id}: anchor group count");
			for (int g = 0; g < expectedGroups.Length; g++)
			{
				CollectionAssert.AreEqual(expectedGroups[g].Split(','), dish.Anchors[g], $"{id}: anchors[{g}]");
			}

			CollectionAssert.AreEqual(routes.Split(','), dish.AllowedRoutes, $"{id}: allowed_routes");

			var expectedCaps = new Dictionary<string, int>();
			foreach (string pair in caps.Split(','))
			{
				string[] parts = pair.Split(':');
				expectedCaps.Add(parts[0], int.Parse(parts[1]));
			}
			CollectionAssert.AreEquivalent(expectedCaps, dish.RouteCaps, $"{id}: route_caps");

			Assert.AreEqual(new FloatRange(bandLo, bandHi), dish.BalancedBand, $"{id}: balanced_band");
			Assert.AreEqual(basePrice, dish.BasePrice, $"{id}: base_price");
			Assert.AreEqual(specificity, dish.Specificity, $"{id}: specificity");
			Assert.AreEqual($"dish.{id}.name", dish.NameKey, $"{id}: name_key naming rule (4.1)");
		}

		// ---- 交叉驗證 ----

		[Test]
		public void EveryAnchorCategory_IsCarriedByAtLeastOneIngredient()
		{
			var missing = new List<string>();
			foreach (DishData dish in _dishes)
			{
				for (int g = 0; g < dish.Anchors.Count; g++)
				{
					foreach (string category in dish.Anchors[g])
					{
						if (!_ingredients.Any(x => x.HasCategory(category)))
						{
							missing.Add($"{dish.Id}.anchors[{g}] '{category}'");
						}
					}
				}
			}

			CollectionAssert.IsEmpty(missing, "Anchor categories with no ingredient carrying them: " + string.Join("; ", missing));
		}

		[Test]
		public void EveryNameKey_HasZhHantString()
		{
			var keys = _ingredients.Select(x => x.NameKey).Concat(_dishes.Select(x => x.NameKey));
			var missing = new List<string>();
			foreach (string key in keys)
			{
				if (!_strings.TryGetValue(key, out string text) || string.IsNullOrWhiteSpace(text))
				{
					missing.Add(key);
				}
			}

			CollectionAssert.IsEmpty(missing, $"name_key without a string in {StringsPath}: " + string.Join(", ", missing));
		}
	}
}
