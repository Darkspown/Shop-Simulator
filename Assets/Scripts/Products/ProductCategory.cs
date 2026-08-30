using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Категория товара (ScriptableObject — Data). Type-safe: категория идентифицируется
    /// по ссылке на объект, а НЕ по строке. Строковые поля допустимы только как
    /// display-метки (название для UI), но не для игровой логики/маппингов.
    ///
    /// Каждая категория — отдельный ассет, который выбирается в <see cref="ProductData"/>
    /// полем <c>Category</c>. Добавление новой категории не требует изменения кода —
    /// достаточно создать новый ассет через меню <c>Create/ShelfRush/Products/Category</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "ProductCategory", menuName = "ShelfRush/Products/Category")]
    public sealed class ProductCategory : ScriptableObject
    {
        [Tooltip("Отображаемое название категории (для UI). Не используется как идентификатор.")]
        [SerializeField] private string displayName;

        [Tooltip("Цвет категории (используется в UI, подсветке полок/ценников).")]
        [SerializeField] private Color color = Color.white;

        /// <summary>Отображаемое название (только для UI).</summary>
        public string DisplayName => displayName;

        /// <summary>Цвет категории.</summary>
        public Color Color => color;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }
    }
}