using UnityEngine;

namespace ShelfRush.Player
{
    /// <summary>Настройки игрока (ScriptableObject — Data).</summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "ShelfRush/Player")]
    public sealed class PlayerConfig : ScriptableObject
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private int carryCapacity = 4;
        [SerializeField] private float pickupRadius = 1.2f;

        public float MoveSpeed => moveSpeed;
        public int CarryCapacity => Mathf.Max(1, carryCapacity);
        public float PickupRadius => pickupRadius;
    }
}