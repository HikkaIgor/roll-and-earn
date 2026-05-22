using UnityEngine;

namespace RollAndEarn
{
    public class GameConfigProvider : MonoBehaviour
    {
        public static GameConfigProvider Instance { get; private set; }

        [SerializeField] private GameConfig config;
        public GameConfig Config => config;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }
}
