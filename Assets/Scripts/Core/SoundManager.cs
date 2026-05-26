using UnityEngine;

namespace RollAndEarn
{
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[SoundManager]");
                    _instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private AudioSource _sfxSource;
        private AudioClip _clickClip;
        private AudioClip _diceRollClip;
        private AudioClip _diceTickClip;
        private AudioClip _rewardClip;
        private AudioClip _equipClip;
        private AudioClip _unequipClip;
        private AudioClip _failClip;
        private AudioClip _levelUpClip;
        private AudioClip _loginClip;
        private AudioClip _dailyRewardClip;
        private AudioClip _diceResultBadClip;
        private AudioClip _diceResultMidClip;
        private AudioClip _diceResultGoodClip;
        private AudioClip _diceResultCriticalClip;
        private bool _initialized;

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            Initialize();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
            _sfxSource.volume = 0.5f;

            _clickClip = GenerateTone(800, 0.06f, 0.3f);
            _diceRollClip = GenerateNoiseBurst(0.25f, 0.4f);
            _diceTickClip = GenerateTone(1200, 0.03f, 0.15f);
            _rewardClip = GenerateChime(new[] { 523, 659, 784 }, 0.35f, 0.35f);
            _equipClip = GenerateChime(new[] { 440, 554 }, 0.18f, 0.3f);
            _unequipClip = GenerateChime(new[] { 554, 440 }, 0.18f, 0.25f);
            _failClip = GenerateChime(new[] { 330, 262 }, 0.3f, 0.25f);
            _levelUpClip = GenerateChime(new[] { 523, 659, 784, 1047 }, 0.5f, 0.35f);
            _loginClip = GenerateChime(new[] { 523, 659, 784, 1047, 1319 }, 0.7f, 0.3f);
            _dailyRewardClip = GenerateChime(new[] { 659, 784, 988, 1175 }, 0.45f, 0.35f);
            _diceResultBadClip = GenerateChime(new[] { 330, 262, 220 }, 0.35f, 0.2f);
            _diceResultMidClip = GenerateTone(660, 0.12f, 0.3f);
            _diceResultGoodClip = GenerateChime(new[] { 660, 880, 1047 }, 0.25f, 0.35f);
            _diceResultCriticalClip = GenerateChime(new[] { 523, 659, 784, 1047, 1319, 1568 }, 0.6f, 0.4f);
        }

        public void PlayClick() => Play(_clickClip);
        public void PlayDiceRoll() => Play(_diceRollClip);
        public void PlayDiceTick() => Play(_diceTickClip);
        public void PlayReward() => Play(_rewardClip);
        public void PlayEquip() => Play(_equipClip);
        public void PlayUnequip() => Play(_unequipClip);
        public void PlayFail() => Play(_failClip);
        public void PlayLevelUp() => Play(_levelUpClip);
        public void PlayLogin() => Play(_loginClip);
        public void PlayDailyReward() => Play(_dailyRewardClip);
        public void PlayDiceResult(byte roll)
        {
            if (roll >= 20) Play(_diceResultCriticalClip);
            else if (roll >= 16) Play(_diceResultGoodClip);
            else if (roll >= 6) Play(_diceResultMidClip);
            else Play(_diceResultBadClip);
        }

        private void Play(AudioClip clip)
        {
            if (clip == null || _sfxSource == null) return;
            _sfxSource.PlayOneShot(clip);
        }

        private static AudioClip GenerateTone(float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * volume;
            }
            var clip = AudioClip.Create("Tone_" + frequency, samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateNoiseBurst(float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Pow(1f - t / duration, 2f);
                float noise = (Random.value * 2f - 1f) * 0.5f;
                float tone = Mathf.Sin(2f * Mathf.PI * 200 * t) * 0.3f;
                float rattle = Mathf.Sin(2f * Mathf.PI * (800 + Random.value * 400) * t) * 0.2f;
                data[i] = (noise + tone + rattle) * envelope * volume;
            }
            var clip = AudioClip.Create("NoiseBurst", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateChime(int[] frequencies, float duration, float volume)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float noteLen = duration / frequencies.Length;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIdx = Mathf.Min((int)(t / noteLen), frequencies.Length - 1);
                float noteT = t - noteIdx * noteLen;
                float envelope = Mathf.Exp(-noteT * 8f);
                float freq = frequencies[noteIdx];
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
                data[i] += Mathf.Sin(2f * Mathf.PI * freq * 2f * t) * envelope * volume * 0.3f;
            }
            var clip = AudioClip.Create("Chime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
