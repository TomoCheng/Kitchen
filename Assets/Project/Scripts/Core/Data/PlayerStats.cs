namespace Kitchen.Core.Data
{
	/// <summary>累計統計（技術文件 4.4 stats、5.11 StatsService）。可變存檔模型。</summary>
	public sealed class PlayerStats
	{
		// Properties
		/// <summary>累計成菜數。</summary>
		public int TotalDishes { get; set; }
		/// <summary>累計 SS 數。</summary>
		public int SsCount { get; set; }
	}
}
