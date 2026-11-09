using System;

namespace Kitchen.Core.Events
{
	/// <summary>
	/// 訂閱者陣列的寫入時複製（copy-on-write）操作。
	/// 每次增刪都產生新陣列、不修改舊陣列，因此派發時直接取用當下的陣列引用即為快照，
	/// 派發中增刪訂閱不影響進行中的派發，Publish 本身不配置記憶體。
	/// </summary>
	internal static class HandlerList
	{
		/// <summary>回傳加入 handler 後的新陣列；已存在（Delegate 相等）則回傳原陣列。</summary>
		public static TDelegate[] Add<TDelegate>(TDelegate[] handlers, TDelegate handler) where TDelegate : Delegate
		{
			if (IndexOf(handlers, handler) >= 0) return handlers;

			var result = new TDelegate[handlers.Length + 1];
			Array.Copy(handlers, result, handlers.Length);
			result[handlers.Length] = handler;
			return result;
		}

		/// <summary>回傳移除 handler 後的新陣列；不存在則回傳原陣列。</summary>
		public static TDelegate[] Remove<TDelegate>(TDelegate[] handlers, TDelegate handler) where TDelegate : Delegate
		{
			int index = IndexOf(handlers, handler);
			if (index < 0) return handlers;
			if (handlers.Length == 1) return Array.Empty<TDelegate>();

			var result = new TDelegate[handlers.Length - 1];
			Array.Copy(handlers, 0, result, 0, index);
			Array.Copy(handlers, index + 1, result, index, handlers.Length - index - 1);
			return result;
		}

		private static int IndexOf<TDelegate>(TDelegate[] handlers, TDelegate handler) where TDelegate : Delegate
		{
			for (int i = 0; i < handlers.Length; i++)
			{
				if (handlers[i].Equals(handler)) return i;
			}
			return -1;
		}
	}
}
