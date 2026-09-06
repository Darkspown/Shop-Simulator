using System.Collections.Generic;
using ShelfRush.Player.View;
using ShelfRush.Products;
using UnityEngine;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Выделенная зона перед полкой, в которую игрок «наступает», и товары из его рук
    /// **перелетают** на полку (ShelfController.TryPlaceFlying → DOTween от игрока к свободному слоту).
    ///
    /// Зона создаётся из объекта <b>Plane</b> (<c>GameObject > 3D Object > Plane</c>): плоскость
    /// кладётся на пол перед полкой, масштабом Plane настраивается её размер (ширина/глубина).
    /// В Awake concave MeshCollider плоскости автоматически заменяется BoxCollider'ом
    /// (Unity не поддерживает триггеры на concave MeshCollider). Подсветка отсутствует.
    ///
    /// Как работает:
    /// - Игрок движется transform'ом без Rigidbody (см. PlayerMovement), поэтому обычный
    ///   `OnTriggerEnter` физикой НЕ вызывается. Вместо этого зона периодически ({scanInterval})
    ///   проверяет, находится ли позиция игрока внутри её BoxCollider ({IsInsideZone}).
    /// - Когда игрок сначала внутри и у него в руках есть товар — товары «перелетают» на полку
    ///   (PlaceAll → TryPlaceFlying), размещённое снимается с рук (TryDrop); остаток
    ///   (не поместившийся / неверная категория) остаётся у игрока.
    /// - После выкладки зона не срабатывает повторно, пока игрок не выйдет и не зайдёт снова
    ///   (или не возьмёт новые товары, находясь внутри).
    /// - Игровое решение принимает ShelfController по данным (Category == AllowedCategory + слот).
    ///
    /// Настройка: повесьте этот компонент на объект Plane перед полкой. Rigidbody не требуется.
    /// </summary>
    [AddComponentMenu("ShelfRush/Shelves/Shelf Interaction Zone (Plane)")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class ShelfInteractionZone : MonoBehaviour
    {
        [Tooltip("Контроллер полки (логика размещения). Если пусто — ищется в родителе.")]
        [SerializeField] private ShelfController controller;

        [Tooltip("В Awake привести Collider к триггер-виду (заменить concave MeshCollider Plane на BoxCollider по габаритам меша).")]
        [SerializeField] private bool autoConfigureCollider = true;

        [Tooltip("Размещать товары сразу при входе в зону.")]
        [SerializeField] private bool autoPlaceOnEnter = true;

        [Tooltip("Задержка между «перелётами» отдельных товаров (чтобы летели вереницей), сек.")]
        [SerializeField] private float flyDelayStep = 0.12f;

        [Tooltip("Как часто (сек) зона проверяет, стоит ли игрок внутри. Нужно, потому что игрок " +
                 "двигается transform'ом без Rigidbody, и OnTriggerEnter не вызывается.")]
        [SerializeField] private float scanInterval = 0.1f;

        private PlayerController _player; // кеш игрока (единственный)
        private bool _playerInside;
        private bool _placedThisStay = true;
        private bool _prevEmptyInside = true;
        private float _scanTimer;

        private void Awake()
        {
            if (controller == null) controller = GetComponentInParent<ShelfController>();
            if (autoConfigureCollider) EnsureBoxTrigger();
        }

        private void Update()
        {
            if (!autoPlaceOnEnter) return;

            // Периодическая проверка близости: работает и без физики (игрок движется transform.position).
            _scanTimer -= Time.deltaTime;
            if (_scanTimer > 0f) return;
            _scanTimer = Mathf.Max(0.02f, scanInterval);

            var player = ResolvePlayer();
            var inside = player != null && IsInsideZone(player.transform.position);

            if (inside != _playerInside)
            {
                _playerInside = inside;
                if (inside)
                {
                    _placedThisStay = false;  // только что вошёл — можно выкладывать
                    _prevEmptyInside = true;
                }
                else _placedThisStay = true;
            }

            if (!_playerInside) return;
            var carry = player != null ? player.Carry : null;
            if (carry == null) return;

            // Если внутри руки снова наполнились (были пусты) — разрешаем выкладку ещё раз.
            if (!carry.IsEmpty && _prevEmptyInside)
            {
                _prevEmptyInside = false;
                _placedThisStay = false;
            }
            if (carry.IsEmpty) _prevEmptyInside = true;

            if (_placedThisStay) return;
            _placedThisStay = true;
            PlaceAll(player, carry, carry.Items);
        }

        private PlayerController ResolvePlayer()
        {
            if (_player != null && _player.gameObject.activeSelf) return _player;
            _player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            return _player;
        }

        private bool IsInsideZone(Vector3 worldPos)
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return false;

            var b = box.bounds;
            b.Expand(new Vector3(0.2f, 0.8f, 0.2f)); // небольшой запас по высоте/границам
            return b.Contains(worldPos);
        }

        /// <summary>
        /// Разместить все несущиеся товары с «перелётом» от позиции игрока.
        /// Размещённое снимается с рук (TryDrop); остаток (не подходящая категория / полка
        /// заполнилась) остаётся у игрока.
        /// </summary>
        private void PlaceAll(PlayerController player, PlayerCarry carry, IReadOnlyList<ProductData> items)
        {
            if (controller == null) return;
            var flyFrom = player != null ? player.transform.position : transform.position;
            var delay = 0f;

            for (var i = 0; i < items.Count; i++)
            {
                var product = items[i];
                if (product == null) continue;
                if (controller.IsFull) break; // доступное количество исчерпано — остаток у игрока

                if (controller.TryPlaceFlying(product, flyFrom, delay, out _))
                {
                    carry.TryDrop(product); // убираем из рук только фактически размещённый
                    delay += Mathf.Max(0f, flyDelayStep);
                }
                // Неудачу (категория не совпала) пропускаем — товар остаётся в руках.
            }
        }

        /// <summary>
        /// Гарантирует, что зона имеет BoxCollider-триггер (вызывается НЕ в OnValidate, а в Awake,
        /// чтобы не делать Destroy/AddComponent во время редакторных колбэков).
        /// MeshCollider у Plane по умолчанию concave (convex=false), а Unity не поддерживает
        /// <c>isTrigger = true</c> на concave MeshCollider — поэтому заменяем его BoxCollider'ом,
        /// повторяющим габариты меша плоскости с небольшой толщиной по высоте. Размер зоны
        /// настраивается масштабом Plane (BoxCollider.size задан в локальном пространстве).
        /// </summary>
        private void EnsureBoxTrigger()
        {
            // Все коллайдеры на объекте. Раньше багованный OnValidate мог оставить и MeshCollider,
            // и BoxCollider одновременно — предпочитаем BoxCollider и удаляем все concave MeshCollider'ы.
            var all = GetComponents<Collider>();

            BoxCollider box = null;
            for (var i = 0; i < all.Length; i++)
            {
                if (box == null && all[i] is BoxCollider bx) box = bx;
            }

            // Удаляем все MeshCollider'ы и лишние коллайдеры (по ленивому Destroy — безопасно в Awake).
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i] is MeshCollider || (box != null && !ReferenceEquals(all[i], box)))
                {
                    Destroy(all[i]);
                }
            }

            if (box != null)
            {
                box.isTrigger = true;
                return;
            }

            // Нет BoxCollider'а: если был MeshCollider — используем его габариты для нового BoxCollider.
            var mesh = GetComponent<MeshCollider>();
            if (mesh != null)
            {
                var size = mesh.sharedMesh != null ? mesh.sharedMesh.bounds.size : Vector3.one;
                var center = mesh.sharedMesh != null ? mesh.sharedMesh.bounds.center : Vector3.zero;
                if (size.y < 0.1f) size.y = 0.2f; // у плоскости нулевая высота — даём толщину

                box = gameObject.AddComponent<BoxCollider>();
                box.size = size;
                box.center = new Vector3(center.x, center.y + size.y * 0.5f, center.z);
                Destroy(mesh);
                box.isTrigger = true;
                return;
            }

            // Совсем нет коллайдера — добавляем BoxCollider.
            box = gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
        }
    }
}