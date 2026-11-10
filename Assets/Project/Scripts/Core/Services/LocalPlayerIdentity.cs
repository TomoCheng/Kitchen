using System;

namespace Kitchen.Core.Services
{
	/// <summary>
	/// 本地暱稱身分（技術文件 4.3 maker、5.11）。暱稱為 null 或空白時回退為預設暱稱；
	/// 預設暱稱由呼叫端從 GameConfig 取得後傳入，不寫死在此。前後空白會被去除。
	/// </summary>
	public sealed class LocalPlayerIdentity : IPlayerIdentity
	{
		// Constructor
		public LocalPlayerIdentity(string nickname, string defaultName)
		{
			if (string.IsNullOrWhiteSpace(defaultName))
			{
				throw new ArgumentException("Default player name must not be null or empty.", nameof(defaultName));
			}

			DisplayName = string.IsNullOrWhiteSpace(nickname) ? defaultName.Trim() : nickname.Trim();
		}

		// Properties
		public string DisplayName { get; }

		// Public
		public override string ToString() => $"LocalPlayerIdentity({DisplayName})";
	}
}
