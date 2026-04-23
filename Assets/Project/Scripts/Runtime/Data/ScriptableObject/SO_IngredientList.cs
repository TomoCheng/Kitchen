using UnityEngine;
using System.Collections.Generic;

namespace Kitchen.Data
{
	[CreateAssetMenu(fileName = "SO_IngredientList", menuName = "Kitchen/SO_Ingredient List")]
	public class SO_IngredientList : ScriptableObject
	{
		// Properties
		public List<Ingredient> Ingredients => _ingredients;

		// Public
		public void Clear() => _ingredients.Clear();
		public void Add(Ingredient ingredient) => _ingredients.Add(ingredient);

		// Serialized properties
		[SerializeField] private List<Ingredient> _ingredients = new List<Ingredient>();
	}
}