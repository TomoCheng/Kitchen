using Kitchen.Systems;
using System;
using UnityEngine;

namespace Kitchen.Data
{
    [Serializable]
    public class IngredientClient
    {
        // Properties
        public long Id => _id;
        public int Level => _level;
        public int TotalCount => _totalCount;
        public float SpeedMultiplier => 1f + (_level * 0.05f);
        public Ingredient ingredient => IngredientManager.Instance.GetIngredient(_id);

        // Public
        public IngredientClient(long id)
        {
            _id = id;
            _level = 1;
            _totalCount = 0;
        }

        public void AddCount(int amount)
        {
            _totalCount += amount;
            _level = (_totalCount / 10) + 1;
        }

        // Serialized properties
        [SerializeField] private long _id;
        [SerializeField] private int _level;
        [SerializeField] private int _totalCount;
    }
}