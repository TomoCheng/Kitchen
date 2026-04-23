using UnityEngine;
using System.Collections.Generic;
using Tomo.Core;
using Kitchen.Runtime;

namespace Kitchen.Systems
{
    public class FridgeSlotManager : Singleton<FridgeSlotManager>
    {
        // Properties
        public List<FridgeSlot> FridgeSlotList => _fridgeSlotList;

        // Public
        public FridgeSlot GetSlot(int index)
        {
            return (index >= 0 && index < _fridgeSlotList.Count) ? _fridgeSlotList[index] : null;
        }

        // Protected
        protected override void Awake()
        {
            base.Awake();
            Setup();
        }

        private void Update()
        {
            // 驅動所有格子的計時
            foreach (var slot in _fridgeSlotList)
            {
                slot.Tick(Time.deltaTime);
            }
        }

        // Private
        private void Setup()
        {
            _fridgeSlotList.Clear();
            _fridgeSlotList.Add(new FridgeSlot(new Data.IngredientClient(0)));
            _fridgeSlotList.Add(new FridgeSlot(new Data.IngredientClient(1)));
        }

        // Variable
        private List<FridgeSlot> _fridgeSlotList = new List<FridgeSlot>();
    }
}