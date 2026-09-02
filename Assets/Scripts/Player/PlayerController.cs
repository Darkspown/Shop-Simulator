using System.Collections.Generic;
using ShelfRush.Core;
using ShelfRush.Input;
using ShelfRush.Levels;
using ShelfRush.Products;
using ShelfRush.Shelves;
using UnityEngine;

namespace ShelfRush.Player
{
    /// <summary>
    /// Реализация контроллера игрока (plain C#, тикается из GameBootstrap).
    /// Движение делегируется в <see cref="PlayerMovement"/>, а ввод приходит уже
    /// нормализованным через <see cref="IPlayerInput"/> — геймплей не знает, откуда
    /// был получен input (клавиатура/тач/джойстик/геймпад).
    /// Ведёт «инвентарь» игрока, берёт товар с полки (IStockService) и доставляет заказ клиенту.
    /// </summary>
    public sealed class PlayerController : IPlayerController
    {
        private readonly List<ProductData> _carried = new List<ProductData>();
        private IPlayerInput _input;
        private IStockService _stock;
        private Customers.ICustomerService _customers;
        private PlayerConfig _config;
        private IEventBus _events;
        private PlayerMovement _movement;
        private ILevelManager _levels;

        public Transform View { get; set; }

        public IReadOnlyList<ProductData> Carried => _carried;

        /// <summary>
        /// Вместимость переноски. Источник — текущий уровень (прогрессия не хранится здесь),
        /// фолбэк — PlayerConfig.
        /// </summary>
        public int CarryCapacity
        {
            get
            {
                var level = _levels?.Current;
                if (level != null) return level.CarryCapacity;
                return _config != null ? _config.CarryCapacity : 1;
            }
        }

        public void Initialize(ServiceLocator services)
        {
            _input = services.Get<IPlayerInput>();
            _stock = services.Get<IStockService>();
            _customers = services.Get<Customers.ICustomerService>();
            _events = services.Get<IEventBus>();
            services.TryGet<PlayerConfig>(out _config);
            services.TryGet<ILevelManager>(out _levels);
            _movement = new PlayerMovement(_config);
        }

        public void Dispose()
        {
            _carried.Clear();
            _input = null;
            _stock = null;
            _customers = null;
            _events = null;
            _config = null;
            _movement = null;
            _levels = null;
        }

        public void Tick(float deltaTime)
        {
            if (View == null || _input == null || _movement == null) return;

            // Движение: нормализованный ввод -> PlayerMovement (без знания об источнике ввода).
            _movement.Move(View, _input.MoveWorld, deltaTime);
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