using UnityEngine;
using UnityEngine.UI;

namespace Kitchen.UI
{
	public class Unit_Bar : Tomo.UI.Unit
	{
        // Public
        public void SetValue(float progressValue, float maxValue)
        {
            _progressValue = progressValue;
            _maxValue = maxValue;
        }

        // Protected
        protected override void OnTick()
        {
            _image_Fill.fillAmount = Mathf.Clamp01(_progressValue / _maxValue);
            _unit_Progress.SetValue(_progressValue, _maxValue);
            _unit_Progress.Tick();
        }

        // Serialized properties
        [SerializeField] private Image _image_Fill;
        [SerializeField] private Unit_Progress _unit_Progress;

        // Variable
        private float _progressValue;
        private float _maxValue;
    }
}