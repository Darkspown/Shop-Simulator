using UnityEngine;

namespace ShelfRush.Economy
{
    /// <summary>Настройки экономики (ScriptableObject — Data).</summary>
    [CreateAssetMenu(fileName = "EconomyConfig", menuName = "ShelfRush/Economy")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField] private int startingCoins = 0;
        [SerializeField] private int startingGems = 0;

        public int StartingCoins => startingCoins;
        public int StartingGems => startingGems;
    }
}