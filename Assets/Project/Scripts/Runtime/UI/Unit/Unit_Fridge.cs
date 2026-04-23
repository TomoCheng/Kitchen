using Kitchen.Runtime;
using Kitchen.Systems;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Kitchen.UI
{
	public class Unit_Fridge : Tomo.UI.Unit
	{
        // Public
        public void Setup()
		{
            CreateSlot();
            Refresh();
        }

        // Private 
        private void Awake()
        {
			Setup();//TEMP
        }
        private void Update()
        {
            Tick();//TEMP
        }
        private void CreateSlot()
        {
            if (_unit_FridgeSlot_Prefab == null) { return; }

            foreach (Transform child in _gridLayoutGroup_Slot.transform)
            {
                Destroy(child.gameObject);
            }

            _unit_fridgeSlotList.Clear();

            List<FridgeSlot> fridgeSlotList = FridgeSlotManager.Instance.FridgeSlotList;
            foreach (var fridgeSlot in fridgeSlotList)
            {
                Unit_FridgeSlot unit = Instantiate(_unit_FridgeSlot_Prefab, _gridLayoutGroup_Slot.transform);
                unit.Setup(fridgeSlot);
                _unit_fridgeSlotList.Add(unit);
            }
        }

        // Protected
        protected override void OnRefresh()
        {
            foreach (var fridgeSlot in _unit_fridgeSlotList)
            {
                fridgeSlot.Refresh();
            }
        }
        protected override void OnTick()
        {
            foreach (var fridgeSlot in _unit_fridgeSlotList)
            {
                fridgeSlot.Tick();
            }
        }

        // Serialized properties
        [SerializeField] private GridLayoutGroup _gridLayoutGroup_Slot;
        [SerializeField] private Unit_FridgeSlot _unit_FridgeSlot_Prefab;

        // Variable
        private List<Unit_FridgeSlot> _unit_fridgeSlotList = new List<Unit_FridgeSlot>();
    }
}