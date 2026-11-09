using System;
using System.Collections.Generic;
using Kitchen.Core.Data;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class RecipeTests
	{
		[Test]
		public void Constructor_AssignsNameAndItems()
		{
			var recipe = new Recipe("酸香番茄炒蛋", new[] { "tomato", "tomato", "egg" });

			Assert.AreEqual("酸香番茄炒蛋", recipe.Name);
			CollectionAssert.AreEqual(new[] { "tomato", "tomato", "egg" }, recipe.Items);
		}

		[Test]
		public void Constructor_CopiesItems()
		{
			var source = new List<string> { "tomato", "egg" };
			var recipe = new Recipe("r", source);

			source.Add("salt");
			source[0] = "sugar";

			CollectionAssert.AreEqual(new[] { "tomato", "egg" }, recipe.Items);
		}

		[Test]
		public void Items_IsReadOnly()
		{
			var recipe = new Recipe("r", new[] { "tomato" });

			Assert.IsFalse(recipe.Items is List<string>);
			Assert.Throws<NotSupportedException>(() => ((IList<string>)recipe.Items).Add("egg"));
		}

		[Test]
		public void Constructor_EmptyNameAndItems_Allowed()
		{
			var recipe = new Recipe(string.Empty, new string[0]);

			Assert.AreEqual(string.Empty, recipe.Name);
			Assert.AreEqual(0, recipe.Items.Count);
		}

		[Test]
		public void Constructor_NullArguments_Throw()
		{
			Assert.Throws<ArgumentNullException>(() => new Recipe(null, new[] { "tomato" }));
			Assert.Throws<ArgumentNullException>(() => new Recipe("r", null));
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("  ")]
		public void Constructor_InvalidItem_Throws(string item)
		{
			var ex = Assert.Throws<ArgumentException>(() => new Recipe("r", new[] { "tomato", item }));

			StringAssert.Contains("items[1]", ex.Message);
		}
	}

	public class DishRecordTests
	{
		private static DishRecord Make(
			string dishId = "tomato_egg_stirfry",
			string route = DishRoutes.Sour,
			int tier = 4,
			string maker = "Chef_Lin",
			float score = 0.93f)
		{
			return new DishRecord(dishId, route, "S", tier, new DishAffix("sour_ingredient_bonus", 0.124f), maker, 1782541920L, score);
		}

		[Test]
		public void Constructor_AssignsAllFields()
		{
			DishRecord record = Make();

			Assert.AreEqual("tomato_egg_stirfry", record.DishId);
			Assert.AreEqual("sour", record.Route);
			Assert.AreEqual("S", record.Rank);
			Assert.AreEqual(4, record.Tier);
			Assert.AreEqual("sour_ingredient_bonus", record.Affix.Type);
			Assert.AreEqual(0.124f, record.Affix.Value);
			Assert.AreEqual("Chef_Lin", record.Maker);
			Assert.AreEqual(1782541920L, record.Timestamp);
			Assert.AreEqual(0.93f, record.Score);
			Assert.IsFalse(record.IsFailedDish);
		}

		[Test]
		public void FailedDish_NullDishIdRouteRankAffix_Allowed()
		{
			var record = new DishRecord(null, null, null, 0, null, "Chef_Lin", 0L, 0f);

			Assert.IsTrue(record.IsFailedDish);
			Assert.IsNull(record.Affix);
		}

		[TestCase("")]
		[TestCase("  ")]
		public void Constructor_BlankDishId_Throws(string dishId)
		{
			Assert.Throws<ArgumentException>(() => Make(dishId: dishId));
		}

		[TestCase("bitter")]
		[TestCase("Sour")]
		public void Constructor_InvalidRoute_Throws(string route)
		{
			Assert.Throws<ArgumentException>(() => Make(route: route));
		}

		[Test]
		public void Constructor_BalancedRoute_Allowed()
		{
			Assert.AreEqual(DishRoutes.Balanced, Make(route: DishRoutes.Balanced).Route);
		}

		[Test]
		public void Constructor_NegativeTier_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => Make(tier: -1));
		}

		[Test]
		public void Constructor_NullMaker_Throws()
		{
			Assert.Throws<ArgumentNullException>(() => Make(maker: null));
		}

		[TestCase(float.NaN)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NegativeInfinity)]
		public void Constructor_NonFiniteScore_Throws(float score)
		{
			Assert.Throws<ArgumentException>(() => Make(score: score));
		}

		[Test]
		public void Affix_InvalidArguments_Throw()
		{
			Assert.Throws<ArgumentException>(() => new DishAffix(null, 0.1f));
			Assert.Throws<ArgumentException>(() => new DishAffix(" ", 0.1f));
			Assert.Throws<ArgumentException>(() => new DishAffix("gold_bonus", float.NaN));
		}
	}

	public class EventPayloadTests
	{
		[Test]
		public void GoldChanged_AssignsFields()
		{
			var payload = new GoldChangedPayload(500, -12);

			Assert.AreEqual(500L, payload.NewValue);
			Assert.AreEqual(-12L, payload.Delta);
		}

		[Test]
		public void InventoryChanged_Validates()
		{
			var payload = new InventoryChangedPayload("egg", 0);
			Assert.AreEqual("egg", payload.IngredientId);
			Assert.AreEqual(0, payload.NewCount);

			Assert.Throws<ArgumentException>(() => new InventoryChangedPayload(null, 1));
			Assert.Throws<ArgumentException>(() => new InventoryChangedPayload("", 1));
			Assert.Throws<ArgumentOutOfRangeException>(() => new InventoryChangedPayload("egg", -1));
		}

		[Test]
		public void UpgradePurchased_Validates()
		{
			var payload = new UpgradePurchasedPayload("pot_speed", 2);
			Assert.AreEqual("pot_speed", payload.UpgradeId);
			Assert.AreEqual(2, payload.NewLevel);

			Assert.Throws<ArgumentException>(() => new UpgradePurchasedPayload(" ", 1));
			Assert.Throws<ArgumentOutOfRangeException>(() => new UpgradePurchasedPayload("pot_speed", -1));
		}

		[Test]
		public void RecordShelved_Validates()
		{
			var payload = new RecordShelvedPayload(0, 2);
			Assert.AreEqual(0, payload.RecordIndex);
			Assert.AreEqual(2, payload.Slot);

			Assert.Throws<ArgumentOutOfRangeException>(() => new RecordShelvedPayload(-1, 0));
			Assert.Throws<ArgumentOutOfRangeException>(() => new RecordShelvedPayload(0, -1));
		}

		[Test]
		public void SaveRequested_Validates()
		{
			Assert.AreEqual("quit", new SaveRequestedPayload("quit").Reason);
			Assert.AreEqual(string.Empty, new SaveRequestedPayload(string.Empty).Reason);
			Assert.Throws<ArgumentNullException>(() => new SaveRequestedPayload(null));
		}

		[Test]
		public void OrderState_DefaultIsIdle()
		{
			Assert.AreEqual(OrderState.Idle, default(OrderState));
			Assert.AreEqual(5, Enum.GetValues(typeof(OrderState)).Length);
		}
	}
}
