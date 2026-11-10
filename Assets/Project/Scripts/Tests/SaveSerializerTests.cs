using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Kitchen.Core.Data;
using Kitchen.Core.Save;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class SaveSerializerTests
	{
		// 測試用 JSON 以單引號撰寫，再換成雙引號，避免大量跳脫
		private static string J(string singleQuoted) => singleQuoted.Replace('\'', '"');

		private static readonly string Bom = new string((char)0xFEFF, 1);

		private const string RecordEntry =
			"{'dish_id':'tomato_egg_stirfry','route':'sour','rank':'S','tier':4," +
			"'affix':{'type':'sour_ingredient_bonus','value':0.124}," +
			"'maker':'Chef_Lin','timestamp':1782541920,'score':0.93}";

		// 技術文件 4.4 範例（records 換成一筆合法 DishRecord）
		private static readonly string SpecExample = J(
			"{'version':1,'last_active':1782541920,'nickname':'Chef_Lin','gold':500," +
			"'fridge_inventory':{'tomato':4,'egg':2}," +
			"'fridge_capacity':50," +
			"'upgrades':{'fridge_speed':3,'board_speed':2,'pot_speed':1}," +
			"'fridge_slots':[{'type':'random'},{'type':'category','value':'vegetable'}]," +
			"'records':[" + RecordEntry + "]," +
			"'compendium':{'tomato_egg_stirfry':{'sour':'S','balanced':'A'}}," +
			"'shelf':[0,3,null]," +
			"'recipes':[{'name':'酸香番茄炒蛋','items':['tomato','tomato','tomato','egg','egg','sugar','salt']}]," +
			"'active_recipe':0," +
			"'auto_cook_enabled':true," +
			"'chain_state':{'order_remaining':['salt'],'board_progress':0,'pot_contents':['tomato','tomato','egg'],'cook_progress':62}," +
			"'stats':{'total_dishes':42,'ss_count':1}}");

		private static SaveData CreateFullSave()
		{
			var data = new SaveData
			{
				LastActive = 1782541920L,
				Nickname = "Chef_Lin",
				Gold = 5000000000L, // 超過 int 範圍，確認 long 往返
				FridgeCapacity = 50,
				ActiveRecipe = 1,
				AutoCookEnabled = true,
			};
			data.FridgeInventory.Add("tomato", 4);
			data.FridgeInventory.Add("egg", 2);
			data.Upgrades.Add("fridge_speed", 3);
			data.Upgrades.Add("pot_speed", 1);
			data.FridgeSlots.Add(new FridgeSlotState("random"));
			data.FridgeSlots.Add(new FridgeSlotState("category", "vegetable"));
			data.Records.Add(new DishRecord("tomato_egg_stirfry", "sour", "S", 4,
				new DishAffix("sour_ingredient_bonus", 0.124f), "Chef_Lin", 1782541920L, 0.93f));
			data.Records.Add(new DishRecord("tomato_egg_stirfry", "balanced", "A", 0, null, "Chef_Lin", 1782541999L, 0.71f));
			data.Records.Add(new DishRecord(null, null, null, 0, null, "Chef_Lin", 1782542000L, 0f)); // 失敗料理
			data.Compendium.Add("tomato_egg_stirfry", new Dictionary<string, string> { { "sour", "S" }, { "balanced", "A" } });
			data.Compendium.Add("other_dish", new Dictionary<string, string>());
			data.Shelf.Add(0);
			data.Shelf.Add(null);
			data.Shelf.Add(2);
			data.Recipes.Add(new Recipe("酸香番茄炒蛋", new[] { "tomato", "tomato", "egg" }));
			data.Recipes.Add(new Recipe("", new string[0]));
			data.ChainState.OrderRemaining.Add("salt");
			data.ChainState.BoardProgress = 0.25f;
			data.ChainState.PotContents.Add("tomato");
			data.ChainState.PotContents.Add("egg");
			data.ChainState.CookProgress = 62.5f;
			data.Stats.TotalDishes = 42;
			data.Stats.SsCount = 1;
			return data;
		}

		private static void AssertSaveEqual(SaveData expected, SaveData actual)
		{
			Assert.AreEqual(expected.Version, actual.Version, "version");
			Assert.AreEqual(expected.LastActive, actual.LastActive, "last_active");
			Assert.AreEqual(expected.Nickname, actual.Nickname, "nickname");
			Assert.AreEqual(expected.Gold, actual.Gold, "gold");
			CollectionAssert.AreEquivalent(expected.FridgeInventory, actual.FridgeInventory, "fridge_inventory");
			Assert.AreEqual(expected.FridgeCapacity, actual.FridgeCapacity, "fridge_capacity");
			CollectionAssert.AreEquivalent(expected.Upgrades, actual.Upgrades, "upgrades");

			Assert.AreEqual(expected.FridgeSlots.Count, actual.FridgeSlots.Count, "fridge_slots count");
			for (int i = 0; i < expected.FridgeSlots.Count; i++)
			{
				Assert.AreEqual(expected.FridgeSlots[i].Type, actual.FridgeSlots[i].Type, $"fridge_slots[{i}].type");
				Assert.AreEqual(expected.FridgeSlots[i].Value, actual.FridgeSlots[i].Value, $"fridge_slots[{i}].value");
			}

			Assert.AreEqual(expected.Records.Count, actual.Records.Count, "records count");
			for (int i = 0; i < expected.Records.Count; i++)
			{
				AssertRecordEqual(expected.Records[i], actual.Records[i], $"records[{i}]");
			}

			Assert.AreEqual(expected.Compendium.Count, actual.Compendium.Count, "compendium count");
			foreach (KeyValuePair<string, Dictionary<string, string>> dish in expected.Compendium)
			{
				Assert.IsTrue(actual.Compendium.ContainsKey(dish.Key), $"compendium missing '{dish.Key}'");
				CollectionAssert.AreEquivalent(dish.Value, actual.Compendium[dish.Key], $"compendium.{dish.Key}");
			}

			CollectionAssert.AreEqual(expected.Shelf, actual.Shelf, "shelf");

			Assert.AreEqual(expected.Recipes.Count, actual.Recipes.Count, "recipes count");
			for (int i = 0; i < expected.Recipes.Count; i++)
			{
				Assert.AreEqual(expected.Recipes[i].Name, actual.Recipes[i].Name, $"recipes[{i}].name");
				CollectionAssert.AreEqual(expected.Recipes[i].Items, actual.Recipes[i].Items, $"recipes[{i}].items");
			}

			Assert.AreEqual(expected.ActiveRecipe, actual.ActiveRecipe, "active_recipe");
			Assert.AreEqual(expected.AutoCookEnabled, actual.AutoCookEnabled, "auto_cook_enabled");

			CollectionAssert.AreEqual(expected.ChainState.OrderRemaining, actual.ChainState.OrderRemaining, "chain_state.order_remaining");
			Assert.AreEqual(expected.ChainState.BoardProgress, actual.ChainState.BoardProgress, "chain_state.board_progress");
			CollectionAssert.AreEqual(expected.ChainState.PotContents, actual.ChainState.PotContents, "chain_state.pot_contents");
			Assert.AreEqual(expected.ChainState.CookProgress, actual.ChainState.CookProgress, "chain_state.cook_progress");

			Assert.AreEqual(expected.Stats.TotalDishes, actual.Stats.TotalDishes, "stats.total_dishes");
			Assert.AreEqual(expected.Stats.SsCount, actual.Stats.SsCount, "stats.ss_count");
		}

		private static void AssertRecordEqual(DishRecord expected, DishRecord actual, string label)
		{
			Assert.AreEqual(expected.DishId, actual.DishId, label + ".dish_id");
			Assert.AreEqual(expected.Route, actual.Route, label + ".route");
			Assert.AreEqual(expected.Rank, actual.Rank, label + ".rank");
			Assert.AreEqual(expected.Tier, actual.Tier, label + ".tier");
			if (expected.Affix == null)
			{
				Assert.IsNull(actual.Affix, label + ".affix");
			}
			else
			{
				Assert.IsNotNull(actual.Affix, label + ".affix");
				Assert.AreEqual(expected.Affix.Type, actual.Affix.Type, label + ".affix.type");
				Assert.AreEqual(expected.Affix.Value, actual.Affix.Value, label + ".affix.value");
			}
			Assert.AreEqual(expected.Maker, actual.Maker, label + ".maker");
			Assert.AreEqual(expected.Timestamp, actual.Timestamp, label + ".timestamp");
			Assert.AreEqual(expected.Score, actual.Score, label + ".score");
		}

		private static void AssertIsEmptyDefault(SaveData data)
		{
			Assert.AreEqual(SaveSerializer.CurrentVersion, data.Version);
			Assert.AreEqual(0L, data.LastActive);
			Assert.AreEqual(string.Empty, data.Nickname);
			Assert.AreEqual(0L, data.Gold);
			Assert.AreEqual(0, data.FridgeCapacity);
			Assert.IsNull(data.ActiveRecipe);
			Assert.IsFalse(data.AutoCookEnabled);
			Assert.IsNotNull(data.FridgeInventory);
			Assert.AreEqual(0, data.FridgeInventory.Count);
			Assert.AreEqual(0, data.Upgrades.Count);
			Assert.AreEqual(0, data.FridgeSlots.Count);
			Assert.AreEqual(0, data.Records.Count);
			Assert.AreEqual(0, data.Compendium.Count);
			Assert.AreEqual(0, data.Shelf.Count);
			Assert.AreEqual(0, data.Recipes.Count);
			Assert.IsNotNull(data.ChainState);
			Assert.AreEqual(0, data.ChainState.OrderRemaining.Count);
			Assert.AreEqual(0, data.ChainState.PotContents.Count);
			Assert.AreEqual(0f, data.ChainState.BoardProgress);
			Assert.AreEqual(0f, data.ChainState.CookProgress);
			Assert.IsNotNull(data.Stats);
			Assert.AreEqual(0, data.Stats.TotalDishes);
			Assert.AreEqual(0, data.Stats.SsCount);
		}

		// ---- SaveData 預設 ----

		[Test]
		public void NewSaveData_HasCurrentVersionAndNonNullCollections()
		{
			AssertIsEmptyDefault(new SaveData());
		}

		// ---- 往返 ----

		[Test]
		public void RoundTrip_FullSave_AllFieldsEqual()
		{
			SaveData original = CreateFullSave();

			SaveData loaded = SaveSerializer.Deserialize(SaveSerializer.Serialize(original));

			AssertSaveEqual(original, loaded);
		}

		[Test]
		public void RoundTrip_EmptySave_AllFieldsEqual()
		{
			SaveData loaded = SaveSerializer.Deserialize(SaveSerializer.Serialize(new SaveData()));

			AssertIsEmptyDefault(loaded);
		}

		[Test]
		public void RoundTrip_TwiceProducesIdenticalJson()
		{
			string first = SaveSerializer.Serialize(CreateFullSave());
			string second = SaveSerializer.Serialize(SaveSerializer.Deserialize(first));

			Assert.AreEqual(first, second);
		}

		[Test]
		public void RoundTrip_NullActiveRecipe_StaysNull()
		{
			SaveData original = CreateFullSave();
			original.ActiveRecipe = null;

			string json = SaveSerializer.Serialize(original);
			SaveData loaded = SaveSerializer.Deserialize(json);

			Assert.IsNull(loaded.ActiveRecipe);
			Assert.AreEqual(JTokenType.Null, JObject.Parse(json)["active_recipe"].Type);
		}

		[Test]
		public void RoundTrip_DateLikeNickname_StaysString()
		{
			SaveData original = new SaveData { Nickname = "2026-01-01T00:00:00" };

			SaveData loaded = SaveSerializer.Deserialize(SaveSerializer.Serialize(original));

			Assert.AreEqual("2026-01-01T00:00:00", loaded.Nickname);
		}

		// ---- 序列化格式 ----

		[Test]
		public void Serialize_UsesSnakeCaseKeys()
		{
			JObject root = JObject.Parse(SaveSerializer.Serialize(CreateFullSave()));

			CollectionAssert.AreEquivalent(
				new[]
				{
					"version", "last_active", "nickname", "gold", "fridge_inventory", "fridge_capacity", "upgrades",
					"fridge_slots", "records", "compendium", "shelf", "recipes", "active_recipe", "auto_cook_enabled",
					"chain_state", "stats",
				},
				root.Properties().Select(p => p.Name).ToArray());

			CollectionAssert.AreEquivalent(
				new[] { "dish_id", "route", "rank", "tier", "affix", "maker", "timestamp", "score" },
				((JObject)root["records"][0]).Properties().Select(p => p.Name).ToArray());
			CollectionAssert.AreEquivalent(
				new[] { "type", "value" },
				((JObject)root["records"][0]["affix"]).Properties().Select(p => p.Name).ToArray());
			CollectionAssert.AreEquivalent(
				new[] { "name", "items" },
				((JObject)root["recipes"][0]).Properties().Select(p => p.Name).ToArray());
			CollectionAssert.AreEquivalent(
				new[] { "order_remaining", "board_progress", "pot_contents", "cook_progress" },
				((JObject)root["chain_state"]).Properties().Select(p => p.Name).ToArray());
			CollectionAssert.AreEquivalent(
				new[] { "total_dishes", "ss_count" },
				((JObject)root["stats"]).Properties().Select(p => p.Name).ToArray());
		}

		[Test]
		public void Serialize_WritesCurrentVersion()
		{
			JObject root = JObject.Parse(SaveSerializer.Serialize(new SaveData()));

			Assert.AreEqual(SaveSerializer.CurrentVersion, (int)root["version"]);
		}

		[Test]
		public void Serialize_FridgeSlotWithoutValue_OmitsValueKey()
		{
			JObject root = JObject.Parse(SaveSerializer.Serialize(CreateFullSave()));

			Assert.IsNull(root["fridge_slots"][0]["value"]);
			Assert.AreEqual("vegetable", (string)root["fridge_slots"][1]["value"]);
		}

		[Test]
		public void Serialize_Null_ThrowsArgumentNull()
		{
			Assert.Throws<ArgumentNullException>(() => SaveSerializer.Serialize(null));
		}

		[Test]
		public void Serialize_NullRecordEntry_Throws()
		{
			SaveData data = new SaveData();
			data.Records.Add(null);

			Assert.Throws<SaveException>(() => SaveSerializer.Serialize(data));
		}

		// ---- 4.4 範例 ----

		[Test]
		public void Deserialize_SpecExample_MapsAllFields()
		{
			SaveData data = SaveSerializer.Deserialize(SpecExample);

			Assert.AreEqual(1, data.Version);
			Assert.AreEqual(1782541920L, data.LastActive);
			Assert.AreEqual("Chef_Lin", data.Nickname);
			Assert.AreEqual(500L, data.Gold);
			Assert.AreEqual(4, data.FridgeInventory["tomato"]);
			Assert.AreEqual(2, data.FridgeInventory["egg"]);
			Assert.AreEqual(50, data.FridgeCapacity);
			Assert.AreEqual(3, data.Upgrades["fridge_speed"]);
			Assert.AreEqual(2, data.Upgrades["board_speed"]);
			Assert.AreEqual(1, data.Upgrades["pot_speed"]);

			Assert.AreEqual(2, data.FridgeSlots.Count);
			Assert.AreEqual("random", data.FridgeSlots[0].Type);
			Assert.IsNull(data.FridgeSlots[0].Value);
			Assert.AreEqual("category", data.FridgeSlots[1].Type);
			Assert.AreEqual("vegetable", data.FridgeSlots[1].Value);

			Assert.AreEqual(1, data.Records.Count);
			AssertRecordEqual(
				new DishRecord("tomato_egg_stirfry", "sour", "S", 4, new DishAffix("sour_ingredient_bonus", 0.124f), "Chef_Lin", 1782541920L, 0.93f),
				data.Records[0],
				"records[0]");

			Assert.AreEqual("S", data.Compendium["tomato_egg_stirfry"]["sour"]);
			Assert.AreEqual("A", data.Compendium["tomato_egg_stirfry"]["balanced"]);

			CollectionAssert.AreEqual(new int?[] { 0, 3, null }, data.Shelf);

			Assert.AreEqual(1, data.Recipes.Count);
			Assert.AreEqual("酸香番茄炒蛋", data.Recipes[0].Name);
			CollectionAssert.AreEqual(new[] { "tomato", "tomato", "tomato", "egg", "egg", "sugar", "salt" }, data.Recipes[0].Items);

			Assert.AreEqual(0, data.ActiveRecipe);
			Assert.IsTrue(data.AutoCookEnabled);

			CollectionAssert.AreEqual(new[] { "salt" }, data.ChainState.OrderRemaining);
			Assert.AreEqual(0f, data.ChainState.BoardProgress);
			CollectionAssert.AreEqual(new[] { "tomato", "tomato", "egg" }, data.ChainState.PotContents);
			Assert.AreEqual(62f, data.ChainState.CookProgress);

			Assert.AreEqual(42, data.Stats.TotalDishes);
			Assert.AreEqual(1, data.Stats.SsCount);
		}

		[Test]
		public void Deserialize_UnknownExtraField_IsIgnored()
		{
			string json = SpecExample.Replace("\"gold\":500,", "\"gold\":500,\"future_field\":{\"x\":1},");

			SaveData data = SaveSerializer.Deserialize(json);

			Assert.AreEqual(500L, data.Gold);
		}

		// ---- 缺欄位補預設 ----

		[Test]
		public void Deserialize_EmptyObject_FillsDefaults()
		{
			AssertIsEmptyDefault(SaveSerializer.Deserialize("{}"));
		}

		[Test]
		public void Deserialize_MissingVersion_TreatedAsFirstVersion()
		{
			SaveData data = SaveSerializer.Deserialize(J("{'gold':7}"));

			Assert.AreEqual(SaveSerializer.CurrentVersion, data.Version);
			Assert.AreEqual(7L, data.Gold);
		}

		[Test]
		public void Deserialize_MissingSomeFields_KeepsPresentOnesAndDefaultsOthers()
		{
			SaveData data = SaveSerializer.Deserialize(J("{'version':1,'nickname':'Chef_Lin','fridge_capacity':50,'stats':{'total_dishes':3}}"));

			Assert.AreEqual("Chef_Lin", data.Nickname);
			Assert.AreEqual(50, data.FridgeCapacity);
			Assert.AreEqual(3, data.Stats.TotalDishes);
			Assert.AreEqual(0, data.Stats.SsCount);
			Assert.AreEqual(0L, data.Gold);
			Assert.AreEqual(0, data.Records.Count);
			Assert.AreEqual(0, data.ChainState.PotContents.Count);
			Assert.IsNull(data.ActiveRecipe);
		}

		[Test]
		public void Deserialize_NullCollections_BecomeEmpty()
		{
			string json = J(
				"{'version':1,'nickname':null,'fridge_inventory':null,'upgrades':null,'fridge_slots':null,'records':null," +
				"'compendium':null,'shelf':null,'recipes':null,'active_recipe':null,'chain_state':null,'stats':null}");

			AssertIsEmptyDefault(SaveSerializer.Deserialize(json));
		}

		[Test]
		public void Deserialize_ChainStateMissingInnerFields_FillsDefaults()
		{
			SaveData data = SaveSerializer.Deserialize(J("{'chain_state':{'cook_progress':5}}"));

			Assert.AreEqual(5f, data.ChainState.CookProgress);
			Assert.AreEqual(0f, data.ChainState.BoardProgress);
			Assert.IsNotNull(data.ChainState.OrderRemaining);
			Assert.AreEqual(0, data.ChainState.OrderRemaining.Count);
		}

		// ---- 版本 ----

		[Test]
		public void Deserialize_VersionNewerThanCurrent_Throws()
		{
			int newer = SaveSerializer.CurrentVersion + 1;

			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"version\":" + newer + "}"));

			StringAssert.Contains("newer", ex.Message);
			StringAssert.Contains(newer.ToString(), ex.Message);
		}

		[Test]
		public void Deserialize_HugeVersion_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"version\":99999999999999999999999}"));
		}

		[Test]
		public void Deserialize_VersionZero_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"version\":0}"));
		}

		[Test]
		public void Deserialize_NonIntegerVersion_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"version\":\"1\"}"));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"version\":1.5}"));
		}

		// ---- 非法 JSON / 內容 ----

		[Test]
		public void Deserialize_InvalidJson_ThrowsWithInnerJsonException()
		{
			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"version\":1,"));

			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[Test]
		public void Deserialize_NullOrWhitespace_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(null));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(""));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("   "));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(Bom));
		}

		[Test]
		public void Deserialize_TopLevelNotObject_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("[]"));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("null"));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("42"));
		}

		[Test]
		public void Deserialize_TrailingContent_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{} {}"));
		}

		[Test]
		public void Deserialize_WrongFieldType_Throws()
		{
			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'gold':'lots'}")));

			Assert.IsInstanceOf<JsonException>(ex.InnerException);
		}

		[Test]
		public void Deserialize_InvalidRecord_ThrowsWithIndex()
		{
			// route 非法 → DishRecord 建構子拒絕
			string json = J("{'records':[" + RecordEntry + "," + RecordEntry.Replace("'route':'sour'", "'route':'umami'") + "]}");

			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));

			StringAssert.Contains("records[1]", ex.Message);
			Assert.IsInstanceOf<ArgumentException>(ex.InnerException);
		}

		[Test]
		public void Deserialize_RecordMissingMaker_Throws()
		{
			string json = J("{'records':[" + RecordEntry.Replace("'maker':'Chef_Lin',", "") + "]}");

			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));
		}

		[Test]
		public void Deserialize_NullRecordEntry_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'records':[null]}")));
		}

		[Test]
		public void Deserialize_RecipeMissingItems_Throws()
		{
			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'recipes':[{'name':'x'}]}")));

			StringAssert.Contains("recipes[0]", ex.Message);
		}

		[Test]
		public void Deserialize_FridgeSlotMissingType_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'fridge_slots':[{'value':'vegetable'}]}")));
		}

		[Test]
		public void Deserialize_CompendiumEmptyRank_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'compendium':{'d':{'sour':''}}}")));
		}

		// ---- 存檔端拒絕載入會拒絕的狀態（M1）----

		[TestCase("")]
		[TestCase("   ")]
		[TestCase(null)]
		public void Serialize_ChainStateEmptyId_Throws(string badId)
		{
			SaveData orderData = new SaveData();
			orderData.ChainState.OrderRemaining.Add(badId);
			SaveData potData = new SaveData();
			potData.ChainState.PotContents.Add("tomato");
			potData.ChainState.PotContents.Add(badId);

			var orderEx = Assert.Throws<SaveException>(() => SaveSerializer.Serialize(orderData));
			var potEx = Assert.Throws<SaveException>(() => SaveSerializer.Serialize(potData));

			StringAssert.Contains("chain_state.order_remaining[0]", orderEx.Message);
			StringAssert.Contains("chain_state.pot_contents[1]", potEx.Message);
		}

		[Test]
		public void Deserialize_ChainStateEmptyId_Throws()
		{
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'chain_state':{'order_remaining':['']}}")));
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'chain_state':{'pot_contents':[null]}}")));
		}

		[TestCase("")]
		[TestCase("  ")]
		[TestCase(null)]
		public void Serialize_CompendiumEmptyRank_Throws(string badRank)
		{
			SaveData data = new SaveData();
			data.Compendium.Add("tomato_egg_stirfry", new Dictionary<string, string> { { "sour", badRank } });

			Assert.Throws<SaveException>(() => SaveSerializer.Serialize(data));
		}

		[Test]
		public void Serialize_CompendiumNullInner_Throws()
		{
			SaveData data = new SaveData();
			data.Compendium.Add("tomato_egg_stirfry", null);

			Assert.Throws<SaveException>(() => SaveSerializer.Serialize(data));
		}

		[TestCase("umami")]
		[TestCase("bitter")]
		[TestCase("Sour")]
		public void Serialize_CompendiumInvalidRoute_Throws(string route)
		{
			SaveData data = new SaveData();
			data.Compendium.Add("tomato_egg_stirfry", new Dictionary<string, string> { { route, "S" } });

			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Serialize(data));

			StringAssert.Contains(route, ex.Message);
		}

		[TestCase("umami")]
		[TestCase("bitter")]
		[TestCase("Sour")]
		public void Deserialize_CompendiumInvalidRoute_Throws(string route)
		{
			string json = J("{'compendium':{'tomato_egg_stirfry':{'sour':'S','" + route + "':'A'}}}");

			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));

			StringAssert.Contains(route, ex.Message);
		}

		[Test]
		public void Serialize_NonFiniteProgress_Throws()
		{
			SaveData data = new SaveData();
			data.ChainState.CookProgress = float.NaN;

			Assert.Throws<SaveException>(() => SaveSerializer.Serialize(data));
		}

		[Test]
		public void Serialize_RejectedState_DoesNotProduceUnreadableSave()
		{
			// 對稱性：只要 Serialize 成功，Deserialize 必定成功
			SaveData data = CreateFullSave();
			data.ChainState.PotContents.Add(" ");

			Assert.Throws<SaveException>(() => SaveSerializer.Serialize(data));
		}

		// ---- 非預期例外統一包成 SaveException（M2）----

		[TestCase("{\"fridge_capacity\":99999999999999999999999}")]
		[TestCase("{\"stats\":{\"total_dishes\":99999999999999999999999}}")]
		[TestCase("{\"gold\":99999999999999999999999}")]
		[TestCase("{\"fridge_capacity\":3000000000}")]
		public void Deserialize_HugeIntField_ThrowsSaveException(string json)
		{
			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));

			Assert.IsNotNull(ex.InnerException);
		}

		// ---- 紀錄欄位必填（Required）----

		[Test]
		public void Deserialize_RecordMissingScore_Throws()
		{
			string json = J("{'records':[" + RecordEntry.Replace(",'score':0.93", "") + "]}");

			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));
		}

		[Test]
		public void Deserialize_ContentError_IsNotReportedAsInvalidJson()
		{
			string missingScore = J("{'records':[" + RecordEntry.Replace(",'score':0.93", "") + "]}");

			var requiredEx = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(missingScore));
			var typeEx = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(J("{'gold':'lots'}")));

			StringAssert.DoesNotContain("invalid JSON", requiredEx.Message);
			StringAssert.Contains("could not be read", requiredEx.Message);
			StringAssert.DoesNotContain("invalid JSON", typeEx.Message);
		}

		[Test]
		public void Deserialize_MalformedJson_IsReportedAsInvalidJson()
		{
			var ex = Assert.Throws<SaveException>(() => SaveSerializer.Deserialize("{\"gold\":"));

			StringAssert.Contains("invalid JSON", ex.Message);
		}

		[Test]
		public void RoundTrip_DollarPrefixedDictionaryKeys_ArePreserved()
		{
			SaveData original = new SaveData();
			original.FridgeInventory.Add("$ref", 1);
			original.FridgeInventory.Add("$id", 2);
			original.FridgeInventory.Add("$type", 3);
			original.Upgrades.Add("$id", 4);

			SaveData loaded = SaveSerializer.Deserialize(SaveSerializer.Serialize(original));

			CollectionAssert.AreEquivalent(original.FridgeInventory, loaded.FridgeInventory);
			CollectionAssert.AreEquivalent(original.Upgrades, loaded.Upgrades);
		}

		[Test]
		public void Deserialize_RecordMissingDishId_ThrowsInsteadOfFailedDish()
		{
			string json = J("{'records':[" + RecordEntry.Replace("'dish_id':'tomato_egg_stirfry',", "") + "]}");

			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));
		}

		[Test]
		public void Deserialize_RecordExplicitNullDishId_IsFailedDish()
		{
			string json = J("{'records':[{'dish_id':null,'route':null,'rank':null,'tier':0,'affix':null,'maker':'Chef_Lin','timestamp':1,'score':0}]}");

			SaveData data = SaveSerializer.Deserialize(json);

			Assert.IsTrue(data.Records[0].IsFailedDish);
			Assert.IsNull(data.Records[0].Affix);
		}

		[TestCase("'tier':4,")]
		[TestCase("'timestamp':1782541920,")]
		[TestCase("'affix':{'type':'sour_ingredient_bonus','value':0.124},")]
		[TestCase("'route':'sour',")]
		public void Deserialize_RecordMissingRequiredField_Throws(string removed)
		{
			string json = J("{'records':[" + RecordEntry.Replace(removed, "") + "]}");

			Assert.AreNotEqual(RecordEntry, RecordEntry.Replace(removed, ""), "test setup: field not found");
			Assert.Throws<SaveException>(() => SaveSerializer.Deserialize(json));
		}

		// ---- 文化設定 ----

		[Test]
		public void RoundTrip_UnderGermanCulture_UsesInvariantNumbers()
		{
			CultureInfo german;
			try
			{
				german = new CultureInfo("de-DE");
			}
			catch (CultureNotFoundException)
			{
				Assert.Ignore("de-DE culture is not available in this runtime.");
				return;
			}

			CultureInfo previous = CultureInfo.CurrentCulture;
			try
			{
				CultureInfo.CurrentCulture = german;
				SaveData original = CreateFullSave();

				string json = SaveSerializer.Serialize(original);
				SaveData loaded = SaveSerializer.Deserialize(json);

				StringAssert.Contains("0.93", json);
				StringAssert.Contains("62.5", json);
				StringAssert.DoesNotContain("0,93", json);
				AssertSaveEqual(original, loaded);
				Assert.AreEqual(0.93f, SaveSerializer.Deserialize(SpecExample).Records[0].Score);
			}
			finally
			{
				CultureInfo.CurrentCulture = previous;
			}
		}

		[Test]
		public void CurrentVersion_MatchesSaveDataConstant()
		{
			Assert.AreEqual(SaveData.CurrentVersion, SaveSerializer.CurrentVersion);
			Assert.AreEqual(SaveData.CurrentVersion, new SaveData().Version);
		}

		// ---- BOM ----

		[Test]
		public void Deserialize_LeadingUtf8Bom_IsStripped()
		{
			SaveData data = SaveSerializer.Deserialize(Bom + SpecExample);

			Assert.AreEqual("Chef_Lin", data.Nickname);
			Assert.AreEqual(1, data.Records.Count);
		}

		// ---- 全域設定隔離 ----

		[Test]
		public void SerializeAndDeserialize_IgnoreGlobalJsonConvertDefaultSettings()
		{
			Func<JsonSerializerSettings> previous = JsonConvert.DefaultSettings;
			try
			{
				JsonConvert.DefaultSettings = JsonTestUtil.HostileDefaultSettings;
				SaveData original = CreateFullSave();

				string json = SaveSerializer.Serialize(original);
				SaveData loaded = SaveSerializer.Deserialize(json);
				SaveData fromSpec = SaveSerializer.Deserialize(SpecExample.Replace("\"gold\":500,", "\"gold\":500,\"future_field\":1,"));

				AssertSaveEqual(original, loaded);
				Assert.AreEqual(500L, fromSpec.Gold);
			}
			finally
			{
				JsonConvert.DefaultSettings = previous;
			}
		}
	}
}
