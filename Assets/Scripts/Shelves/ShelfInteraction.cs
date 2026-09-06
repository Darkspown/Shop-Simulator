using ShelfRush.Player.View;
using UnityEngine;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Интерактив полки (MonoBehaviour-вью, реализует <see cref="IInteractable"/>).
    /// Связывает игрока (через <see cref="PlayerController"/>/<see cref="PlayerCarry"/>)
    /// с <see cref="ShelfController"/>: при взаимодействии размещает товары из рук игрока
    /// на полку, оставляя остаток (не поместившийся / неверный) у игрока.
    ///
    /// Не содержит игровое решение — только дергает <see cref="ShelfController.TryPlace"/>.
    /// Все проверки (категория, полна ли полка) выполняет сам контроллер и публикует
    /// события; здесь ничего не удаляется/не уничтожается/не начисляется вручную.
    /// Размещается на объекте полки (Collider обязателен для поиска PlayerInteraction).
    /// Подсказка (Prompt) настраивается через унаследованное поле <c>prompt</c>.
    /// </summary>
    [AddComponentMenu("ShelfRush/Shelves/Shelf Interaction")]
    [RequireComponent(typeof(ShelfController))]
    public sealed class ShelfInteraction : InteractableComponent
    {
        private ShelfController _controller;

        private void Awake()
        {
            if (_controller == null) _controller = GetComponent<ShelfController>();
        }

        /// <summary>
        /// Можно ли размещать: полка настроена, есть свободный слот и у игрока есть что размещать.
        /// </summary>
        public override bool CanInteract(PlayerController player)
        {
            if (_controller == null || _controller.Data == null) return false;
            if (_controller.IsFull) return false;
            if (player == null || player.Carry == null) return false;
            return !player.Carry.IsEmpty;
        }

        /// <summary>Размещаем только по кнопке/тапу (вручную), не автоматически при подходе.</summary>
        public override bool AutoInteractOnApproach => false;

        /// <summary>
        /// Разместить товары из рук на полку. Размещается только доступное количество
        /// (пока есть свободные слоты), остаток остаётся у игрока. Неверная категория —
        /// товар не трогается (контроллер публикует OnPlacementFailed → OnCategoryMismatch).
        /// </summary>
        public override void Interact(PlayerController player)
        {
            if (player == null || player.Carry == null) return;
            if (_controller == null) return;

            // Снимок состава рук (TryDrop мутирует carry во время итерации).
            var items = player.Carry.Items;

            for (var i = 0; i < items.Count; i++)
            {
                var product = items[i];
                if (product == null) continue;

                // Размещаем; снимаем с игрока только фактически размещённый товар.
                if (_controller.TryPlace(product, out _))
                {
                    player.Carry.TryDrop(product);
                }
                // Если полка заполнилась — больше не размещаем (остаток остаётся у игрока).
                else if (_controller.IsFull)
                {
                    break;
                }
            }
        }
    }
}