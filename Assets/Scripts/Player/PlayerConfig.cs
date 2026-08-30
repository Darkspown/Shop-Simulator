using UnityEngine;

namespace ShelfRush.Player
{
    /// <summary>
    /// Настройки игрока (ScriptableObject — Data). Единый источник всех параметров
    /// геймплея (движение/взаимодействие/переноска). Ничего не хранится hardcoded:
    /// prefab-вью и plain C# сервисы читают значения только отсюда.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "ShelfRush/Player")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Максимальная скорость движения, units/сек.")]
        [SerializeField] private float moveSpeed = 4f;

        [Tooltip("Ускорение (Units/s^2): как быстро набирается скорость движения.")]
        [SerializeField] private float acceleration = 40f;

        [Tooltip("Замедление (Units/s^2): как быстро гаснет скорость после отпускания ввода (используется, когда instantStop выключен).")]
        [SerializeField] private float deceleration = 60f;

        [Tooltip("Мгновенная остановка: если включено — при отпускании тапа/кнопки движения игрок останавливается сразу, без проскальзывания (инерции).")]
        [SerializeField] private bool instantStop = true;

        [Tooltip("Скорость поворота модели к направлению движения, град/сек.")]
        [SerializeField] private float rotationSpeed = 540f;

        [Header("Interaction")]
        [Tooltip("Радиус поиска ближайшего интерактивного объекта, units.")]
        [SerializeField] private float interactionRadius = 2f;

        [Tooltip("Длительность анимации «взять товар в руки», сек (DOTween-эффект и лок взаимодействия).")]
        [SerializeField] private float pickupDuration = 0.35f;

        [Tooltip("Длительность анимации «положить/разместить товар», сек (DOTween-эффект).")]
        [SerializeField] private float placementDuration = 0.35f;

        [Header("Carry")]
        [Tooltip("Максимальное число товаров, которые игрок может нести одновременно.")]
        [SerializeField] private int carryCapacity = 4;

        // ---- Legacy-поля (используются plain C# PlayerController сервисом) ----
        [Header("Legacy (plain C# service)")]
        [Tooltip("Радиус подбора товара для plain C# IPlayerController (унаследовано).")]
        [SerializeField] private float pickupRadius = 1.2f;

        public float MoveSpeed => Mathf.Max(0f, moveSpeed);

        public float Acceleration => Mathf.Max(0f, acceleration);

        public float Deceleration => Mathf.Max(0f, deceleration);

        /// <summary>Мгновенная остановка при отпускании ввода (без инерции).</summary>
        public bool InstantStop => instantStop;

        public float RotationSpeed => Mathf.Max(0f, rotationSpeed);

        public float InteractionRadius => Mathf.Max(0f, interactionRadius);

        public float PickupDuration => Mathf.Max(0f, pickupDuration);

        public float PlacementDuration => Mathf.Max(0f, placementDuration);

        public int CarryCapacity => Mathf.Max(1, carryCapacity);

        /// <summary>Унаследованный радиус подбора (plain C# сервис).</summary>
        public float PickupRadius => Mathf.Max(0f, pickupRadius);
    }
}