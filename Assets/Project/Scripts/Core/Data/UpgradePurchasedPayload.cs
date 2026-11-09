using System;

namespace Kitchen.Core.Data
{
	/// <summary>UpgradePurchased 事件 payload（技術文件 3.2）：upgradeId、新等級。</summary>
	public readonly struct UpgradePurchasedPayload
	{
		// Constructor
		public UpgradePurchasedPayload(string upgradeId, int newLevel)
		{
			if (string.IsNullOrWhiteSpace(upgradeId))
			{
				throw new ArgumentException("Upgrade id must not be null or empty.", nameof(upgradeId));
			}
			if (newLevel < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(newLevel), newLevel, $"Level of upgrade '{upgradeId}' must not be negative.");
			}

			UpgradeId = upgradeId;
			NewLevel = newLevel;
		}

		// Properties
		public string UpgradeId { get; }
		/// <summary>購買後的等級。</summary>
		public int NewLevel { get; }

		// Public
		public override string ToString() => $"UpgradePurchased({UpgradeId}, {NewLevel})";
	}
}
