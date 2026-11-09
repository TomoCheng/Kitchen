using System;

namespace Kitchen.Core.Events
{
	/// <summary>
	/// 帶 payload 的事件頻道（技術文件 3.2）。
	/// <list type="bullet">
	/// <item>派發時對訂閱者清單取快照：派發中新增的訂閱者不會收到本次事件；派發中被取消的訂閱者若在快照中仍會收到本次事件（與 C# event 語意相同）。</item>
	/// <item>同頻道重入（派發中再次 Publish 本頻道）：不派發巢狀發布，經 <see cref="EventBus.ErrorReporter"/> 回報事件名稱與呼叫堆疊。不同頻道之間的巢狀發布允許。</item>
	/// <item>某訂閱者拋例外：catch 後經 <see cref="EventBus.ErrorReporter"/> 回報，繼續派發其餘訂閱者；例外不會傳回發布者。</item>
	/// </list>
	/// 非執行緒安全：僅供主執行緒使用。
	/// </summary>
	public sealed class EventChannel<T> : EventChannelBase
	{
		// Fields
		private Action<T>[] _handlers = Array.Empty<Action<T>>();

		// Constructor
		internal EventChannel(string name) : base(name)
		{
		}

		// Properties
		public override int SubscriberCount => _handlers.Length;

		// Public
		/// <summary>訂閱。同一 handler（Delegate 相等）重複訂閱會被忽略。</summary>
		public void Subscribe(Action<T> handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler), $"Handler for '{Name}' must not be null.");
			}

			_handlers = HandlerList.Add(_handlers, handler);
		}

		/// <summary>取消訂閱。未訂閱或 null 時不做事。</summary>
		public void Unsubscribe(Action<T> handler)
		{
			if (handler == null) return;

			_handlers = HandlerList.Remove(_handlers, handler);
		}

		/// <summary>發布。派發中再次發布本頻道會被略過並回報錯誤。</summary>
		public void Publish(T payload)
		{
			if (!TryBeginDispatch()) return;

			Action<T>[] snapshot = _handlers;
			try
			{
				for (int i = 0; i < snapshot.Length; i++)
				{
					try
					{
						snapshot[i](payload);
					}
					catch (Exception exception)
					{
						ReportSubscriberException(exception);
					}
				}
			}
			finally
			{
				EndDispatch();
			}
		}

		// Internal
		private protected override void ClearSubscribers()
		{
			_handlers = Array.Empty<Action<T>>();
		}
	}
}
