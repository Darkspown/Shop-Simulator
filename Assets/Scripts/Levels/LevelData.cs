using System;
using UnityEngine;

namespace ShelfRush.Levels
{
    /// <summary>
    /// Данные уровня (ScriptableObject — Data). ЕДИНСТВЕННЫЙ источник всех параметров уровня:
    /// <see cref="LevelObjective"/> (цели), <see cref="CompletionReward"/>, <see cref="CarryCapacity"/>,
    /// <see cref="LevelIndex"/>. Игровая логика (<see cref="LevelManager"/> / <see cref="LevelProgress"/>)
    /// читает ТОЛЬКО эти данные и НЕ содержит хардкод конкретного уровня/товара/полки (Задача 09).
    ///
    /// Новый уровень = Create → LevelData → Set Objectives (Product/Shelf/Target/Rewards) → Done.
    /// Менять <see cref="LevelManager"/> для нового уровня не требуется (см. Documentation/LEVELS.md).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "ShelfRush/Level")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private int levelIndex;

        [Tooltip("Цели уровня (Level 1: Fill Drinks Shelf — Water, Target 10).")]
        [SerializeField] private LevelObjective[] objectives;

        [Tooltip("Награда (монеты) за полное прохождение уровня (Level Complete).")]
        [SerializeField, Min(0)] private int completionReward = 500;

        [Tooltip("Максимум товаров, которые игрок может нести одновременно (прогрессия переноски). " +
                 "НЕ должен храниться внутри PlayerCarry — источник этого значения уровень. L1=1.")]
        [SerializeField, Min(1)] private int carryCapacity = 1;

        public int LevelIndex => levelIndex;

        public LevelObjective[] Objectives => objectives ?? Array.Empty<LevelObjective>();

        public int CompletionReward => Mathf.Max(0, completionReward);

        public int CarryCapacity => Mathf.Max(1, carryCapacity);

        /// <summary>Суммарная цель уровня: сколько единиц товара нужно разместить на все Objectives.</summary>
        public int TotalTarget
        {
            get
            {
                var arr = Objectives;
                var total = 0;
                for (var i = 0; i < arr.Length; i++)
                {
                    if (arr[i] != null) total += arr[i].TargetCount;
                }
                return total;
            }
        }
    }
}