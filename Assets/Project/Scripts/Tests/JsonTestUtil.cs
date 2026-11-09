using System;
using Newtonsoft.Json;

namespace Kitchen.Core.Tests
{
	/// <summary>測試共用：模擬「其他程式碼把 JsonConvert.DefaultSettings 設成會出錯的設定」。</summary>
	internal static class JsonTestUtil
	{
		/// <summary>若被 JsonConvert / JsonSerializer.CreateDefault 套用，任何反序列化都會拋例外。</summary>
		public static JsonSerializerSettings HostileDefaultSettings()
		{
			var settings = new JsonSerializerSettings
			{
				MissingMemberHandling = MissingMemberHandling.Error,
			};
			settings.Converters.Add(new ThrowingConverter());
			return settings;
		}

		private sealed class ThrowingConverter : JsonConverter
		{
			public override bool CanConvert(Type objectType) => true;

			public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
			{
				throw new InvalidOperationException("Hostile global converter was used.");
			}

			public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
			{
				throw new InvalidOperationException("Hostile global converter was used.");
			}
		}
	}
}
