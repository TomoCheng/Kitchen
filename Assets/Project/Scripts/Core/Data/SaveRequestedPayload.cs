using System;

namespace Kitchen.Core.Data
{
	/// <summary>SaveRequested 事件 payload（技術文件 3.2）：原因字串（供除錯／記錄）。</summary>
	public readonly struct SaveRequestedPayload
	{
		// Constructor
		public SaveRequestedPayload(string reason)
		{
			if (reason == null)
			{
				throw new ArgumentNullException(nameof(reason), "Save reason must not be null.");
			}

			Reason = reason;
		}

		// Properties
		public string Reason { get; }

		// Public
		public override string ToString() => $"SaveRequested({Reason})";
	}
}
