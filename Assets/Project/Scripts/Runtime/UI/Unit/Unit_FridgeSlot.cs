using Cysharp.Threading.Tasks;
using Kitchen.Data;
using Kitchen.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Kitchen.UI
{
	public class Unit_FridgeSlot : Tomo.UI.Unit
	{
        // Public
        public void Setup(FridgeSlot fridgeSlot)
        {
            _fridgeSlot = fridgeSlot;
        }

        private async UniTask RefreshAsync()
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(_fridgeSlot.Ingredient.FridgeSlotIconName);
            _image_Ingredient.sprite = sprite;
        }

        // Protected
        protected override void OnRefresh()
        {
            RefreshAsync().Forget();
        }
        protected override void OnTick()
        {
            float progress = _fridgeSlot.FixedProduceTime - _fridgeSlot.RemainingTime;
            _unit_Bar.SetValue(progress, _fridgeSlot.FixedProduceTime);
            _unit_Bar.Tick();
        }

        // Serialized properties
        [SerializeField] private Unit_Bar _unit_Bar;
        [SerializeField] private Image _image_Ingredient;

        // Variable
        private FridgeSlot _fridgeSlot;
    }
}