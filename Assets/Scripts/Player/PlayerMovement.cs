using System;
using UnityEngine;

namespace ShelfRush.Player
{
    /// <summary>
    /// Движение игрока. Единственная ответственность — превратить уже нормализованный
    /// вектор ввода в смещение Transform в плоскости XZ.
    ///
    /// ВАЖНО: PlayerMovement НЕ читает ни Input.GetAxis, ни Keyboard/Touch/Joystick.
    /// Он получает готовый нормализованный Vector2/Vector3 от PlayerController,
    /// который, в свою очередь, получает его через IPlayerInput. Источник ввода
    /// (PC/Mobile/WebGL/Yandex) для движения не имеет значения.
    /// </summary>
    public sealed class PlayerMovement
    {
        private readonly PlayerConfig _config;

        public PlayerMovement(PlayerConfig config)
        {
            _config = config;
        }

        /// <summary>Скорость движения (units/сек) из конфига.</summary>
        public float MoveSpeed => _config != null ? _config.MoveSpeed : 4f;

        /// <summary>Смещение за кадр из нормализованного 3D-ввода (XZ-плоскость).</summary>
        public Vector3 ComputeStep(Vector3 normalizedMove, float deltaTime)
        {
            return new Vector3(normalizedMove.x, 0f, normalizedMove.z) * (MoveSpeed * deltaTime);
        }

        /// <summary>Смещение за кадр из нормализованного 2D-ввода (y -> Z).</summary>
        public Vector3 ComputeStep(Vector2 normalizedMove, float deltaTime)
        {
            return ComputeStep(new Vector3(normalizedMove.x, 0f, normalizedMove.y), deltaTime);
        }

        /// <summary>Применить движение к Transform (view) игрока.</summary>
        public void Move(Transform view, Vector3 normalizedMove, float deltaTime)
        {
            if (view == null) return;
            if (normalizedMove.sqrMagnitude <= 0.0000001f) return;
            view.position += ComputeStep(normalizedMove, deltaTime);
        }
    }
}