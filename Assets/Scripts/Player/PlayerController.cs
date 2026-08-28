using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Input;
using ShelfRush.Products;
using ShelfRush.Shelves;
using UnityEngine;

namespace ShelfRush.Player
{
    /// <summary>
    /// Реализация контроллера игрока (plain C#, тикается из GameBootstrap).
    /// Движение — по вектору ввода с привязанным Transform (View задаётся при создании сцены).
    /// Ведёт «инвентарь» игрока, берёт товар с полки (IStockService) и доставляет заказ клиенту.
    /// </summary>
    public sealed class PlayerController : IPlayerController
    {
        private readonly List<ProductData> _carried = new List<ProductData>();
        private IInputService _input;
        private IStockService _stock;
        private Customers.ICustomerService _customers;
        private PlayerConfig _config;
        private IEventBus _events;

        public Transform View { get; set; }

        public IReadOnlyList<ProductData> Carried => _carried;

        public int CarryCapacity => _config != null ? _config.CarryCapacity : 4;

        public void Initialize(ServiceLocator services)
        {
            _input = services.Get<IInputService>();
            _stock = services.Get<IStockService>();
            _customers = services.Get<Customers.ICustomerService>();
            _events = services.Get<IEventBus>();
            services.TryGet<PlayerConfig>(out _config);
        }

        public void Dispose()
        {
            _carried.Clear();
            _input = null;
            _stock = null;
            _customers = null;
            _events = null;
            _config = null;
        }

        public void Tick(float deltaTime)
        {
            if (View == null || _input == null) return;

            var move = _input.Move;
            if (move.sqrMagnitude > 0.0001f)
            {
                var step = move * (_config != null ? _config.MoveSpeed : 4f) * deltaTime;
                View.position += new Vector3(step.x, 0f, step.y);
            }
        }

        public bool TryPickUp(ShelfData shelf)
        {
            if (_carried.Count >= CarryCapacity) return false;
            if (!_stock.TryTakeProduct(shelf, out var product)) return false;

            _carried.Add(product);
            _events?.Publish(new ProductPickedEvent(product));
            return true;
        }

        public bool TryDeliver(Customers.CustomerOrder order)
        {
            if (order == null) return false;

            for (var i = 0; i < _carried.Count; i++)
            {
                if (!_carried[i].Equals(order.Product)) continue;

                if (_customers.TryCompleteOrder(order, _carried[i]))
                {
                    _carried.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
}