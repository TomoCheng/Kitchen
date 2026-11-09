using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kitchen.Core.Data;

namespace Kitchen.Core.Events
{
	/// <summary>
	/// 全域事件集線器（技術文件 3、3.2）。跨系統溝通一律經此，不互相抓引用。
	/// 每個事件一個強型別頻道：<c>EventBus.GoldChanged.Subscribe(h)</c> / <c>Unsubscribe(h)</c> / <c>Publish(payload)</c>。
	/// 鐵律：payload 只帶可序列化資料、不帶物件引用；訂閱回呼內禁止再發布同名事件（偵測到即略過並回報）。
	/// 非執行緒安全：僅供主執行緒使用。
	/// </summary>
	public static class EventBus
	{
		// Fields
		// 注意：不可給初始值。靜態欄位初始化依宣告順序執行，頻道欄位在 Register 中延遲建立此清單，
		// 若此處有初始值且宣告在頻道之後，會把已註冊的頻道覆蓋掉。
		private static List<EventChannelBase> s_channels;

		// Channels（技術文件 3.2 事件契約表，新增頻道一律經 Register 建立）
		/// <summary>金幣變動。發布者：Economy。</summary>
		public static readonly EventChannel<GoldChangedPayload> GoldChanged = Register(new EventChannel<GoldChangedPayload>(nameof(GoldChanged)));
		/// <summary>庫存變動。發布者：冰箱庫存。</summary>
		public static readonly EventChannel<InventoryChangedPayload> InventoryChanged = Register(new EventChannel<InventoryChangedPayload>(nameof(InventoryChanged)));
		/// <summary>冰箱生成食材，payload = ingredientId。發布者：冰箱生成器。</summary>
		public static readonly EventChannel<string> IngredientGenerated = Register(new EventChannel<string>(nameof(IngredientGenerated)));
		/// <summary>訂單確認，payload = Recipe。發布者：訂單 UI。</summary>
		public static readonly EventChannel<Recipe> OrderConfirmed = Register(new EventChannel<Recipe>(nameof(OrderConfirmed)));
		/// <summary>訂單狀態變動。發布者：生產鏈控制器。</summary>
		public static readonly EventChannel<OrderState> OrderStateChanged = Register(new EventChannel<OrderState>(nameof(OrderStateChanged)));
		/// <summary>提前烹調請求。發布者：烹調鈕。</summary>
		public static readonly EventChannel EarlyCookRequested = Register(new EventChannel(nameof(EarlyCookRequested)));
		/// <summary>成菜，payload = DishRecord。發布者：評分管線。</summary>
		public static readonly EventChannel<DishRecord> DishCompleted = Register(new EventChannel<DishRecord>(nameof(DishCompleted)));
		/// <summary>紀錄上架。發布者：層架 UI。</summary>
		public static readonly EventChannel<RecordShelvedPayload> RecordShelved = Register(new EventChannel<RecordShelvedPayload>(nameof(RecordShelved)));
		/// <summary>全域 modifier 表變動。發布者：ModifierTable。</summary>
		public static readonly EventChannel ModifiersChanged = Register(new EventChannel(nameof(ModifiersChanged)));
		/// <summary>升級購買。發布者：升級商店。</summary>
		public static readonly EventChannel<UpgradePurchasedPayload> UpgradePurchased = Register(new EventChannel<UpgradePurchasedPayload>(nameof(UpgradePurchased)));
		/// <summary>請求存檔。發布者：任一系統。</summary>
		public static readonly EventChannel<SaveRequestedPayload> SaveRequested = Register(new EventChannel<SaveRequestedPayload>(nameof(SaveRequested)));

		// Properties
		/// <summary>
		/// 錯誤回報掛點（重入、訂閱者例外）。Core 不呼叫 Debug，由 View 啟動時接到 Debug.LogError。
		/// 為 null 時靜默略過；掛點本身拋出的例外會被吞掉，以免破壞派發。
		/// </summary>
		public static Action<string> ErrorReporter { get; set; }

		/// <summary>所有已註冊頻道（唯讀）。</summary>
		public static IReadOnlyList<EventChannelBase> Channels => new ReadOnlyCollection<EventChannelBase>(s_channels);

		// Public
		/// <summary>清空所有頻道的訂閱與派發中狀態，並清掉 <see cref="ErrorReporter"/>。供測試與重新開局使用。</summary>
		public static void ResetAll()
		{
			for (int i = 0; i < s_channels.Count; i++)
			{
				s_channels[i].Reset();
			}
			ErrorReporter = null;
		}

		// Internal
		internal static void ReportError(string message)
		{
			Action<string> reporter = ErrorReporter;
			if (reporter == null) return;

			try
			{
				reporter(message);
			}
			catch (Exception)
			{
				// 掛點本身失敗時無處可報；吞掉以維持派發狀態一致
			}
		}

		private static TChannel Register<TChannel>(TChannel channel) where TChannel : EventChannelBase
		{
			if (s_channels == null)
			{
				s_channels = new List<EventChannelBase>();
			}
			s_channels.Add(channel);
			return channel;
		}
	}
}
