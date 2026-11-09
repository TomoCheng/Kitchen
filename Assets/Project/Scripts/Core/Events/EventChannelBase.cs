using System;

namespace Kitchen.Core.Events
{
	/// <summary>
	/// EventBus 頻道共用基底：名稱、派發中旗標、重入偵測與錯誤回報（技術文件 3.2）。
	/// 建構子為 private protected，只能由本組件內的 <see cref="EventChannel"/>／<see cref="EventChannel{T}"/> 衍生，
	/// 頻道實例只由 <see cref="EventBus"/> 建立並註冊，確保 <see cref="EventBus.ResetAll"/> 涵蓋所有頻道。
	/// 非執行緒安全：僅供主執行緒使用。
	/// </summary>
	public abstract class EventChannelBase
	{
		// Fields
		private bool _isDispatching;

		// Constructor
		private protected EventChannelBase(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException("Event channel name must not be null or empty.", nameof(name));
			}

			Name = name;
		}

		// Properties
		/// <summary>事件名稱（與技術文件 3.2 事件契約表一致）。</summary>
		public string Name { get; }

		/// <summary>目前是否正在派發本頻道。</summary>
		public bool IsDispatching => _isDispatching;

		/// <summary>目前訂閱者數量。</summary>
		public abstract int SubscriberCount { get; }

		// Internal
		/// <summary>清空訂閱者與派發中狀態（供 <see cref="EventBus.ResetAll"/> 使用）。</summary>
		internal void Reset()
		{
			_isDispatching = false;
			ClearSubscribers();
		}

		private protected abstract void ClearSubscribers();

		/// <summary>
		/// 開始派發。若本頻道已在派發中（同名事件重入），不派發並回報錯誤（3.2 重入處理定案），回傳 false。
		/// </summary>
		private protected bool TryBeginDispatch()
		{
			if (_isDispatching)
			{
				EventBus.ReportError(
					$"EventBus: re-entrant Publish of '{Name}' was ignored (publishing an event from within its own dispatch is not allowed)."
					+ Environment.NewLine + "Stack trace:" + Environment.NewLine + Environment.StackTrace);
				return false;
			}

			_isDispatching = true;
			return true;
		}

		private protected void EndDispatch()
		{
			_isDispatching = false;
		}

		/// <summary>訂閱者拋例外：回報後由呼叫端繼續派發其餘訂閱者。</summary>
		private protected void ReportSubscriberException(Exception exception)
		{
			EventBus.ReportError(
				$"EventBus: a subscriber of '{Name}' threw an exception; remaining subscribers were still notified."
				+ Environment.NewLine + exception);
		}

		// Public
		public override string ToString() => $"EventChannel({Name}, subscribers={SubscriberCount})";
	}
}
