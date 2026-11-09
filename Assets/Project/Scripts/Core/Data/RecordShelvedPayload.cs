using System;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// RecordShelved 事件 payload（技術文件 3.2、4.4 shelf、5.9）：record 索引、層架 slot。
	/// 只帶索引不帶 DishRecord 引用（3.2 鐵律）。
	/// </summary>
	public readonly struct RecordShelvedPayload
	{
		// Constructor
		public RecordShelvedPayload(int recordIndex, int slot)
		{
			if (recordIndex < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(recordIndex), recordIndex, "Record index must not be negative.");
			}
			if (slot < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(slot), slot, "Shelf slot must not be negative.");
			}

			RecordIndex = recordIndex;
			Slot = slot;
		}

		// Properties
		/// <summary>存檔 records 陣列中的索引。</summary>
		public int RecordIndex { get; }
		/// <summary>層架格位（0 起算）。</summary>
		public int Slot { get; }

		// Public
		public override string ToString() => $"RecordShelved(record={RecordIndex}, slot={Slot})";
	}
}
