using System;
using System.Collections.Generic;
using Kitchen.Core.Data;
using Kitchen.Core.Definitions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class DefinitionParserTests
	{
		// 測試用 JSON 以單引號撰寫，再換成雙引號，避免大量跳脫
		private static string J(string singleQuoted) => singleQuoted.Replace('\'', '"');

		private const string TomatoEntry =
			"{'id':'tomato','name_key':'ingredient.tomato.name'," +
			"'categories':['vegetable','tomato_family','sour_family']," +
			"'flavor':{'sour':2,'sweet':1,'bitter':0,'salty':0,'spicy':0}," +
			"'rarity':1,'buy_price':12,'sell_price':3,'sprite':'S_Ingredient_Tomato'}";

		private const string EggEntry =
			"{'id':'egg','name_key':'ingredient.egg.name'," +
			"'categories':['protein','egg_family']," +
			"'flavor':{'sour':0,'sweet':0,'bitter':0,'salty':0,'spicy':0}," +
			"'rarity':1,'buy_price':10,'sell_price':3,'sprite':'S_Ingredient_Egg'}";

		// ---- 正常解析 ----

		[Test]
		public void Parse_ValidEntries_ReturnsAllInOrder()
		{
			IReadOnlyList<IngredientData> list = DefinitionParser.ParseIngredients(J("[" + TomatoEntry + "," + EggEntry + "]"));

			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("tomato", list[0].Id);
			Assert.AreEqual("egg", list[1].Id);
		}

		[Test]
		public void Parse_ValidEntry_MapsAllFields()
		{
			IngredientData tomato = DefinitionParser.ParseIngredients(J("[" + TomatoEntry + "]"))[0];

			Assert.AreEqual("tomato", tomato.Id);
			Assert.AreEqual("ingredient.tomato.name", tomato.NameKey);
			CollectionAssert.AreEqual(new[] { "vegetable", "tomato_family", "sour_family" }, tomato.Categories);
			Assert.AreEqual(new FlavorVector(2, 1, 0, 0, 0), tomato.Flavor);
			Assert.AreEqual(1, tomato.Rarity);
			Assert.AreEqual(12, tomato.BuyPrice);
			Assert.AreEqual(3, tomato.SellPrice);
			Assert.AreEqual("S_Ingredient_Tomato", tomato.Sprite);
		}

		[Test]
		public void Parse_PureIdentityIngredient_HasZeroFlavor()
		{
			IngredientData egg = DefinitionParser.ParseIngredients(J("[" + EggEntry + "]"))[0];

			Assert.AreEqual(FlavorVector.Zero, egg.Flavor);
			Assert.IsTrue(egg.HasCategory("egg_family"));
			Assert.IsFalse(egg.HasCategory("tomato_family"));
		}

		[Test]
		public void Parse_UnknownExtraField_IsIgnored()
		{
			string json = J("[" + TomatoEntry.Replace("'rarity':1,", "'rarity':1,'future_field':42,") + "]");

			IReadOnlyList<IngredientData> list = DefinitionParser.ParseIngredients(json);

			Assert.AreEqual(1, list.Count);
		}

		[Test]
		public void Parse_EmptyArray_ReturnsEmptyList()
		{
			IReadOnlyList<IngredientData> list = DefinitionParser.ParseIngredients("[]");

			Assert.IsNotNull(list);
			Assert.AreEqual(0, list.Count);
		}

		// ---- 欄位缺漏 / 重複 ----

		[Test]
		public void Parse_MissingId_Throws()
		{
			string json = J("[" + EggEntry + "," + TomatoEntry.Replace("'id':'tomato',", "") + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			StringAssert.Contains("#1", ex.Message);
			StringAssert.Contains("'id'", ex.Message);
		}

		[Test]
		public void Parse_EmptyId_Throws()
		{
			string json = J("[" + TomatoEntry.Replace("'id':'tomato'", "'id':''") + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			StringAssert.Contains("'id'", ex.Message);
		}

		[Test]
		public void Parse_DuplicateId_Throws()
		{
			string json = J("[" + TomatoEntry + "," + EggEntry + "," + TomatoEntry + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			StringAssert.Contains("#2", ex.Message);
			StringAssert.Contains("tomato", ex.Message);
			StringAssert.Contains("duplicate", ex.Message);
		}

		[Test]
		public void Parse_MissingFlavor_Throws()
		{
			string json = J("[" + TomatoEntry.Replace("'flavor':{'sour':2,'sweet':1,'bitter':0,'salty':0,'spicy':0},", "") + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			StringAssert.Contains("tomato", ex.Message);
			StringAssert.Contains("'flavor'", ex.Message);
		}

		[Test]
		public void Parse_MissingFlavorAxis_Throws()
		{
			string json = J("[" + TomatoEntry.Replace(",'spicy':0", "") + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			StringAssert.Contains("flavor.spicy", ex.Message);
		}

		[TestCase("'name_key':'ingredient.tomato.name',", "name_key")]
		[TestCase("'categories':['vegetable','tomato_family','sour_family'],", "categories")]
		[TestCase("'rarity':1,", "rarity")]
		[TestCase("'buy_price':12,", "buy_price")]
		[TestCase("'sell_price':3,", "sell_price")]
		[TestCase(",'sprite':'S_Ingredient_Tomato'", "sprite")]
		public void Parse_MissingRequiredField_Throws(string fragmentToRemove, string fieldName)
		{
			string entry = TomatoEntry.Replace(fragmentToRemove, "");
			Assert.AreNotEqual(TomatoEntry, entry, "Test setup: fragment not found in entry.");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(J("[" + entry + "]")));

			StringAssert.Contains("'" + fieldName + "'", ex.Message);
			StringAssert.Contains("tomato", ex.Message);
		}

		[Test]
		public void Parse_NullEntry_Throws()
		{
			string json = J("[" + TomatoEntry + ",null]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			StringAssert.Contains("#1", ex.Message);
		}

		// ---- 非法 JSON ----

		[Test]
		public void Parse_MalformedJson_ThrowsWrappingNewtonsoftException()
		{
			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients("[{\"id\": \"tomato\", "));

			Assert.IsNotNull(ex.InnerException);
			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[Test]
		public void Parse_WrongValueType_ThrowsWrappingNewtonsoftException()
		{
			string json = J("[" + TomatoEntry.Replace("'rarity':1,", "'rarity':'high',") + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));

			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[Test]
		public void Parse_TopLevelObject_ThrowsWrappingNewtonsoftException()
		{
			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(J(TomatoEntry)));

			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[TestCase((string)null)]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("null")]
		public void Parse_NullOrEmptyInput_Throws(string json)
		{
			Assert.Throws<DefinitionException>(() => DefinitionParser.ParseIngredients(json));
		}

		// ---- IngredientData 建構子防呆 ----

		[Test]
		public void IngredientData_EmptyId_Throws()
		{
			Assert.Throws<ArgumentException>(() =>
				new IngredientData("", "k", new string[0], FlavorVector.Zero, 1, 1, 1, "s"));
		}

		[Test]
		public void IngredientData_NullCategories_Throws()
		{
			Assert.Throws<ArgumentNullException>(() =>
				new IngredientData("egg", "k", null, FlavorVector.Zero, 1, 1, 1, "s"));
		}

		[Test]
		public void IngredientData_CategoriesAreDefensivelyCopied()
		{
			var source = new List<string> { "protein" };
			var data = new IngredientData("egg", "k", source, FlavorVector.Zero, 1, 1, 1, "s");

			source.Add("mutated");

			Assert.AreEqual(1, data.Categories.Count);
		}
	}
}
