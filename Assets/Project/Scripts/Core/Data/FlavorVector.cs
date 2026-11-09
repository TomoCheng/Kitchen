using System;

namespace Kitchen.Core.Data
{
	/// <summary>五軸整數風味向量（技術文件 3.3、4.1）。不可變值型別。</summary>
	public readonly struct FlavorVector : IEquatable<FlavorVector>
	{
		// Constructor
		public FlavorVector(int sour, int sweet, int bitter, int salty, int spicy)
		{
			Sour = sour;
			Sweet = sweet;
			Bitter = bitter;
			Salty = salty;
			Spicy = spicy;
		}

		// Fields
		public readonly int Sour;
		public readonly int Sweet;
		public readonly int Bitter;
		public readonly int Salty;
		public readonly int Spicy;

		// Properties
		public static FlavorVector Zero => default;

		public int this[FlavorAxis axis]
		{
			get
			{
				switch (axis)
				{
					case FlavorAxis.Sour: return Sour;
					case FlavorAxis.Sweet: return Sweet;
					case FlavorAxis.Bitter: return Bitter;
					case FlavorAxis.Salty: return Salty;
					case FlavorAxis.Spicy: return Spicy;
					default: throw new ArgumentOutOfRangeException(nameof(axis), axis, "Unknown flavor axis.");
				}
			}
		}

		// Operators
		public static FlavorVector operator +(FlavorVector a, FlavorVector b)
		{
			return new FlavorVector(
				a.Sour + b.Sour,
				a.Sweet + b.Sweet,
				a.Bitter + b.Bitter,
				a.Salty + b.Salty,
				a.Spicy + b.Spicy);
		}

		public static bool operator ==(FlavorVector a, FlavorVector b) => a.Equals(b);
		public static bool operator !=(FlavorVector a, FlavorVector b) => !a.Equals(b);

		// Equality
		public bool Equals(FlavorVector other)
		{
			return Sour == other.Sour
				&& Sweet == other.Sweet
				&& Bitter == other.Bitter
				&& Salty == other.Salty
				&& Spicy == other.Spicy;
		}

		public override bool Equals(object obj) => obj is FlavorVector other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 31 + Sour;
				hash = hash * 31 + Sweet;
				hash = hash * 31 + Bitter;
				hash = hash * 31 + Salty;
				hash = hash * 31 + Spicy;
				return hash;
			}
		}

		public override string ToString()
		{
			return $"(Sour:{Sour}, Sweet:{Sweet}, Bitter:{Bitter}, Salty:{Salty}, Spicy:{Spicy})";
		}
	}
}
