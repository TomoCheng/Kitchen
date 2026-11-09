using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Kitchen.Core.Data
{
	/// <summary>食材定義資料（技術文件 4.1）。由 JSON 解析而來的不可變純資料。</summary>
	public sealed class IngredientData
	{
		// Constructor
		public IngredientData(
			string id,
			string nameKey,
			IEnumerable<string> categories,
			FlavorVector flavor,
			int rarity,
			int buyPrice,
			int sellPrice,
			string sprite)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException("Ingredient id must not be null or empty.", nameof(id));
			}
			if (categories == null)
			{
				throw new ArgumentNullException(nameof(categories), $"Ingredient '{id}' categories must not be null.");
			}

			Id = id;
			NameKey = nameKey;
			// 防禦性複製，避免外部持有的集合被修改後影響本物件
			Categories = new ReadOnlyCollection<string>(new List<string>(categories));
			Flavor = flavor;
			Rarity = rarity;
			BuyPrice = buyPrice;
			SellPrice = sellPrice;
			Sprite = sprite;
		}

		// Properties
		public string Id { get; }
		public string NameKey { get; }
		public IReadOnlyList<string> Categories { get; }
		public FlavorVector Flavor { get; }
		public int Rarity { get; }
		public int BuyPrice { get; }
		public int SellPrice { get; }
		/// <summary>Addressables address。缺圖由 View 處理。</summary>
		public string Sprite { get; }

		// Public
		public bool HasCategory(string category)
		{
			for (int i = 0; i < Categories.Count; i++)
			{
				if (string.Equals(Categories[i], category, StringComparison.Ordinal)) return true;
			}
			return false;
		}

		public override string ToString() => $"IngredientData({Id})";
	}
}
