using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kitchen.Core.Data
{
	/// <summary>菜餚定義資料（技術文件 3.3、4.2）。由 JSON 解析而來的不可變純資料。</summary>
	public sealed class DishData
	{
		// Constructor
		public DishData(
			string id,
			string nameKey,
			IEnumerable<IEnumerable<string>> anchors,
			IEnumerable<string> allowedRoutes,
			IEnumerable<KeyValuePair<string, int>> routeCaps,
			FloatRange balancedBand,
			int basePrice,
			int specificity)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("Dish id must not be null or empty.", nameof(id));
			}
			if (anchors == null)
			{
				throw new ArgumentNullException(nameof(anchors), $"Dish '{id}' anchors must not be null.");
			}
			if (allowedRoutes == null)
			{
				throw new ArgumentNullException(nameof(allowedRoutes), $"Dish '{id}' allowed routes must not be null.");
			}
			if (routeCaps == null)
			{
				throw new ArgumentNullException(nameof(routeCaps), $"Dish '{id}' route caps must not be null.");
			}

			Id = id;
			NameKey = nameKey;

			// 防禦性複製（含內層），避免外部持有的集合被修改後影響本物件
			var anchorGroups = new List<IReadOnlyList<string>>();
			foreach (IEnumerable<string> group in anchors)
			{
				if (group == null)
				{
					throw new ArgumentException($"Dish '{id}' anchors must not contain a null group.", nameof(anchors));
				}
				anchorGroups.Add(new ReadOnlyCollection<string>(new List<string>(group)));
			}
			Anchors = new ReadOnlyCollection<IReadOnlyList<string>>(anchorGroups);

			AllowedRoutes = new ReadOnlyCollection<string>(new List<string>(allowedRoutes));

			var caps = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (KeyValuePair<string, int> pair in routeCaps)
			{
				// RouteCaps 只允許特化路線（sour / sweet / salty / spicy）；balanced 沒有 cap
				if (!DishRoutes.IsSpecialized(pair.Key))
				{
					throw new ArgumentException($"Dish '{id}' route caps may only contain specialized routes (got '{pair.Key}').", nameof(routeCaps));
				}
				caps.Add(pair.Key, pair.Value); // 重複 key 由 Dictionary.Add 拋 ArgumentException
			}
			RouteCaps = new ReadOnlyDictionary<string, int>(caps);

			BalancedBand = balancedBand;
			BasePrice = basePrice;
			Specificity = specificity;
		}

		// Properties
		public string Id { get; }
		public string NameKey { get; }
		/// <summary>外層 = 每一組都要滿足；內層 = 該組內任一類別命中即可（技術文件 4.2）。</summary>
		public IReadOnlyList<IReadOnlyList<string>> Anchors { get; }
		public IReadOnlyList<string> AllowedRoutes { get; }
		/// <summary>特化路線 → route_cap（5.4.3 強度基準）。</summary>
		public IReadOnlyDictionary<string, int> RouteCaps { get; }
		/// <summary>均衡系適中度區間（5.4.4）。</summary>
		public FloatRange BalancedBand { get; }
		public int BasePrice { get; }
		/// <summary>多菜餚同時命中時的優先序（5.4.1）。</summary>
		public int Specificity { get; }

		// Public
		public bool AllowsRoute(string route)
		{
			for (int i = 0; i < AllowedRoutes.Count; i++)
			{
				if (string.Equals(AllowedRoutes[i], route, StringComparison.Ordinal)) return true;
			}
			return false;
		}

		public override string ToString() => $"DishData({Id})";
	}
}
