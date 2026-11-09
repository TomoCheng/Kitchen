using System;

namespace Kitchen.Core.Definitions
{
	/// <summary>定義資料（JSON）格式或內容不合法時拋出。</summary>
	public sealed class DefinitionException : Exception
	{
		public DefinitionException(string message)
			: base(message)
		{
		}

		public DefinitionException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
