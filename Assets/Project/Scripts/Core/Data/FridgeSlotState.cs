using System;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 冰箱格子配置（技術文件 4.4 fridge_slots、5.8）。不可變純資料。
	/// 例：{ "type": "random" }、{ "type": "category", "value": "vegetable" }。
	/// 格子種類名稱的合法性由冰箱系統依設定判斷，不在此寫死。
	/// </summary>
	public sealed class FridgeSlotState
	{
		// Constructor
		public FridgeSlotState(string type, string value = null)
		{
			if (string.IsNullOrWhiteSpace(type))
			{
				throw new ArgumentException("Fridge slot type must not be null or empty.", nameof(type));
			}

			Type = type;
			Value = value;
		}

		// Properties
		/// <summary>格子種類，例：random、category。</summary>
		public string Type { get; }
		/// <summary>種類參數（例：category 的類別名）；null = 無參數。</summary>
		public string Value { get; }

		// Public
		public override string ToString() => Value == null ? $"FridgeSlot({Type})" : $"FridgeSlot({Type}, {Value})";
	}
}
