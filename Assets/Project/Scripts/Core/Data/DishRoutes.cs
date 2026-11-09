using System;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 菜餚路線名稱（技術文件 4.2、5.4.2）。四條特化路線各對應一個風味軸；
	/// balanced 為均衡系；苦（bitter）是純干擾軸，不是路線。
	/// </summary>
	public static class DishRoutes
	{
		public const string Sour = "sour";
		public const string Sweet = "sweet";
		public const string Salty = "salty";
		public const string Spicy = "spicy";
		public const string Balanced = "balanced";

		/// <summary>是否為合法路線名（含 balanced）。大小寫敏感。</summary>
		public static bool IsValid(string route)
		{
			return IsSpecialized(route) || string.Equals(route, Balanced, StringComparison.Ordinal);
		}

		/// <summary>是否為特化路線（sour / sweet / salty / spicy）。</summary>
		public static bool IsSpecialized(string route)
		{
			return TryGetAxis(route, out _);
		}

		/// <summary>特化路線 → 對應風味軸。balanced 或非法名稱回傳 false。</summary>
		public static bool TryGetAxis(string route, out FlavorAxis axis)
		{
			switch (route)
			{
				case Sour: axis = FlavorAxis.Sour; return true;
				case Sweet: axis = FlavorAxis.Sweet; return true;
				case Salty: axis = FlavorAxis.Salty; return true;
				case Spicy: axis = FlavorAxis.Spicy; return true;
				default: axis = default; return false;
			}
		}
	}
}
