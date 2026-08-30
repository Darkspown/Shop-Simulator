using UnityEngine;

namespace ShelfRush.Products
{
    /// <summary>
    /// Вью-компонент визуала товара (MonoBehaviour). Единственная ответственность — визуальный
    /// вид runtime-товара: применяет настройки (масштаб/тон) из <see cref="ProductData"/> и
    /// позволяет временно менять цвет (подсветка при взаимодействии/выборе).
    ///
    /// Применение цвета идёт через <c>MaterialPropertyBlock</c> — без создания клонов
    /// материалов (share Renderer.sharedMaterial), что важно для pooling.
    ///
    /// Компонент НЕ содержит игровую логику. Монтируется на префаб товара (обычно дочерний
    /// объект с Renderer), а ProductSpawner получает его из компонента <see cref="Product"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProductVisual : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Tooltip("Renderer'ы товара, которые будут перекрашиваться. Если пусто — заполняются автоматически.\n" +
                 "Выключение Renderer безопасности: всегда перечитываем заново в Awake.")]
        [SerializeField] private Renderer[] renderers;

        private MaterialPropertyBlock _block;
        private Color _baseTint = Color.white;
        private bool _blockCreated;

        /// <summary>Применить данные товара (масштаб + базовый тон).</summary>
        public void Apply(ProductData data)
        {
            if (data == null) return;
            _baseTint = data.VisualSettings.Tint;
            ApplyVisual(data.VisualSettings);
        }

        /// <summary>Применить визуальные настройки (масштаб, тон) из ProductData.</summary>
        public void ApplyVisual(VisualSettings settings)
        {
            if (settings == null) return;
            transform.localScale = settings.Scale;
            _baseTint = settings.Tint;
            SetTint(_baseTint);
        }

        /// <summary>Временно перекрасить товар (подсветка). Хранит базовый тон.</summary>
        public void SetTint(Color color) => ApplyColor(color);

        /// <summary>Вернуть исходный тон из ProductData.</summary>
        public void ResetTint() => ApplyColor(_baseTint);

        private void Awake()
        {
            CollectRenderers();
        }

        private void OnDisable()
        {
            // Пункт «reset state» перед возвратом в пул: возвращаем базовый тон.
            // Твины здесь не ведутся — только цвет.
            if (_blockCreated) ResetTint();
        }

        private void ApplyColor(Color color)
        {
            if (!_blockCreated)
            {
                _block = new MaterialPropertyBlock();
                _blockCreated = true;
            }

            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(ColorId, color);
                r.SetPropertyBlock(_block);
            }
        }

        private void CollectRenderers()
        {
            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                for (var i = 0; i < renderers.Length; i++)
                    if (renderers[i] == null) renderers[i] = null;
            }
        }
    }
}