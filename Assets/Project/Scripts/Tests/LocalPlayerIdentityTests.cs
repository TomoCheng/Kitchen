using System;
using Kitchen.Core.Services;
using NUnit.Framework;

namespace Kitchen.Core.Tests
{
	public class LocalPlayerIdentityTests
	{
		private const string DefaultName = "Chef";

		[Test]
		public void DisplayName_WithNickname_ReturnsNickname()
		{
			IPlayerIdentity identity = new LocalPlayerIdentity("Chef_Lin", DefaultName);

			Assert.AreEqual("Chef_Lin", identity.DisplayName);
		}

		[Test]
		public void DisplayName_NicknameWithSurroundingSpaces_IsTrimmed()
		{
			var identity = new LocalPlayerIdentity("  Chef_Lin  ", DefaultName);

			Assert.AreEqual("Chef_Lin", identity.DisplayName);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		[TestCase("\t")]
		public void DisplayName_NullOrBlankNickname_FallsBackToDefault(string nickname)
		{
			var identity = new LocalPlayerIdentity(nickname, DefaultName);

			Assert.AreEqual(DefaultName, identity.DisplayName);
		}

		[TestCase(null)]
		[TestCase("")]
		[TestCase("   ")]
		public void Constructor_NullOrBlankDefault_Throws(string defaultName)
		{
			Assert.Throws<ArgumentException>(() => new LocalPlayerIdentity("Chef_Lin", defaultName));
		}
	}
}
