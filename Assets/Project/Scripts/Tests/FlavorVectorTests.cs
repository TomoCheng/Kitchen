using System;
using Kitchen.Core.Data;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class FlavorVectorTests
	{
		[Test]
		public void Constructor_AssignsAllAxes()
		{
			var v = new FlavorVector(1, 2, 3, 4, 5);

			Assert.AreEqual(1, v.Sour);
			Assert.AreEqual(2, v.Sweet);
			Assert.AreEqual(3, v.Bitter);
			Assert.AreEqual(4, v.Salty);
			Assert.AreEqual(5, v.Spicy);
		}

		[Test]
		public void Add_SumsEachAxis()
		{
			var a = new FlavorVector(2, 1, 0, 0, 0);
			var b = new FlavorVector(0, 1, 0, 2, 1);

			FlavorVector sum = a + b;

			Assert.AreEqual(new FlavorVector(2, 2, 0, 2, 1), sum);
		}

		[Test]
		public void Add_ZeroIsIdentity()
		{
			var a = new FlavorVector(3, 0, 2, 1, 4);

			Assert.AreEqual(a, a + FlavorVector.Zero);
			Assert.AreEqual(a, FlavorVector.Zero + a);
		}

		[Test]
		public void Default_IsAllZero()
		{
			Assert.AreEqual(new FlavorVector(0, 0, 0, 0, 0), default(FlavorVector));
		}

		[Test]
		public void Indexer_ReturnsMatchingAxis()
		{
			var v = new FlavorVector(1, 2, 3, 4, 5);

			Assert.AreEqual(1, v[FlavorAxis.Sour]);
			Assert.AreEqual(2, v[FlavorAxis.Sweet]);
			Assert.AreEqual(3, v[FlavorAxis.Bitter]);
			Assert.AreEqual(4, v[FlavorAxis.Salty]);
			Assert.AreEqual(5, v[FlavorAxis.Spicy]);
		}

		[Test]
		public void Indexer_InvalidAxis_Throws()
		{
			var v = new FlavorVector(1, 2, 3, 4, 5);

			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				int unused = v[(FlavorAxis)99];
			});
		}

		[Test]
		public void Equality_SameValues_AreEqual()
		{
			var a = new FlavorVector(1, 2, 3, 4, 5);
			var b = new FlavorVector(1, 2, 3, 4, 5);

			Assert.IsTrue(a == b);
			Assert.IsFalse(a != b);
			Assert.IsTrue(a.Equals(b));
			Assert.IsTrue(a.Equals((object)b));
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		}

		[Test]
		public void Equality_DifferentValues_AreNotEqual()
		{
			var a = new FlavorVector(1, 2, 3, 4, 5);

			Assert.IsTrue(a != new FlavorVector(0, 2, 3, 4, 5));
			Assert.IsTrue(a != new FlavorVector(1, 0, 3, 4, 5));
			Assert.IsTrue(a != new FlavorVector(1, 2, 0, 4, 5));
			Assert.IsTrue(a != new FlavorVector(1, 2, 3, 0, 5));
			Assert.IsTrue(a != new FlavorVector(1, 2, 3, 4, 0));
			Assert.IsFalse(a.Equals((object)null));
			Assert.IsFalse(a.Equals((object)"not a vector"));
		}

		[Test]
		public void ToString_ContainsAllAxisValues()
		{
			string s = new FlavorVector(1, 2, 3, 4, 5).ToString();

			StringAssert.Contains("Sour:1", s);
			StringAssert.Contains("Sweet:2", s);
			StringAssert.Contains("Bitter:3", s);
			StringAssert.Contains("Salty:4", s);
			StringAssert.Contains("Spicy:5", s);
		}
	}
}
