using Kitchen.Data;

namespace Kitchen.Runtime
{
    public class FridgeSlot
    {
        // Constructor
        public FridgeSlot(IngredientClient ingredientClient)
        {
            _ingredientClient = ingredientClient;
            _remainingTime = FixedProduceTime;
        }

        // Properties
        public IngredientClient IngredientClient => _ingredientClient;
        public Ingredient Ingredient => _ingredientClient.ingredient;
        public float RemainingTime => _remainingTime;
        public float FixedProduceTime => Ingredient != null ? Ingredient.BaseProduceTime / IngredientClient.SpeedMultiplier : 0f;
        public bool IsReady => _remainingTime <= 0 && Ingredient != null;

        // Public
        public void Tick(float deltaTime)
        {
            if (Ingredient == null || _remainingTime <= 0) return;
            _remainingTime -= deltaTime;

            if (IsReady)
            {
                Collect();
            }
        }
        public void Collect()
        {
            _remainingTime = FixedProduceTime;
        }

        // Variable
        private IngredientClient _ingredientClient;
        private float _remainingTime;
    }
}