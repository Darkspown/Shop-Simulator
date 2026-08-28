using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Данные полки (ScriptableObject — Data): какой товар на ней лежит,
    /// вместимость и точки размещения объектов товаров на полке.
    /// </summary>
    [CreateAssetMenu(fileName = "ShelfData", menuName = "ShelfRush/Shelf")]
    public sealed class ShelfData : ScriptableObject
    {
        [SerializeField] private ProductData product;
        [SerializeField] private int capacity = 8;
        [SerializeField] private Vector3[] placements = System.Array.Empty<Vector3>();

        public ProductData Product => product;
        public int Capacity => Mathf.Max(1, capacity);
        public Vector3[] Placements => placements;
    }
}