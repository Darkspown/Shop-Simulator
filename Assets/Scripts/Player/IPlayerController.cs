using System.Collections.Generic;
using ShelfRush.Products;
using ShelfRush.Shelves;
using UnityEngine;

namespace ShelfRush.Player
{
    /// <summary>
    /// Контроллер игрока. Читает ввод, двигает привязанный Transform (сцену), ведёт
    /// «инвентарь» игрока и взаимодействует с полками (IStockService) и клиентами.
    /// Контракт для внешних систем.
    /// </summary>
    public interface IPlayerController : Core.IGameService, Core.ITickable
    {
        /// <summary>Визуальный объект игрока, привязывается при создании сцены.</summary>
        Transform View { get; set; }

        /// <summary>Товары, которые игрок несёт в руках.</summary>
        IReadOnlyList<ProductData> Carried { get; }

        int CarryCapacity { get; }

        /// <summary>Взять один товар с указанной полки (если игрок может нести).</summary>
        bool TryPickUp(ShelfData shelf);

        /// <summary>Выполнить заказ клиента одним из товаров из рук.</summary>
        bool TryDeliver(Customers.CustomerOrder order);
    }
}