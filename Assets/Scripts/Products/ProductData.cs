using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Статические данные товара (ScriptableObject — Data).
    /// Идентификация объекта — по ссылке (airbrach); <see cref="Id"/> — стабильный код
    /// только для сохранений/ключей. Строковая идентификация категорий в логике не используется.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductData", menuName = "ShelfRush/Product")]
    public sealed class ProductData : ScriptableObject
    {
        [Tooltip("Стабильный идентификатор для сохранений (генерируется автоматически).")]
        [SerializeField] private string id;

        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;
        [SerializeField] private int basePrice = 10;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public int BasePrice => basePrice;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }
    }
}