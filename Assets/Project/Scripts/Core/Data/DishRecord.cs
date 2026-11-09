using System;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 菜餚紀錄：一次製作結果（技術文件 4.3）。不可變純資料，可作為 EventBus payload（DishCompleted）。
	/// M0 最小形式：Rank 以字串（"SS"/"S"/"A"/"B"/"C"）、詞綴以 <see cref="DishAffix"/> 表示，
	/// 評分與詞綴系統實作時再收斂型別。
	/// 失敗料理（5.4.1 錨定零命中）以 DishId == null 表示；其餘欄位的失敗語意由評分管線決定。
	/// </summary>
	public sealed class DishRecord
	{
		// Constructor
		public DishRecord(
			string dishId,
			string route,
			string rank,
			int tier,
			DishAffix affix,
			string maker,
			long timestamp,
			float score)
		{
			if (dishId != null && dishId.Trim().Length == 0)
			{
				throw new ArgumentException("DishRecord dish id must be null (failed dish) or non-empty.", nameof(dishId));
			}
			if (route != null && !DishRoutes.IsValid(route))
			{
				throw new ArgumentException($"DishRecord route '{route}' is not a valid route.", nameof(route));
			}
			if (tier < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(tier), tier, "DishRecord tier must not be negative.");
			}
			if (maker == null)
			{
				throw new ArgumentNullException(nameof(maker), "DishRecord maker must not be null.");
			}
			if (float.IsNaN(score) || float.IsInfinity(score))
			{
				throw new ArgumentException(FormattableString.Invariant($"DishRecord score must be finite (got {score})."), nameof(score));
			}

			DishId = dishId;
			Route = route;
			Rank = rank;
			Tier = tier;
			Affix = affix;
			Maker = maker;
			Timestamp = timestamp;
			Score = score;
		}

		// Properties
		/// <summary>菜餚 id；null = 失敗料理。</summary>
		public string DishId { get; }
		/// <summary>路線名（<see cref="DishRoutes"/>）；可為 null（例如失敗料理）。</summary>
		public string Route { get; }
		/// <summary>Rank 字串（"SS"/"S"/"A"/"B"/"C"）；可為 null（例如失敗料理）。</summary>
		public string Rank { get; }
		/// <summary>Tier（5.5，1~5）；0 = 無 Tier。</summary>
		public int Tier { get; }
		/// <summary>詞綴；null = 無詞綴。</summary>
		public DishAffix Affix { get; }
		/// <summary>製作者暱稱（prototype 為本地暱稱）。</summary>
		public string Maker { get; }
		/// <summary>Unix 秒。</summary>
		public long Timestamp { get; }
		/// <summary>分數（0~1）。</summary>
		public float Score { get; }

		public bool IsFailedDish => DishId == null;

		// Public
		public override string ToString() => FormattableString.Invariant($"DishRecord({DishId ?? "<failed>"}, {Route}, {Rank}, score={Score})");
	}
}
