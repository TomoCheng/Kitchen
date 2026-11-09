using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 配方（技術文件 4.4 recipes、5.3）：玩家命名 + 食材 id 清單（可重複，數量即份數）。
	/// 不可變純資料，可作為 EventBus payload（OrderConfirmed）。
	/// 份數上限（鍋容量）屬訂單規則，由下單流程依設定檢查，不在此寫死。
	/// </summary>
	public sealed class Recipe
	{
		// Constructor
		public Recipe(string name, IEnumerable<string> items)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name), "Recipe name must not be null.");
			}
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items), $"Recipe '{name}' items must not be null.");
			}

			// 防禦性複製，避免外部持有的集合被修改後影響本物件
			var list = new List<string>(items);
			for (int i = 0; i < list.Count; i++)
			{
				if (string.IsNullOrWhiteSpace(list[i]))
				{
					throw new ArgumentException($"Recipe '{name}' items[{i}] must not be null or empty.", nameof(items));
				}
			}

			Name = name;
			Items = new ReadOnlyCollection<string>(list);
		}

		// Properties
		/// <summary>玩家命名的字面值（不是在地化 key）。可為空字串。</summary>
		public string Name { get; }
		/// <summary>食材 id 清單；同一 id 出現 n 次 = n 份。</summary>
		public IReadOnlyList<string> Items { get; }

		// Public
		public override string ToString() => $"Recipe({Name}, {Items.Count} items)";
	}
}
