namespace Kitchen.Core.Data
{
	/// <summary>生產鏈訂單狀態（技術文件 3.2 OrderStateChanged、5.3）。</summary>
	public enum OrderState
	{
		/// <summary>閒置：沒有進行中的訂單。</summary>
		Idle = 0,
		/// <summary>供料中：食材逐份送往砧板／鍋子。</summary>
		Supplying = 1,
		/// <summary>缺料待機：訂單有缺口且庫存已空，等冰箱生成。</summary>
		WaitingForIngredients = 2,
		/// <summary>待開煮：全數入鍋，等玩家按烹調鈕。</summary>
		ReadyToCook = 3,
		/// <summary>烹調中。</summary>
		Cooking = 4,
	}
}
