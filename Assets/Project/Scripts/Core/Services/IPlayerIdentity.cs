namespace Kitchen.Core.Services
{
	/// <summary>
	/// 玩家身分（技術文件 5.11）。prototype 實作 = 本地暱稱（<see cref="LocalPlayerIdentity"/>），
	/// 正式版換成回傳 Steam persona，呼叫端不動。
	/// </summary>
	public interface IPlayerIdentity
	{
		/// <summary>顯示名稱（例：DishRecord.maker）。恆不為 null 或空白。</summary>
		string DisplayName { get; }
	}
}
