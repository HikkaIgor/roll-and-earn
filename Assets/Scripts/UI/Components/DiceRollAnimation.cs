using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RollAndEarn
{
    public class DiceRollAnimation : MonoBehaviour
    {
        [SerializeField] private Image diceImage;
        [SerializeField] private Sprite[] diceFaces = new Sprite[6];
        [SerializeField] private float rollDuration = 2f;
        [SerializeField] private TMP_Text resultText;

        private void Start()
        {
            if (diceImage != null) diceImage.color = Color.clear;
        }

        public async UniTask PlayRollAsync(byte result)
        {
            if (diceImage == null) return;

            diceImage.color = Color.white;
            float elapsed = 0f;
            float interval = 0.05f;

            while (elapsed < rollDuration)
            {
                int randomFace = Random.Range(0, diceFaces.Length);
                if (diceFaces[randomFace] != null)
                    diceImage.sprite = diceFaces[randomFace];
                else
                    diceImage.color = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f);

                await UniTask.Delay((int)(interval * 1000));
                elapsed += interval;
                interval = Mathf.Min(interval + 0.01f, 0.2f);
            }

            int finalFace = result == 0 ? 0 : ((result - 1) % 6 + 6) % 6;
            if (diceFaces != null && diceFaces.Length > 0 && diceFaces[finalFace] != null)
                diceImage.sprite = diceFaces[finalFace];

            if (resultText != null)
                resultText.text = result.ToString();
        }
    }
}
