using Kitchen.Core.Events;
using UnityEngine;

namespace Kitchen.Systems
{
	/// <summary>
	/// Core static 集線器的啟動接線（技術文件 3.2 重入處理、7.2 最後一段 static 重置規則）。
	/// Core 不引用 UnityEngine，因此「重置 static 狀態」與「把錯誤回報掛點接到 Debug.LogError」由 View 側在此負責。
	/// </summary>
	public static class CoreBootstrap
	{
		// Private
		/// <summary>
		/// SubsystemRegistration 是進入 Play 時最早的時點（早於任何場景物件的 Awake），
		/// 不論是否重載 Domain 都會執行一次。
		/// 7.2：目前每次 Play 都會重載 Domain，static 不會殘留；但日後若改為「不重載 Domain」以加速進入 Play，
		/// 上一輪 Play 的訂閱與派發中狀態會殘留在 static 欄位，因此一律在此重置。
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetCoreStatics()
		{
			// 順序很重要：ResetAll 會一併把 ErrorReporter 清為 null，
			// 所以必須「先重置、再接掛點」；顛倒順序會讓重入與訂閱者例外在整個 Play 期間被靜默吞掉。
			EventBus.ResetAll();
			EventBus.ErrorReporter = message => Debug.LogError(message);
		}
	}
}
