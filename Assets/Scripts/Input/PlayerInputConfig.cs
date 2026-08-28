using UnityEngine;

namespace ShelfRush.Input
{
    /// <summary>
    /// Настройки ввода игрока (ScriptableObject — Data). Задаёт поведение, общее для
    /// ВСЕХ платформ (PC/Mobile/WebGL/Yandex): единый dead zone, sensitivity и smoothing,
    /// чтобы геймплей вёл себя одинаково независимо от источника ввода.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerInputConfig", menuName = "ShelfRush/Input")]
    public sealed class PlayerInputConfig : ScriptableObject
    {
        [Header("Movement")]
        [Tooltip("Мёртвая зона: ввод с магнитудой меньше этого порога (0..1) игнорируется.")]
        [SerializeField] [Range(0f, 0.9f)] private float deadZone = 0.15f;

        [Tooltip("Чувствительность: множитель на нормализованный ввод (0..1 -> дальше). 1 — без изменений.")]
        [SerializeField] [Range(0.1f, 3f)] private float sensitivity = 1f;

        [Tooltip("Input smoothing: частота сглаживания (с^-1). Больше = быстрее отклик, меньше = плавнее.")]
        [SerializeField] [Range(0f, 60f)] private float smoothing = 12f;

        public float DeadZone => Mathf.Max(0f, Mathf.Min(deadZone, 1f));

        public float Sensitivity => Mathf.Max(0f, sensitivity);

        public float Smoothing => Mathf.Max(0f, smoothing);
    }
}