using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Kitchen.UI
{
	public class Unit_Progress : Tomo.UI.Unit
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
            _text_ProgressValue.SetText(Mathf.RoundToInt(_progressValue).ToString());
            _text_MaxValue.SetText(Mathf.RoundToInt(_maxValue).ToString());
        }

        // Serialized properties
        [SerializeField] private TextMeshProUGUI _text_ProgressValue;
        [SerializeField] private TextMeshProUGUI _text_MaxValue;

        // Variable
        private float _progressValue;
        private float _maxValue;
    }
}