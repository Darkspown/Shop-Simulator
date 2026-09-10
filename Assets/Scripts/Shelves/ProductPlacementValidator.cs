using ShelfRush.Products;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Default implementation of <see cref="IProductPlacementValidator"/> (Task 08)..
    /// Validation order: <see cref="ShelfPlacementFailReason.InvalidProduct"/>, then
    /// <see cref="ShelfPlacementFailReason.InvalidShelf"/>, then
    /// <see cref="ShelfPlacementFailReason.CategoryMismatch"/>, then
    /// <see cref="ShelfPlacementFailReason.ShelfFull"/>; else <see cref="ProductPlacementResult.Ok"/>.
    ///
    /// Extensibility points ( for VIP shelves / special products / temporary shelves / quests / bonuses ):
    /// <list type="bullet">
    ///   <item><see cref="CanBypassCategory"/> - true lets any category pass ( VIP shelf / special goods ).</item>
    ///   <item><see cref="EffectiveCapacity"/> - override to provide bonus / temporary capacity.</item>
    ///   <item><see cref="IsInvalidProduct"/>, <see cref="IsInvalidShelf"/> - override to relax rules for special data.</item>
    /// </list>
    /// Registered in <see cref="ShelfRush.Core.GameBootstrap"/> as a service; <see cref="ShelfController"/>
    /// also falls back to a local instance when bootstrap is not available.
    /// </summary>
    public class ProductPlacementValidator : IProductPlacementValidator, Core.IGameService
    {
        /// <inheritdoc />
        public virtual ProductPlacementResult CanPlace(ProductData product, ShelfData shelf) =>
            CanPlace(product, shelf, currentAmount: 0);

        /// <inheritdoc />
        public virtual ProductPlacementResult CanPlace(ProductData product, ShelfData shelf, int currentAmount)
        {
            // 1. Argument validity ( highest priority ).
            if (IsInvalidProduct(product)) return ProductPlacementResult.Fail(ShelfPlacementFailReason.InvalidProduct);
            if (((IsInvalidShelf(shelf)))) return ProductPlacementResult.Fail(ShelfPlacementFailReason.InvalidShelf);

            // 2. Capacity ( a shelf must be able to hold at least one item ).
            var capacity = EffectiveCapacity(product, shelf);
            if ((capacity <= 0)) return ProductPlacementResult.Fail(ShelfPlacementFailReason.ShelfFull);

            // 3. Category ( VIP / special goods may bypass it ).
            if (!CanBypassCategory(product, shelf) && !CategoryMatches(product, shelf))
                return ProductPlacementResult.Fail(ShelfPlacementFailReason.CategoryMismatch);

            // 4. Filling ( current amount must be less than effective capacity ).
            if ((currentAmount >= capacity)) return ProductPlacementResult.Fail(ShelfPlacementFailReason.ShelfFull);

            return ProductPlacementResult.Ok;
        }

        /// <summary>Product is invalid when null or missing a category (not configured)..</summary>
        protected virtual bool IsInvalidProduct(ProductData product) => product == null || product.Category == null;

        /// <summary>Shelf is invalid when null or missing AllowedCategory (not configured)..</summary>
        protected virtual bool IsInvalidShelf(ShelfData shelf) => shelf == null || shelf.AllowedCategory == null;

        /// <summary>Category matches when both sides reference the same ScriptableObject.</summary>
        protected virtual bool CategoryMatches(ProductData product, ShelfData shelf) =>
            ReferenceEquals(product.Category, shelf.AllowedCategory);

        /// <summary>Override to allow VIP shelves / special products to bypass the category check.</summary>
        protected virtual bool CanBypassCategory(ProductData product, ShelfData shelf) => false;

        /// <summary>Effective placement capacity of a shelf ( default: slot count ).</summary>
        protected virtual int EffectiveCapacity(ProductData product, ShelfData shelf) => shelf != null ? shelf.SlotCapacity :0;

        // stateless service
        public void Initialize(Core.ServiceLocator services) { }
        public void Dispose() { }
    }
}