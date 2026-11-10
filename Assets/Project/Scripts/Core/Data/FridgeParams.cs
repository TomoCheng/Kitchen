using System;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 冰箱產出參數（技術文件 5.1 升級線「間隔縮短」、4.4 upgrades.fridge_speed）。不可變純資料，數值由設定資料提供。
	/// M0（D1 定案）：所有格子共用同一產出間隔，取代各食材的 BaseProduceTime。
	/// 等級 = 付費升級等級（存檔 upgrades 的 fridge_speed，由呼叫端取出傳入），0 起算；新冰箱（0 級）速度恰為 1.0 倍。
	/// 注意：舊 Runtime 程式（IngredientClient：_level 從 1 起算、倍率 1 + _level × 0.05）有 off-by-one，
	/// 讓全新格子就有 1.05 倍；本類別刻意不重現該行為。舊的「每產出 N 個升一級」規則已依技術文件移除。
	/// 建構時保證：任何合法等級（0 ~ int.MaxValue）的倍率皆為有限值、實際間隔皆 &gt; 0。
	/// </summary>
	public sealed class FridgeParams
	{
		// Constructor
		public FridgeParams(float produceIntervalSeconds, float speedBonusPerLevel)
		{
			if (!(produceIntervalSeconds > 0f) || float.IsInfinity(produceIntervalSeconds))
			{
				throw new ArgumentOutOfRangeException(nameof(produceIntervalSeconds), produceIntervalSeconds,
					"Fridge produce interval must be a finite number greater than 0.");
			}
			if (!(speedBonusPerLevel >= 0f) || float.IsInfinity(speedBonusPerLevel))
			{
				throw new ArgumentOutOfRangeException(nameof(speedBonusPerLevel), speedBonusPerLevel,
					"Fridge speed bonus per level must be a finite number greater than or equal to 0.");
			}

			// 倍率隨等級單調遞增，故以最高等級檢查即涵蓋所有等級：倍率不得溢位成 Infinity，間隔不得下溢成 0。
			// ECMA-335 允許 float 中間值以較高精度計算，故以 (float) 強制捨入到 float，確保檢查與實際使用的值一致。
			float maxMultiplier = Multiplier(int.MaxValue, speedBonusPerLevel);
			if (float.IsInfinity(maxMultiplier))
			{
				throw new ArgumentOutOfRangeException(nameof(speedBonusPerLevel), speedBonusPerLevel,
					FormattableString.Invariant(
						$"Fridge speed bonus per level is too large: the speed multiplier at level {int.MaxValue} overflows."));
			}
			if (!(Interval(produceIntervalSeconds, maxMultiplier) > 0f))
			{
				// 下溢可能來自過小的間隔或過大的加速，兩者皆有責任，故不指定單一參數
				throw new ArgumentOutOfRangeException(null,
					FormattableString.Invariant(
						$"Fridge parameters are out of range: with interval {produceIntervalSeconds}s and bonus {speedBonusPerLevel}/level, the effective interval at level {int.MaxValue} underflows to 0."));
			}

			ProduceIntervalSeconds = produceIntervalSeconds;
			SpeedBonusPerLevel = speedBonusPerLevel;
		}

		// Properties
		/// <summary>基礎產出間隔（秒，&gt; 0）＝ 0 級時的實際間隔。</summary>
		public float ProduceIntervalSeconds { get; }
		/// <summary>每升級一級的加速比例（≥ 0，例：0.05 = 每級 +5%）。</summary>
		public float SpeedBonusPerLevel { get; }

		// Public
		/// <summary>升級等級 → 速度倍率（= 1 + upgradeLevel × SpeedBonusPerLevel；0 級 = 1.0）。負值拋 <see cref="ArgumentOutOfRangeException"/>。</summary>
		public float SpeedMultiplier(int upgradeLevel)
		{
			if (upgradeLevel < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(upgradeLevel), upgradeLevel, "Fridge upgrade level must not be negative.");
			}

			return Multiplier(upgradeLevel, SpeedBonusPerLevel);
		}

		/// <summary>升級等級 → 實際產出間隔（秒）（= ProduceIntervalSeconds / SpeedMultiplier(upgradeLevel)），恆 &gt; 0。</summary>
		public float EffectiveInterval(int upgradeLevel)
		{
			return Interval(ProduceIntervalSeconds, SpeedMultiplier(upgradeLevel));
		}

		public override string ToString() =>
			FormattableString.Invariant($"FridgeParams(interval={ProduceIntervalSeconds}s, bonus={SpeedBonusPerLevel}/lv)");

		// Private
		// 明確轉型強制捨入到 float（避免 Mono 以較高精度保留中間值）
		private static float Multiplier(int level, float bonus) => (float)(1f + (float)(level * bonus));
		private static float Interval(float baseInterval, float multiplier) => (float)(baseInterval / multiplier);
	}
}
