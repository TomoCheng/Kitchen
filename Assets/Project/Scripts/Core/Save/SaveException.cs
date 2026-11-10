using System;

namespace Kitchen.Core.Save
{
	/// <summary>存檔 JSON 無法解析、內容不合法或版本不受支援時拋出。</summary>
	public sealed class SaveException : Exception
	{
		public SaveException(string message)
			: base(message)
		{
		}

		public SaveException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
