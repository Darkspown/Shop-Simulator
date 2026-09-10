using ShelfRush.Products;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Contract of a dedicated product placement validator ( Task 08: Product Category Validation ).
    /// Isolates the decision ( can a product be placed on a shelf ) from UI / events / rendering:
    /// the single source of truth for product-to-shelf rules. It is designed to stay extensible for
    /// VIP shelves, special products, temporary shelves, quests and bonuses: standard implementation
    /// <see cref="ProductPlacementValidator"/> defines protected virtual hooks that derived rules can
    /// override (bypass-category for VIP / special products, effective capacity for temporary / bonus)..
    /// </summary>
    public interface IProductPlacementValidator
    {
        /// <summary>Pure data check: only product / shelf, ignores runtime current amount
        /// (fullness is the judged by shelf capacity only.</summary>
        ProductPlacementResult CanPlace(ProductData product, ShelfData shelf);

        /// <summary>Check with the caller-provided current amount ( honest ShelfFull answer ).</summary>
        ProductPlacementResult CanPlace(ProductData product, ShelfData shelf, int currentAmount);
    }
}