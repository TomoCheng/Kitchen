using System;
using UnityEngine;

namespace Kitchen.Data
{
	[Serializable]
	public class Ingredient
	{
		// Properties
		public string NameKey => _nameKey;
		public long Id => _id;
		public float BaseProduceTime => _baseProduceTime;
		public int BaseValue => _baseValue;
		public string[] Tags => _tags;

		// Asset Names from CSV
		public string FridgeSlotIconName => _fridgeSlotIconName;
		public string ChoppingBoardIconName => _choppingBoardIconName;
		public string ChoppedIconName => _choppedIconName;

		// Public
		public Ingredient(string nameKey, long id, string fridgeName, string boardName, string choppedName, float baseProduceTime, int baseValue, string[] tags)
		{
			_nameKey = nameKey;
			_id = id;
			_fridgeSlotIconName = fridgeName;
			_choppingBoardIconName = boardName;
			_choppedIconName = choppedName;
			_baseProduceTime = baseProduceTime;
			_baseValue = baseValue;
			_tags = tags;
		}

		// Serialized properties
		[SerializeField] private string _nameKey;
		[SerializeField] private long _id;
		[SerializeField] private string _fridgeSlotIconName;
		[SerializeField] private string _choppingBoardIconName;
		[SerializeField] private string _choppedIconName;
		[SerializeField] private float _baseProduceTime;
		[SerializeField] private int _baseValue;
		[SerializeField] private string[] _tags;
	}
}