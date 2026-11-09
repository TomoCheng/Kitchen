using System;

namespace Kitchen.Core.Data
{
	/// <summary>InventoryChanged 事件 payload（技術文件 3.2）：ingredientId、新數量。</summary>
	public readonly struct InventoryChangedPayload
	{
		// Constructor
		public InventoryChangedPayload(string ingredientId, int newCount)
		{
			if (string.IsNullOrWhiteSpace(ingredientId))
			{
				throw new ArgumentException("Ingredient id must not be null or empty.", nameof(ingredientId));
			}
			if (newCount < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(newCount), newCount, $"Inventory count of '{ingredientId}' must not be negative.");
			}

			IngredientId = ingredientId;
			NewCount = newCount;
		}

		// Properties
		public string IngredientId { get; }
		/// <summary>變動後的庫存數量。</summary>
		public int NewCount { get; }

		// Public
		public override string ToString() => $"InventoryChanged({IngredientId}, {NewCount})";
	}
}
