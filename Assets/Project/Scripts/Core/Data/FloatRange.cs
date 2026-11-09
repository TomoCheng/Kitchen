using System;
using System.Globalization;

namespace Kitchen.Core.Data
{
	/// <summary>閉區間 [Lo, Hi]（技術文件 3.3）。不可變值型別。</summary>
	public readonly struct FloatRange : IEquatable<FloatRange>
	{
		// Constructor
		public FloatRange(float lo, float hi)
		{
			if (float.IsNaN(lo) || float.IsNaN(hi))
			{
				throw new ArgumentException($"FloatRange bounds must not be NaN (lo={lo}, hi={hi}).");
			}
			if (lo > hi)
			{
				throw new ArgumentException($"FloatRange requires lo <= hi (lo={lo}, hi={hi}).");
			}

			Lo = lo;
			Hi = hi;
		}

		// Fields
		public readonly float Lo;
		public readonly float Hi;

		// Public
		/// <summary>value 是否落在閉區間 [Lo, Hi] 內。</summary>
		public bool Contains(float value) => value >= Lo && value <= Hi;

		// Operators
		public static bool operator ==(FloatRange a, FloatRange b) => a.Equals(b);
		public static bool operator !=(FloatRange a, FloatRange b) => !a.Equals(b);

		// Equality
		public bool Equals(FloatRange other) => Lo.Equals(other.Lo) && Hi.Equals(other.Hi);

		public override bool Equals(object obj) => obj is FloatRange other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + Lo.GetHashCode();
				hash = hash * 31 + Hi.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0}, {1}]", Lo, Hi);
		}
	}
}
