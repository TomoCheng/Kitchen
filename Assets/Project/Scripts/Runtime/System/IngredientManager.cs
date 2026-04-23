using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using Tomo.Core;
using Kitchen.Data;

namespace Kitchen.Systems
{
	public class IngredientManager : Singleton<IngredientManager>
	{
		// Properties
		public List<Ingredient> IngredientList => _SO_IngredientList != null ? _SO_IngredientList.Ingredients : null;

		// Public
		public Ingredient GetIngredient(long id)
		{
			if (_ingredientMap.TryGetValue(id, out var ingredient))
			{
				return ingredient;
			}
			return null;
		}

		// Protected
		protected override void Awake()
		{
			base.Awake();
			Setup();
		}

		// Private
		private void Setup()
		{
			if (_SO_IngredientList == null) { return; }

			_ingredientMap.Clear();

			foreach (var ingredient in _SO_IngredientList.Ingredients)
			{
				if (!_ingredientMap.ContainsKey(ingredient.Id))
				{
					_ingredientMap.Add(ingredient.Id, ingredient);
				}
			}
		}

		// Serialized properties
		[SerializeField] private SO_IngredientList _SO_IngredientList;

		// Variable
		private Dictionary<long, Ingredient> _ingredientMap = new Dictionary<long, Ingredient>();
	}
}