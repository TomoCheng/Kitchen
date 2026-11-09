using System;
using System.Collections.Generic;
using Kitchen.Core.Data;
using Kitchen.Core.Definitions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class DishDefinitionParserTests
	{
		// 測試用 JSON 以單引號撰寫，再換成雙引號，避免大量跳脫
		private static string J(string singleQuoted) => singleQuoted.Replace('\'', '"');

		private const string TomatoEggEntry =
			"{'id':'tomato_egg_stirfry','name_key':'dish.tomato_egg_stirfry.name'," +
			"'anchors':[['egg_family'],['tomato_family']]," +
			"'allowed_routes':['sour','salty','balanced']," +
			"'route_caps':{'sour':10,'salty':10}," +
			"'balanced_band':[2.5,6.5]," +
			"'base_price':50,'specificity':2}";

		private const string SpicyEggEntry =
			"{'id':'spicy_egg_stirfry','name_key':'dish.spicy_egg_stirfry.name'," +
			"'anchors':[['egg_family'],['chili_family']]," +
			"'allowed_routes':['spicy','salty','balanced']," +
			"'route_caps':{'spicy':10,'salty':10}," +
			"'balanced_band':[2.5,6.5]," +
			"'base_price':55,'specificity':2}";

		private static IReadOnlyList<DishData> ParseOne(string entry) => DefinitionParser.ParseDishes(J("[" + entry + "]"));

		private static DefinitionException ThrowsOne(string entry)
		{
			return Assert.Throws<DefinitionException>(() => ParseOne(entry));
		}

		private static string Replaced(string source, string from, string to)
		{
			string result = source.Replace(from, to);
			Assert.AreNotEqual(source, result, "Test setup: fragment not found in entry.");
			return result;
		}

		// ---- 正常解析 ----

		[Test]
		public void ParseDishes_ValidEntry_MapsAllFields()
		{
			DishData dish = ParseOne(TomatoEggEntry)[0];

			Assert.AreEqual("tomato_egg_stirfry", dish.Id);
			Assert.AreEqual("dish.tomato_egg_stirfry.name", dish.NameKey);
			Assert.AreEqual(2, dish.Anchors.Count);
			CollectionAssert.AreEqual(new[] { "egg_family" }, dish.Anchors[0]);
			CollectionAssert.AreEqual(new[] { "tomato_family" }, dish.Anchors[1]);
			CollectionAssert.AreEqual(new[] { "sour", "salty", "balanced" }, dish.AllowedRoutes);
			Assert.AreEqual(2, dish.RouteCaps.Count);
			Assert.AreEqual(10, dish.RouteCaps["sour"]);
			Assert.AreEqual(10, dish.RouteCaps["salty"]);
			Assert.AreEqual(new FloatRange(2.5f, 6.5f), dish.BalancedBand);
			Assert.AreEqual(50, dish.BasePrice);
			Assert.AreEqual(2, dish.Specificity);
			Assert.IsTrue(dish.AllowsRoute("balanced"));
			Assert.IsFalse(dish.AllowsRoute("spicy"));
		}

		[Test]
		public void ParseDishes_MultipleEntries_KeepsDefinitionOrder()
		{
			IReadOnlyList<DishData> list = DefinitionParser.ParseDishes(J("[" + TomatoEggEntry + "," + SpicyEggEntry + "]"));

			Assert.AreEqual(2, list.Count);
			Assert.AreEqual("tomato_egg_stirfry", list[0].Id);
			Assert.AreEqual("spicy_egg_stirfry", list[1].Id);
		}

		[Test]
		public void ParseDishes_AnchorGroupWithAlternatives_IsKept()
		{
			string entry = Replaced(TomatoEggEntry, "['tomato_family']", "['tomato_family','lemon_family']");

			DishData dish = ParseOne(entry)[0];

			CollectionAssert.AreEqual(new[] { "tomato_family", "lemon_family" }, dish.Anchors[1]);
		}

		[Test]
		public void ParseDishes_SpecializedOnly_WithoutBalanced_SingleRouteIsValid()
		{
			string entry = Replaced(TomatoEggEntry, "'allowed_routes':['sour','salty','balanced']", "'allowed_routes':['sour']");
			entry = Replaced(entry, "'route_caps':{'sour':10,'salty':10}", "'route_caps':{'sour':10}");

			DishData dish = ParseOne(entry)[0];

			CollectionAssert.AreEqual(new[] { "sour" }, dish.AllowedRoutes);
		}

		[Test]
		public void ParseDishes_IntegerBandValues_AreAccepted()
		{
			string entry = Replaced(TomatoEggEntry, "[2.5,6.5]", "[3,6]");

			Assert.AreEqual(new FloatRange(3f, 6f), ParseOne(entry)[0].BalancedBand);
		}

		[Test]
		public void ParseDishes_EqualBandBounds_AreAccepted()
		{
			string entry = Replaced(TomatoEggEntry, "[2.5,6.5]", "[4,4]");

			Assert.AreEqual(new FloatRange(4f, 4f), ParseOne(entry)[0].BalancedBand);
		}

		[Test]
		public void ParseDishes_EmptyArray_ReturnsEmptyList()
		{
			Assert.AreEqual(0, DefinitionParser.ParseDishes("[]").Count);
		}

		[Test]
		public void ParseDishes_LeadingUtf8Bom_IsStripped()
		{
			IReadOnlyList<DishData> list = DefinitionParser.ParseDishes("\uFEFF" + J("[" + TomatoEggEntry + "]"));

			Assert.AreEqual("tomato_egg_stirfry", list[0].Id);
		}

		[Test]
		public void ParseDishes_IgnoresGlobalJsonConvertDefaultSettings()
		{
			Func<JsonSerializerSettings> previous = JsonConvert.DefaultSettings;
			try
			{
				JsonConvert.DefaultSettings = JsonTestUtil.HostileDefaultSettings;
				string entry = Replaced(TomatoEggEntry, "'base_price':50,", "'base_price':50,'future_field':1,");

				DishData dish = ParseOne(entry)[0];

				Assert.AreEqual("tomato_egg_stirfry", dish.Id);
				Assert.AreEqual(10, dish.RouteCaps["sour"]);
				Assert.AreEqual(new FloatRange(2.5f, 6.5f), dish.BalancedBand);
			}
			finally
			{
				JsonConvert.DefaultSettings = previous;
			}
		}

		[Test]
		public void HostileDefaultSettings_ActuallyBreakJsonConvert()
		{
			// 守門：確認上面的隔離測試有意義（若敵意設定對 JsonConvert 無效，該測試形同虛設）
			Func<JsonSerializerSettings> previous = JsonConvert.DefaultSettings;
			try
			{
				JsonConvert.DefaultSettings = JsonTestUtil.HostileDefaultSettings;

				Assert.Throws<InvalidOperationException>(() => JsonConvert.DeserializeObject<List<int>>("[1]"));
			}
			finally
			{
				JsonConvert.DefaultSettings = previous;
			}
		}

		// ---- 欄位缺漏 / 重複 ----

		[TestCase("'id':'tomato_egg_stirfry',", "id")]
		[TestCase("'name_key':'dish.tomato_egg_stirfry.name',", "name_key")]
		[TestCase("'anchors':[['egg_family'],['tomato_family']],", "anchors")]
		[TestCase("'allowed_routes':['sour','salty','balanced'],", "allowed_routes")]
		[TestCase("'route_caps':{'sour':10,'salty':10},", "route_caps")]
		[TestCase("'balanced_band':[2.5,6.5],", "balanced_band")]
		[TestCase("'base_price':50,", "base_price")]
		[TestCase(",'specificity':2", "specificity")]
		public void ParseDishes_MissingRequiredField_Throws(string fragmentToRemove, string fieldName)
		{
			string entry = Replaced(TomatoEggEntry, fragmentToRemove, "");

			var ex = ThrowsOne(entry);

			StringAssert.Contains("missing required field '" + fieldName + "'", ex.Message);
			StringAssert.Contains("Dish definitions: entry 1", ex.Message);
		}

		[Test]
		public void ParseDishes_DuplicateId_Throws()
		{
			string json = J("[" + TomatoEggEntry + "," + SpicyEggEntry + "," + TomatoEggEntry + "]");

			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseDishes(json));

			StringAssert.Contains("entry 3 (id 'tomato_egg_stirfry')", ex.Message);
			StringAssert.Contains("duplicate", ex.Message);
			StringAssert.Contains("first defined at entry 1", ex.Message);
		}

		[Test]
		public void ParseDishes_NullEntry_Throws()
		{
			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseDishes(J("[" + TomatoEggEntry + ",null]")));

			StringAssert.Contains("entry 2 (id <none>)", ex.Message);
		}

		// ---- balanced_band ----

		[TestCase("[]", "exactly 2")]
		[TestCase("[2.5]", "exactly 2")]
		[TestCase("[2.5,6.5,9]", "exactly 2")]
		[TestCase("[6.5,2.5]", "lo <= hi")]
		public void ParseDishes_InvalidBand_Throws(string band, string expectedFragment)
		{
			string entry = Replaced(TomatoEggEntry, "[2.5,6.5]", band);

			var ex = ThrowsOne(entry);

			StringAssert.Contains("'balanced_band'", ex.Message);
			StringAssert.Contains(expectedFragment, ex.Message);
			StringAssert.Contains("tomato_egg_stirfry", ex.Message);
		}

		[TestCase("[NaN,6.5]")]
		[TestCase("[2.5,1e39]")]
		public void ParseDishes_NonFiniteBand_Throws(string band)
		{
			// NaN 為 Newtonsoft 接受的 JSON 擴充字面值；1e39 超出 float 範圍 → +Infinity
			string entry = Replaced(TomatoEggEntry, "[2.5,6.5]", band);

			var ex = ThrowsOne(entry);

			StringAssert.Contains("'balanced_band'", ex.Message);
			StringAssert.Contains("finite", ex.Message);
		}

		[Test]
		public void ParseDishes_LoGreaterThanHi_MessageUsesInvariantCulture()
		{
			string entry = Replaced(TomatoEggEntry, "[2.5,6.5]", "[6.5,2.5]");

			var ex = ThrowsOne(entry);

			StringAssert.Contains("[6.5, 2.5]", ex.Message);
		}

		// ---- anchors ----

		[Test]
		public void ParseDishes_EmptyAnchors_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "[['egg_family'],['tomato_family']]", "[]"));

			StringAssert.Contains("'anchors'", ex.Message);
		}

		[Test]
		public void ParseDishes_EmptyAnchorGroup_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "['tomato_family']", "[]"));

			StringAssert.Contains("'anchors[1]'", ex.Message);
		}

		[Test]
		public void ParseDishes_NullAnchorGroup_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "['tomato_family']", "null"));

			StringAssert.Contains("'anchors[1]'", ex.Message);
		}

		[Test]
		public void ParseDishes_EmptyAnchorCategory_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "['tomato_family']", "['tomato_family','']"));

			StringAssert.Contains("'anchors[1][1]'", ex.Message);
		}

		// ---- 路線 ----

		[Test]
		public void ParseDishes_EmptyAllowedRoutes_Throws()
		{
			string entry = Replaced(TomatoEggEntry, "'allowed_routes':['sour','salty','balanced']", "'allowed_routes':[]");
			entry = Replaced(entry, "'route_caps':{'sour':10,'salty':10}", "'route_caps':{}");

			var ex = ThrowsOne(entry);

			StringAssert.Contains("'allowed_routes' must not be empty", ex.Message);
		}

		[TestCase("bitter")]
		[TestCase("Sour")]
		[TestCase("umami")]
		[TestCase("")]
		public void ParseDishes_UnknownAllowedRoute_Throws(string route)
		{
			string entry = Replaced(TomatoEggEntry, "['sour','salty','balanced']", "['sour','salty','" + route + "','balanced']");

			var ex = ThrowsOne(entry);

			StringAssert.Contains("'allowed_routes[2]'", ex.Message);
			StringAssert.Contains("unknown route '" + route + "'", ex.Message);
		}

		[Test]
		public void ParseDishes_DuplicateAllowedRoute_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "['sour','salty','balanced']", "['sour','salty','sour','balanced']"));

			StringAssert.Contains("route 'sour' more than once", ex.Message);
		}

		[Test]
		public void ParseDishes_RouteCapKeyNotInAllowedRoutes_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "{'sour':10,'salty':10}", "{'sour':10,'salty':10,'spicy':10}"));

			StringAssert.Contains("key 'spicy' is not in 'allowed_routes'", ex.Message);
		}

		[Test]
		public void ParseDishes_UnknownRouteCapKey_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "{'sour':10,'salty':10}", "{'sour':10,'salty':10,'bitter':10}"));

			StringAssert.Contains("'route_caps' has unknown route 'bitter'", ex.Message);
		}

		[Test]
		public void ParseDishes_RouteCapsContainsBalanced_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "{'sour':10,'salty':10}", "{'sour':10,'salty':10,'balanced':10}"));

			StringAssert.Contains("'route_caps' must not contain 'balanced'", ex.Message);
			StringAssert.Contains("tomato_egg_stirfry", ex.Message);
		}

		[Test]
		public void ParseDishes_ValidDish_RouteCapsHoldOnlySpecializedRoutes()
		{
			DishData dish = ParseOne(TomatoEggEntry)[0];

			foreach (string route in dish.RouteCaps.Keys)
			{
				Assert.IsTrue(DishRoutes.IsSpecialized(route), $"RouteCaps contains non-specialized route '{route}'.");
			}
		}

		[Test]
		public void ParseDishes_AllowedSpecializedRouteWithoutCap_Throws()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "{'sour':10,'salty':10}", "{'sour':10}"));

			StringAssert.Contains("missing a cap for allowed route 'salty'", ex.Message);
		}

		[TestCase(0)]
		[TestCase(-5)]
		public void ParseDishes_NonPositiveRouteCap_Throws(int cap)
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "'salty':10}", "'salty':" + cap + "}"));

			StringAssert.Contains("'route_caps.salty' must be greater than 0", ex.Message);
		}

		[Test]
		public void ParseDishes_BalancedWithSingleActiveAxis_Throws()
		{
			string entry = Replaced(TomatoEggEntry, "['sour','salty','balanced']", "['sour','balanced']");
			entry = Replaced(entry, "{'sour':10,'salty':10}", "{'sour':10}");

			var ex = ThrowsOne(entry);

			StringAssert.Contains("'balanced' requires at least 2", ex.Message);
			StringAssert.Contains("in 'allowed_routes'", ex.Message);
			StringAssert.Contains("found 1", ex.Message);
		}

		[Test]
		public void ParseDishes_BalancedOnly_Throws()
		{
			string entry = Replaced(TomatoEggEntry, "['sour','salty','balanced']", "['balanced']");
			entry = Replaced(entry, "{'sour':10,'salty':10}", "{}");

			var ex = ThrowsOne(entry);

			StringAssert.Contains("'balanced' requires at least 2", ex.Message);
			StringAssert.Contains("found 0", ex.Message);
		}

		// ---- 非法 JSON ----

		[Test]
		public void ParseDishes_WrongValueType_ThrowsWrappingNewtonsoftException()
		{
			var ex = ThrowsOne(Replaced(TomatoEggEntry, "'sour':10,", "'sour':'high',"));

			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[Test]
		public void ParseDishes_MalformedJson_ThrowsWrappingNewtonsoftException()
		{
			var ex = Assert.Throws<DefinitionException>(() => DefinitionParser.ParseDishes("[{\"id\": "));

			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[TestCase((string)null)]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("null")]
		[TestCase("\uFEFF")]
		public void ParseDishes_NullOrEmptyInput_Throws(string json)
		{
			Assert.Throws<DefinitionException>(() => DefinitionParser.ParseDishes(json));
		}
	}

	public class DishDataTests
	{
		private static DishData Create(
			IEnumerable<IEnumerable<string>> anchors = null,
			IEnumerable<string> routes = null,
			IEnumerable<KeyValuePair<string, int>> caps = null)
		{
			return new DishData(
				"tomato_egg_stirfry",
				"dish.tomato_egg_stirfry.name",
				anchors ?? new[] { new[] { "egg_family" }, new[] { "tomato_family" } },
				routes ?? new[] { "sour", "salty", "balanced" },
				caps ?? new Dictionary<string, int> { { "sour", 10 }, { "salty", 10 } },
				new FloatRange(2.5f, 6.5f),
				50,
				2);
		}

		[Test]
		public void Constructor_EmptyId_Throws()
		{
			Assert.Throws<ArgumentException>(() => new DishData(
				" ", "k", new[] { new[] { "a" } }, new[] { "sour" },
				new Dictionary<string, int> { { "sour", 10 } }, new FloatRange(0f, 1f), 1, 1));
		}

		[Test]
		public void Constructor_NullCollections_Throw()
		{
			var band = new FloatRange(0f, 1f);
			var caps = new Dictionary<string, int>();
			var routes = new[] { "sour" };
			var anchors = new[] { new[] { "a" } };

			Assert.Throws<ArgumentNullException>(() => new DishData("d", "k", null, routes, caps, band, 1, 1));
			Assert.Throws<ArgumentNullException>(() => new DishData("d", "k", anchors, null, caps, band, 1, 1));
			Assert.Throws<ArgumentNullException>(() => new DishData("d", "k", anchors, routes, null, band, 1, 1));
		}

		[Test]
		public void Constructor_NullAnchorGroup_Throws()
		{
			Assert.Throws<ArgumentException>(() => Create(anchors: new[] { new[] { "egg_family" }, null }));
		}

		[TestCase("balanced")]
		[TestCase("bitter")]
		[TestCase("Sour")]
		public void Constructor_NonSpecializedRouteCap_Throws(string route)
		{
			var caps = new Dictionary<string, int> { { "sour", 10 }, { route, 10 } };

			var ex = Assert.Throws<ArgumentException>(() => Create(caps: caps));

			StringAssert.Contains("'" + route + "'", ex.Message);
		}

		[Test]
		public void Collections_AreDefensivelyCopied()
		{
			var innerGroup = new List<string> { "egg_family" };
			var anchors = new List<List<string>> { innerGroup };
			var routes = new List<string> { "sour", "salty", "balanced" };
			var caps = new Dictionary<string, int> { { "sour", 10 }, { "salty", 10 } };

			DishData dish = Create(anchors, routes, caps);

			innerGroup.Add("mutated");
			anchors.Add(new List<string> { "mutated" });
			routes.Add("spicy");
			caps["sour"] = 99;
			caps.Add("spicy", 10);

			Assert.AreEqual(1, dish.Anchors.Count);
			Assert.AreEqual(1, dish.Anchors[0].Count);
			Assert.AreEqual(3, dish.AllowedRoutes.Count);
			Assert.AreEqual(2, dish.RouteCaps.Count);
			Assert.AreEqual(10, dish.RouteCaps["sour"]);
		}

		[Test]
		public void Collections_AreReadOnly()
		{
			DishData dish = Create();

			Assert.Throws<NotSupportedException>(() => ((IList<string>)dish.AllowedRoutes).Add("spicy"));
			Assert.Throws<NotSupportedException>(() => ((IList<string>)dish.Anchors[0]).Add("x"));
			Assert.Throws<NotSupportedException>(() => ((IDictionary<string, int>)dish.RouteCaps).Add("spicy", 10));
		}

		[Test]
		public void AllowsRoute_IsOrdinal()
		{
			DishData dish = Create();

			Assert.IsTrue(dish.AllowsRoute("sour"));
			Assert.IsFalse(dish.AllowsRoute("Sour"));
			Assert.IsFalse(dish.AllowsRoute(null));
		}
	}

	public class FloatRangeTests
	{
		[Test]
		public void Constructor_StoresBounds()
		{
			var range = new FloatRange(2.5f, 6.5f);

			Assert.AreEqual(2.5f, range.Lo);
			Assert.AreEqual(6.5f, range.Hi);
		}

		[Test]
		public void Constructor_LoGreaterThanHi_Throws()
		{
			Assert.Throws<ArgumentException>(() => new FloatRange(6.5f, 2.5f));
		}

		[Test]
		public void Constructor_NaN_Throws()
		{
			Assert.Throws<ArgumentException>(() => new FloatRange(float.NaN, 1f));
			Assert.Throws<ArgumentException>(() => new FloatRange(0f, float.NaN));
		}

		[Test]
		public void Constructor_EqualBounds_IsAllowed()
		{
			var range = new FloatRange(4f, 4f);

			Assert.IsTrue(range.Contains(4f));
		}

		[Test]
		public void Contains_IsInclusiveOnBothEnds()
		{
			var range = new FloatRange(2.5f, 6.5f);

			Assert.IsTrue(range.Contains(2.5f));
			Assert.IsTrue(range.Contains(6.5f));
			Assert.IsTrue(range.Contains(4f));
			Assert.IsFalse(range.Contains(2.49f));
			Assert.IsFalse(range.Contains(6.51f));
		}

		[Test]
		public void Equality_ComparesBothBounds()
		{
			var a = new FloatRange(2.5f, 6.5f);
			var b = new FloatRange(2.5f, 6.5f);
			var c = new FloatRange(2.5f, 7f);

			Assert.IsTrue(a == b);
			Assert.IsFalse(a != b);
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(a.Equals((object)b));
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
			Assert.IsTrue(a != c);
			Assert.IsFalse(a.Equals(c));
			Assert.IsFalse(a.Equals("not a range"));
		}

		[Test]
		public void Default_IsZeroZero()
		{
			Assert.AreEqual(new FloatRange(0f, 0f), default(FloatRange));
		}

		[Test]
		public void ToString_UsesInvariantCulture()
		{
			Assert.AreEqual("[2.5, 6.5]", new FloatRange(2.5f, 6.5f).ToString());
		}
	}
}
