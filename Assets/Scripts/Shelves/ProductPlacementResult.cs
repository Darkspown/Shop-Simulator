using ShelfRush.Products;

namespace ShelfRush.Shelves
{
    /// <summary>
    /// Structured result of placement validation (Task 08: Product Category Validation).
    /// Exposes Allowed, a scalar Reason and explicit boolean flags for each rejection kind,
    /// so callers can branch without comparing enum values. Immutable (readonly) struct:
    /// data only, plus a readable Message for UI/feedback. Produced by IProductPlacementValidator.
    /// </summary>
    public readonly struct ProductPlacementResult
    {
        /// <summary>True when the product can be placed (otherwise see Reason).</summary>
        public bool Allowed { get; }

        /// <summary>Reason of rejection (None when allowed)..</summary>
        public ShelfPlacementFailReason Reason { get; }

        /// <summary>Readable message for UI/feedback (optional).</summary>
        public string Message { get; }

        /// <summary>Product category does not match shelf allowed category.</summary>
        public bool IsCategoryMismatch => !Allowed && Reason == ShelfPlacementFailReason.CategoryMismatch;

        /// <summary>Shelf has no free slots (full)..</summary>
        public bool IsShelfFull => !Allowed && Reason == ShelfPlacementFailReason.ShelfFull;

        /// <summary>Product is null or has no category (not configured)..</summary>
        public bool IsInvalidProduct => !Allowed && Reason == ShelfPlacementFailReason.InvalidProduct;

        /// <summary>Shelf is null or has no AllowedCategory (not configured)..</summary>
        public bool IsInvalidShelf => !Allowed && Reason == ShelfPlacementFailReason.InvalidShelf;



        private ProductPlacementResult(bool allowed, ShelfPlacementFailReason reason, string message)
        {
            Allowed = allowed;
            Reason = reason;
            Message = message;
        }

        /// <summary>Allocated sentinel.</summary>
        public static ProductPlacementResult Ok { get; } =
            new ProductPlacementResult(true, ShelfPlacementFailReason.None, "Placement allowed.");

        /// <summary>Creates a rejection result with a readable message</summary>
        public static ProductPlacementResult Fail(ShelfPlacementFailReason reason, string message = null) =>
            new ProductPlacementResult(false, reason, message ?? DefaultMessage(reason));

        private static string DefaultMessage(ShelfPlacementFailReason reason) =>
            reason switch
            {
                ShelfPlacementFailReason.InvalidProduct =>"Invalid product (null or no category).",
                ShelfPlacementFailReason.InvalidShelf =>"Invalid shelf (null or no AllowedCategory).",
                ShelfPlacementFailReason.CategoryMismatch =>"Category mismatch.",
                ShelfPlacementFailReason.ShelfFull =>"Shelf is full.",
                _ =>"Placement rejected.",
            };
    }
}