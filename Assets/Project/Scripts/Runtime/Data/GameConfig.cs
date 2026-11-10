using System;
using Kitchen.Core.Data;
using Kitchen.Core.Services;
using UnityEngine;

namespace Kitchen.Data
{
	/// <summary>
	/// 全域遊戲常數（技術文件 2.3、5.1、5.7、5.11）。集中一份，供 Inspector 與調試面板（5.10）即時調整。
	/// 此 SO 是 View 側資產：Game.Core 不引用本類別，也不依賴 ScriptableObject；
	/// Core 只透過 <see cref="ToFridgeParams"/>、<see cref="CreateIdentity"/> 等轉換後的純資料使用這些值。
	/// 調試面板之後改的就是同一份 config，改完重新呼叫轉換方法取得新的 Core 參數即可。
	/// </summary>
	[CreateAssetMenu(fileName = "GameConfig", menuName = "Kitchen/Game Config")]
	public sealed class GameConfig : ScriptableObject
	{
		// Properties
		/// <summary>基礎產出間隔（秒，&gt; 0）＝ 冰箱速度 0 級時每格的產出間隔。</summary>
		public float ProduceIntervalSeconds => _produceIntervalSeconds;
		/// <summary>每升一級 fridge_speed 的加速比例（≥ 0，0.05 = 每級 +5%）。</summary>
		public float SpeedBonusPerLevel => _speedBonusPerLevel;
		/// <summary>冰箱初始格數（5.1：12）；讀檔時 fridge_capacity &lt;= 0 以此補值（5.7）。</summary>
		public int InitialSlotCount => _initialSlotCount;
		/// <summary>冰箱格子欄數（5.1：3）。</summary>
		public int SlotColumns => _slotColumns;
		/// <summary>冰箱格子列數，由 InitialSlotCount / SlotColumns 推得（5.1：4）。SlotColumns 非正值時回傳 0。</summary>
		public int SlotRows => _slotColumns > 0 ? _initialSlotCount / _slotColumns : 0;
		/// <summary>暱稱未設定時的預設顯示名稱（5.11 IPlayerIdentity）。</summary>
		public string DefaultNickname => _defaultNickname;

		// Public
		/// <summary>
		/// 轉成 Core 的 <see cref="FridgeParams"/>。值不合法時由 FridgeParams 建構子拋
		/// <see cref="ArgumentOutOfRangeException"/>（fail fast，不默默修正數值）。
		/// </summary>
		public FridgeParams ToFridgeParams()
		{
			return new FridgeParams(_produceIntervalSeconds, _speedBonusPerLevel);
		}

		/// <summary>
		/// 以存檔中的暱稱建立 Core 的 <see cref="LocalPlayerIdentity"/>；savedNickname 為 null / 空白時回退為 DefaultNickname。
		/// DefaultNickname 空白時由 LocalPlayerIdentity 建構子拋 <see cref="ArgumentException"/>。
		/// </summary>
		public LocalPlayerIdentity CreateIdentity(string savedNickname)
		{
			return new LocalPlayerIdentity(savedNickname, _defaultNickname);
		}

		// Unity
		private void OnValidate()
		{
			// 只警告、不拋例外：OnValidate 在 Inspector 編輯中途也會被呼叫，拋例外會干擾編輯
			if (!(_produceIntervalSeconds > 0f) || float.IsInfinity(_produceIntervalSeconds))
			{
				Debug.LogWarning($"[GameConfig] Produce interval must be a finite number greater than 0 (current: {_produceIntervalSeconds}).", this);
			}
			if (!(_speedBonusPerLevel >= 0f) || float.IsInfinity(_speedBonusPerLevel))
			{
				Debug.LogWarning($"[GameConfig] Speed bonus per level must be a finite number >= 0 (current: {_speedBonusPerLevel}).", this);
			}
			if (_initialSlotCount <= 0)
			{
				Debug.LogWarning($"[GameConfig] Initial slot count must be greater than 0 (current: {_initialSlotCount}).", this);
			}
			if (_slotColumns <= 0)
			{
				Debug.LogWarning($"[GameConfig] Slot columns must be greater than 0 (current: {_slotColumns}).", this);
			}
			else if (_initialSlotCount % _slotColumns != 0)
			{
				Debug.LogWarning($"[GameConfig] Initial slot count ({_initialSlotCount}) must be divisible by slot columns ({_slotColumns}).", this);
			}
			if (string.IsNullOrWhiteSpace(_defaultNickname))
			{
				Debug.LogWarning("[GameConfig] Default nickname must not be empty.", this);
			}

			// 以 Core 的實際規則再驗一次（含極端等級下的溢位 / 下溢檢查），確保執行期 ToFridgeParams 不會失敗
			try
			{
				_ = new FridgeParams(_produceIntervalSeconds, _speedBonusPerLevel);
			}
			catch (ArgumentOutOfRangeException e)
			{
				Debug.LogWarning($"[GameConfig] Current fridge values would be rejected by FridgeParams: {e.Message}", this);
			}
		}

		// Serialized properties
		[Header("Fridge")]
		[Tooltip("基礎產出間隔（秒，> 0）：每格從空到產出 1 個食材的時間，冰箱速度 0 級時的值。實際間隔 = 此值 / (1 + 等級 × 每級加成)。起測值 7 取自舊原型各食材 BaseProduceTime 的平均。")]
		[Min(0.01f)]
		[SerializeField] private float _produceIntervalSeconds = 7f;

		[Tooltip("每升一級冰箱速度（付費升級 fridge_speed，0 起算）的加速比例（≥ 0）。0.05 = 每級 +5%。")]
		[Min(0f)]
		[SerializeField] private float _speedBonusPerLevel = 0.05f;

		[Tooltip("冰箱初始格數（> 0）。讀檔時 fridge_capacity <= 0 以此補值。必須能被欄數整除。")]
		[Min(1)]
		[SerializeField] private int _initialSlotCount = 12;

		[Tooltip("冰箱格子欄數（> 0）。列數 = 初始格數 / 欄數。")]
		[Min(1)]
		[SerializeField] private int _slotColumns = 3;

		[Header("Player")]
		[Tooltip("玩家未設定暱稱時的預設顯示名稱（不可空白）。")]
		[SerializeField] private string _defaultNickname = "Chef";
	}
}
