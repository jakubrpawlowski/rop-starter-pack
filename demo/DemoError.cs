using RopStarterPack;

namespace Demo;

// Simple error for basic demos
record DemoError(string Message) : IFromException<DemoError>
{
    public static DemoError FromException(Exception ex) => new DemoError($"Caught: {ex.Message}");
}

// Sum type - all errors for the entire app in one place
abstract record AppError : IFromException<AppError>
{
    // Typed errors (business logic)
    public record UserNotFound(string UserId) : AppError;

    public record OrderNotFound(string OrderId) : AppError;

    public record InsufficientStock(string ProductId, int Requested, int Available) : AppError;

    // Crash errors (preserve Exception for logging)
    public record UserServiceCrashed(Exception Ex) : AppError;

    public record OrderServiceCrashed(Exception Ex) : AppError;

    public record InventoryServiceCrashed(Exception Ex) : AppError;

    public record UnknownCrash(Exception Ex) : AppError;

    // Factory methods (return AppError to help type inference)
    public static AppError UserNotFoundErr(string userId) => new UserNotFound(userId);

    public static AppError OrderNotFoundErr(string orderId) => new OrderNotFound(orderId);

    public static AppError InsufficientStockErr(string productId, int requested, int available) =>
        new InsufficientStock(productId, requested, available);

    public static AppError UserCrashed(Exception ex) => new UserServiceCrashed(ex);

    public static AppError OrderCrashed(Exception ex) => new OrderServiceCrashed(ex);

    public static AppError InventoryCrashed(Exception ex) => new InventoryServiceCrashed(ex);

    // Fallback for unhandled exceptions
    public static AppError FromException(Exception ex) => new UnknownCrash(ex);
}
