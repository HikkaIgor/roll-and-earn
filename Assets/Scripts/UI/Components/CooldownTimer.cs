using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class CooldownTimer : MonoBehaviour
    {
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Slider cooldownSlider;

        private float _startTime;
        private float _totalDuration;
        private bool _active;

        public bool IsOnCooldown => _active;
        public int RemainingSeconds => _active ? System.Math.Max(0, (int)(_totalDuration - (Time.time - _startTime))) : 0;

        public void StartCooldown(int totalSeconds)
        {
            _totalDuration = totalSeconds;
            _startTime = Time.time;
            _active = true;
        }

        public void Reset()
        {
            _active = false;
            if (timerText != null) timerText.text = "Ready!";
            if (cooldownSlider != null) cooldownSlider.value = 1f;
        }

        private void Update()
        {
            if (!_active) return;

            int remaining = RemainingSeconds;
            if (remaining <= 0)
            {
                Reset();
                return;
            }

            int minutes = remaining / 60;
            int seconds = remaining % 60;
            if (timerText != null) timerText.text = $"{minutes:D2}:{seconds:D2}";
            if (cooldownSlider != null) cooldownSlider.value = (float)remaining / _totalDuration;
        }
    }
}
