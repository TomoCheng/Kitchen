using System;
using System.Collections.Generic;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 玩家存檔模型（技術文件 4.4 PlayerSave）。可變，供遊戲流程頻繁更新；JSON 轉換由 Kitchen.Core.Save.SaveSerializer 負責。
	/// 集合與子物件欄位為唯讀參考、恆不為 null（要替換內容請 Clear 後再加入）。
	/// 新遊戲的起始值（容量、起始金幣等）由呼叫端依 GameConfig 填入，本類別只給結構性預設（0 / 空集合 / null）。
	/// </summary>
	public sealed class SaveData
	{
		/// <summary>目前的存檔 schema 版本。schema 變動時遞增，並在 SaveSerializer 的 migration 補上轉換。</summary>
		public const int CurrentVersion = 1;

		// Properties
		/// <summary>存檔 schema 版本。記憶體中的模型恆為最新版（舊版在載入時由 SaveSerializer migrate）。</summary>
		public int Version => CurrentVersion;

		/// <summary>最後活動時間（Unix 秒），供離線進度（5.7）。</summary>
		public long LastActive { get; set; }

		/// <summary>本地暱稱（設定頁輸入）；空字串 = 未設定。</summary>
		public string Nickname { get; set; } = string.Empty;

		/// <summary>金幣（與 <see cref="GoldChangedPayload"/> 一致用 long）。</summary>
		public long Gold { get; set; }

		/// <summary>冰箱庫存：食材 id → 數量。</summary>
		public Dictionary<string, int> FridgeInventory { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

		/// <summary>冰箱容量。</summary>
		public int FridgeCapacity { get; set; }

		/// <summary>升級等級：升級 id → 等級。</summary>
		public Dictionary<string, int> Upgrades { get; } = new Dictionary<string, int>(StringComparer.Ordinal);

		/// <summary>冰箱格子配置。</summary>
		public List<FridgeSlotState> FridgeSlots { get; } = new List<FridgeSlotState>();

		/// <summary>菜餚紀錄。</summary>
		public List<DishRecord> Records { get; } = new List<DishRecord>();

		/// <summary>圖鑑：菜餚 id → 路線 → 最佳 Rank 字串。</summary>
		public Dictionary<string, Dictionary<string, string>> Compendium { get; } =
			new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

		/// <summary>展示欄：每格存 <see cref="Records"/> 的索引；null = 空格。</summary>
		public List<int?> Shelf { get; } = new List<int?>();

		/// <summary>玩家配方。</summary>
		public List<Recipe> Recipes { get; } = new List<Recipe>();

		/// <summary>目前啟用的配方索引（<see cref="Recipes"/>）；null = 無配方。</summary>
		public int? ActiveRecipe { get; set; }

		/// <summary>配方自動重複是否開啟（5.3）。</summary>
		public bool AutoCookEnabled { get; set; }

		/// <summary>生產鏈進行中狀態。</summary>
		public ChainState ChainState { get; } = new ChainState();

		/// <summary>累計統計。</summary>
		public PlayerStats Stats { get; } = new PlayerStats();
	}
}
