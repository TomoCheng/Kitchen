using System.Collections.Generic;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 生產鏈進行中狀態（技術文件 4.4 chain_state）。可變存檔模型。
	/// M0 先以最小結構預留，尚未有系統讀寫；欄位語意（進度單位等）待生產鏈實作時定案。
	/// 集合欄位恆不為 null。
	/// </summary>
	public sealed class ChainState
	{
		// Properties
		/// <summary>當前訂單尚缺的食材 id（可重複）。</summary>
		public List<string> OrderRemaining { get; } = new List<string>();
		/// <summary>砧板進度。</summary>
		public float BoardProgress { get; set; }
		/// <summary>鍋內容物食材 id（可重複）。</summary>
		public List<string> PotContents { get; } = new List<string>();
		/// <summary>烹調進度。</summary>
		public float CookProgress { get; set; }
	}
}
