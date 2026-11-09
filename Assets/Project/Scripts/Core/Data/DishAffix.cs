using System;

namespace Kitchen.Core.Data
{
	/// <summary>
	/// 菜餚紀錄的詞綴（技術文件 4.3 affix、5.5）。不可變純資料。
	/// M0 暫以字串表示詞綴種類；詞綴系統（AffixGen）實作時再收斂種類常數。
	/// </summary>
	public sealed class DishAffix
	{
		// Constructor
		public DishAffix(string type, float value)
		{
			if (string.IsNullOrWhiteSpace(type))
			{
				throw new ArgumentException("Affix type must not be null or empty.", nameof(type));
			}
			if (float.IsNaN(value) || float.IsInfinity(value))
			{
				throw new ArgumentException(FormattableString.Invariant($"Affix '{type}' value must be finite (got {value})."), nameof(value));
			}

			Type = type;
			Value = value;
		}

		// Properties
		/// <summary>詞綴種類，例：sour_ingredient_bonus。</summary>
		public string Type { get; }
		/// <summary>加成值（比例，例：0.124 = +12.4%）。</summary>
		public float Value { get; }

		// Public
		public override string ToString() => FormattableString.Invariant($"DishAffix({Type}, {Value})");
	}
}
