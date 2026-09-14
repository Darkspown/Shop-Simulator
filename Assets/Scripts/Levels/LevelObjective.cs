using System;
using ShelfRush.Products;
using ShelfRush.Shelves;
using UnityEngine;

namespace ShelfRush.Levels
{
    /// <summary>
    /// Тип цели уровня.
    /// </summary>
    public enum LevelObjectiveType
    {
        /// <summary>Наполнить указанную полку целевым товаром до <see cref="LevelObjective.TargetCount"/>.</summary>
        FillShelf = 0,
    }

    /// <summary>
    /// Определение одной цели уровня (данные, БЕЗ состояния). Все параметры уровня хранятся
    /// в <see cref="LevelData"/> (ScriptableObject): <see cref="LevelManager"/> и <see cref="LevelProgress"/>
    /// читают только эти данные и не хардкодят товар/полку/цель/награду в коде.
    ///
    /// Пример (Level 1): Type=FillShelf, TargetProduct=Water, TargetShelf=Drinks,
    /// TargetCount=10, ShelfCompleteReward=100.
    /// </summary>
    [Serializable]
    public sealed class LevelObjective
    {
        [Tooltip("Тип цели (пока поддерживается только FillShelf).")]
        [SerializeField] private LevelObjectiveType type = LevelObjectiveType.FillShelf;

        [Tooltip("Отображаемая формулировка цели (для HUD / LevelProgress).")]
        [SerializeField] private string goalDescription;

        [Tooltip("Целевой товар (например Water). Прогресс идёт по размещению этого товара на полку.")]
        [SerializeField] private ProductData targetProduct;

        [Tooltip("Полка цели (например Drinks). Размещение учитывается, если evt.Shelf == TargetShelf.")]
        [SerializeField] private ShelfData targetShelf;

        [Tooltip("Сколько единиц TargetProduct нужно разместить (прогресс 0..TargetCount).")]
        [SerializeField, Min(1)] private int targetCount = 1;

        [Tooltip("Награда (монеты) за полное заполнение этой цели (Shelf Complete).")]
        [SerializeField, Min(0)] private int shelfCompleteReward = 100;

        public LevelObjectiveType Type => type;

        public string GoalDescription => goalDescription;

        public ProductData TargetProduct => targetProduct;

        public ShelfData TargetShelf => targetShelf;

        public int TargetCount => Mathf.Max(1, targetCount);

        public int ShelfCompleteReward => Mathf.Max(0, shelfCompleteReward);
    }
}