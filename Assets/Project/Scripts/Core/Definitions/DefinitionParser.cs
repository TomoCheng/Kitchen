using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kitchen.Core.Data;
using Newtonsoft.Json;

namespace Kitchen.Core.Definitions
{
	/// <summary>
	/// 定義資料 JSON → Core 純資料（技術文件 2.3、3.3、4.1）。
	/// JSON 先進內部 DTO，驗證後再轉為不可變的 Core 資料類別。
	/// </summary>
	public static class DefinitionParser
	{
		// Public
		/// <summary>解析食材定義陣列（ingredients.json）。格式或內容錯誤時拋出 <see cref="DefinitionException"/>。</summary>
		public static IReadOnlyList<IngredientData> ParseIngredients(string json)
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				throw new DefinitionException("Ingredient definitions: JSON text is null or empty.");
			}

			List<IngredientDto> dtos;
			try
			{
				dtos = JsonConvert.DeserializeObject<List<IngredientDto>>(json, SerializerSettings);
			}
			catch (JsonException ex)
			{
				throw new DefinitionException($"Ingredient definitions: invalid JSON. {ex.Message}", ex);
			}

			if (dtos == null)
			{
				throw new DefinitionException("Ingredient definitions: top-level value must be a JSON array.");
			}

			var result = new List<IngredientData>(dtos.Count);
			var seenIds = new Dictionary<string, int>(StringComparer.Ordinal);

			for (int i = 0; i < dtos.Count; i++)
			{
				IngredientDto dto = dtos[i];
				if (dto == null)
				{
					throw Error(i, null, "entry is null.");
				}

				if (string.IsNullOrWhiteSpace(dto.Id))
				{
					throw MissingField(i, null, "id");
				}

				string id = dto.Id;
				if (seenIds.TryGetValue(id, out int firstIndex))
				{
					throw Error(i, id, $"duplicate id (first defined at index {firstIndex}).");
				}
				seenIds.Add(id, i);

				if (string.IsNullOrWhiteSpace(dto.NameKey)) throw MissingField(i, id, "name_key");
				if (dto.Categories == null) throw MissingField(i, id, "categories");
				for (int c = 0; c < dto.Categories.Count; c++)
				{
					if (string.IsNullOrWhiteSpace(dto.Categories[c]))
					{
						throw Error(i, id, $"field 'categories[{c}]' is null or empty.");
					}
				}
				if (dto.Flavor == null) throw MissingField(i, id, "flavor");
				FlavorVector flavor = ToFlavor(dto.Flavor, i, id);
				if (!dto.Rarity.HasValue) throw MissingField(i, id, "rarity");
				if (!dto.BuyPrice.HasValue) throw MissingField(i, id, "buy_price");
				if (!dto.SellPrice.HasValue) throw MissingField(i, id, "sell_price");
				if (string.IsNullOrWhiteSpace(dto.Sprite)) throw MissingField(i, id, "sprite");

				result.Add(new IngredientData(
					id,
					dto.NameKey,
					dto.Categories,
					flavor,
					dto.Rarity.Value,
					dto.BuyPrice.Value,
					dto.SellPrice.Value,
					dto.Sprite));
			}

			return new ReadOnlyCollection<IngredientData>(result);
		}

		// Private
		private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
		{
			MissingMemberHandling = MissingMemberHandling.Ignore,
			NullValueHandling = NullValueHandling.Include,
		};

		private static FlavorVector ToFlavor(FlavorDto f, int index, string id)
		{
			if (!f.Sour.HasValue) throw MissingField(index, id, "flavor.sour");
			if (!f.Sweet.HasValue) throw MissingField(index, id, "flavor.sweet");
			if (!f.Bitter.HasValue) throw MissingField(index, id, "flavor.bitter");
			if (!f.Salty.HasValue) throw MissingField(index, id, "flavor.salty");
			if (!f.Spicy.HasValue) throw MissingField(index, id, "flavor.spicy");
			return new FlavorVector(f.Sour.Value, f.Sweet.Value, f.Bitter.Value, f.Salty.Value, f.Spicy.Value);
		}

		private static DefinitionException MissingField(int index, string id, string field)
		{
			return Error(index, id, $"missing required field '{field}'.");
		}

		private static DefinitionException Error(int index, string id, string detail)
		{
			string idPart = id == null ? "id <none>" : $"id '{id}'";
			return new DefinitionException($"Ingredient definitions: entry #{index} ({idPart}): {detail}");
		}

		// DTO（僅供反序列化；可空型別用來偵測欄位缺漏）
		internal sealed class IngredientDto
		{
			[JsonProperty("id")] public string Id { get; set; }
			[JsonProperty("name_key")] public string NameKey { get; set; }
			[JsonProperty("categories")] public List<string> Categories { get; set; }
			[JsonProperty("flavor")] public FlavorDto Flavor { get; set; }
			[JsonProperty("rarity")] public int? Rarity { get; set; }
			[JsonProperty("buy_price")] public int? BuyPrice { get; set; }
			[JsonProperty("sell_price")] public int? SellPrice { get; set; }
			[JsonProperty("sprite")] public string Sprite { get; set; }
		}

		internal sealed class FlavorDto
		{
			[JsonProperty("sour")] public int? Sour { get; set; }
			[JsonProperty("sweet")] public int? Sweet { get; set; }
			[JsonProperty("bitter")] public int? Bitter { get; set; }
			[JsonProperty("salty")] public int? Salty { get; set; }
			[JsonProperty("spicy")] public int? Spicy { get; set; }
		}
	}
}
