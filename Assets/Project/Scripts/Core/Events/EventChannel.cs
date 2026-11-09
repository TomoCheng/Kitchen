using System;

namespace Kitchen.Core.Events
{
	/// <summary>
	/// 無 payload 的事件頻道。語意見 <see cref="EventChannel{T}"/>。
	/// </summary>
	public sealed class EventChannel : EventChannelBase
	{
		// Fields
		private Action[] _handlers = Array.Empty<Action>();

		// Constructor
		internal EventChannel(string name) : base(name)
		{
		}

		// Properties
		public override int SubscriberCount => _handlers.Length;

		// Public
		/// <summary>訂閱。同一 handler（Delegate 相等）重複訂閱會被忽略。</summary>
		public void Subscribe(Action handler)
		{
			if (handler == null)
			{
				throw new ArgumentNullException(nameof(handler), $"Handler for '{Name}' must not be null.");
			}

			_handlers = HandlerList.Add(_handlers, handler);
		}

		/// <summary>取消訂閱。未訂閱或 null 時不做事。</summary>
		public void Unsubscribe(Action handler)
		{
			if (handler == null) return;

			_handlers = HandlerList.Remove(_handlers, handler);
		}

		/// <summary>發布。派發中再次發布本頻道會被略過並回報錯誤。</summary>
		public void Publish()
		{
			if (!TryBeginDispatch()) return;

			Action[] snapshot = _handlers;
			try
			{
				for (int i = 0; i < snapshot.Length; i++)
				{
					try
					{
						snapshot[i]();
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
			_handlers = Array.Empty<Action>();
		}
	}
}
