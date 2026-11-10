using System;
using Kitchen.Core.Data;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class FridgeParamsTests
	{
		private const float Tolerance = 1e-5f;

		private static FridgeParams Default() => new FridgeParams(10f, 0.05f);

		// ---- 建構子 ----

		[Test]
		public void Constructor_StoresValues()
		{
			var p = new FridgeParams(8f, 0.1f);

			Assert.AreEqual(8f, p.ProduceIntervalSeconds);
			Assert.AreEqual(0.1f, p.SpeedBonusPerLevel);
		}

		[TestCase(0f)]
		[TestCase(-1f)]
		[TestCase(float.NaN)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NegativeInfinity)]
		public void Constructor_InvalidInterval_Throws(float interval)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new FridgeParams(interval, 0.05f));
		}

		[TestCase(-0.01f)]
		[TestCase(float.NaN)]
		[TestCase(float.PositiveInfinity)]
		[TestCase(float.NegativeInfinity)]
		public void Constructor_InvalidSpeedBonus_Throws(float bonus)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new FridgeParams(10f, bonus));
		}

		[Test]
		public void Constructor_ZeroSpeedBonus_IsAllowed()
		{
			Assert.DoesNotThrow(() => new FridgeParams(10f, 0f));
		}

		[Test]
		public void Constructor_SmallestValidInterval_IsAllowedWithZeroBonus()
		{
			Assert.DoesNotThrow(() => new FridgeParams(float.Epsilon, 0f));
		}

		[Test]
		public void Constructor_HugeFiniteBonus_ThatOverflowsMultiplier_Throws()
		{
			// int.MaxValue × 1e30 溢位成 Infinity → 間隔會變 0
			Assert.Throws<ArgumentOutOfRangeException>(() => new FridgeParams(10f, 1e30f));
		}

		[Test]
		public void Constructor_TinyIntervalWithBonus_ThatUnderflowsInterval_Throws()
		{
			// float.Epsilon / (1 + int.MaxValue × 1) 下溢成 0
			Assert.Throws<ArgumentOutOfRangeException>(() => new FridgeParams(float.Epsilon, 1f));
		}

		// ---- SpeedMultiplier ----

		[Test]
		public void SpeedMultiplier_LevelZero_IsExactlyOne()
		{
			// 新冰箱 = 1.0 倍（不重現舊 Runtime 的 off-by-one 1.05 倍）
			Assert.AreEqual(1f, Default().SpeedMultiplier(0));
		}

		[TestCase(1, 1.05f)]
		[TestCase(4, 1.2f)]
		[TestCase(20, 2f)]
		public void SpeedMultiplier_IsOnePlusLevelTimesBonus(int level, float expected)
		{
			Assert.AreEqual(expected, Default().SpeedMultiplier(level), Tolerance);
		}

		[Test]
		public void SpeedMultiplier_UsesBonusParameter()
		{
			var p = new FridgeParams(10f, 0.25f);

			Assert.AreEqual(1.5f, p.SpeedMultiplier(2), Tolerance);
		}

		[Test]
		public void SpeedMultiplier_ZeroBonus_IsAlwaysOne()
		{
			var p = new FridgeParams(10f, 0f);

			Assert.AreEqual(1f, p.SpeedMultiplier(0));
			Assert.AreEqual(1f, p.SpeedMultiplier(100));
		}

		[Test]
		public void SpeedMultiplier_MaxLevel_IsFinite()
		{
			float m = Default().SpeedMultiplier(int.MaxValue);

			Assert.IsFalse(float.IsInfinity(m));
			Assert.Greater(m, 1f);
		}

		[Test]
		public void SpeedMultiplier_NegativeLevel_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => Default().SpeedMultiplier(-1));
		}

		// ---- EffectiveInterval ----

		[Test]
		public void EffectiveInterval_LevelZero_EqualsBaseInterval()
		{
			Assert.AreEqual(10f, Default().EffectiveInterval(0));
		}

		[TestCase(1, 10f / 1.05f)]
		[TestCase(20, 5f)]
		public void EffectiveInterval_IsIntervalDividedByMultiplier(int level, float expected)
		{
			Assert.AreEqual(expected, Default().EffectiveInterval(level), Tolerance);
		}

		[Test]
		public void EffectiveInterval_DecreasesAsLevelRises()
		{
			FridgeParams p = Default();

			Assert.Less(p.EffectiveInterval(2), p.EffectiveInterval(1));
			Assert.Less(p.EffectiveInterval(1), p.EffectiveInterval(0));
		}

		[Test]
		public void EffectiveInterval_ZeroBonus_EqualsBaseInterval()
		{
			var p = new FridgeParams(7.5f, 0f);

			Assert.AreEqual(7.5f, p.EffectiveInterval(30));
		}

		[Test]
		public void EffectiveInterval_MaxLevel_IsPositive()
		{
			Assert.Greater(Default().EffectiveInterval(int.MaxValue), 0f);
		}

		[Test]
		public void EffectiveInterval_NegativeLevel_Throws()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => Default().EffectiveInterval(-1));
		}
	}
}
