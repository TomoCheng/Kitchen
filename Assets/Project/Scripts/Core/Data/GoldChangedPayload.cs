namespace Kitchen.Core.Data
{
	/// <summary>GoldChanged 事件 payload（技術文件 3.2）：新值、增減量。</summary>
	public readonly struct GoldChangedPayload
	{
		// Constructor
		public GoldChangedPayload(long newValue, long delta)
		{
			NewValue = newValue;
			Delta = delta;
		}

		// Properties
		/// <summary>變動後的金幣總額。</summary>
		public long NewValue { get; }
		/// <summary>本次增減量（負值 = 支出）。</summary>
		public long Delta { get; }

		// Public
		public override string ToString() => $"GoldChanged(new={NewValue}, delta={Delta})";
	}
}
