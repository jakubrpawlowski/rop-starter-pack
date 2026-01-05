using RopStarterPack;

namespace Demo;

public static class TypedErrorsDemo
{
    record User(string Id, string Name);

    record Order(string Id, string UserId, string ProductId, int Quantity);

    public static async Task Run()
    {
        Console.WriteLine("\n=== Typed Errors Demo ===");

        // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
        //   Fake External Services
        // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
        async Task<User?> UserServiceGetAsync(string userId)
        {
            await Task.Delay(10);
            return userId switch
            {
                "user-1" => new User("user-1", "Alice"),
                "crash" => throw new Exception("User DB connection failed"),
                _ => null,
            };
        }

        async Task<Order?> OrderServiceGetAsync(string orderId)
        {
            await Task.Delay(10);
            return orderId switch
            {
                "order-1" => new Order("order-1", "user-1", "product-1", 5),
                "order-2" => new Order("order-2", "user-1", "product-1", 100),
                "order-3" => new Order("order-3", "user-missing", "product-1", 1),
                "order-4" => new Order("order-4", "crash", "product-1", 1),
                "order-5" => new Order("order-5", "user-1", "product-missing", 1),
                "crash" => throw new Exception("Order API timeout"),
                _ => null,
            };
        }

        async Task<int> InventoryServiceCheckAsync(string productId)
        {
            await Task.Delay(10);
            return productId switch
            {
                "product-1" => 10,
                _ => throw new Exception("Inventory service down"),
            };
        }

        // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
        //   Wrappers
        // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

        // ─────────────────────────────────────────────────────────────────
        //   GetUserSafe
        // ─────────────────────────────────────────────────────────────────
        async Task<Result<User, AppError>> GetUserAsync(string userId)
        {
            var user = await UserServiceGetAsync(userId);
            return Result.FromNullable(user, AppError.UserNotFoundErr(userId));
        }

        Task<Result<User, AppError>> GetUserSafe(string userId) =>
            Result.From(() => GetUserAsync(userId), AppError.UserCrashed);

        // ─────────────────────────────────────────────────────────────────
        //   GetOrderSafe
        // ─────────────────────────────────────────────────────────────────
        async Task<Result<Order, AppError>> GetOrderAsync(string orderId)
        {
            var order = await OrderServiceGetAsync(orderId);
            return Result.FromNullable(order, AppError.OrderNotFoundErr(orderId));
        }

        Task<Result<Order, AppError>> GetOrderSafe(string orderId) =>
            Result.From(() => GetOrderAsync(orderId), AppError.OrderCrashed);

        // ─────────────────────────────────────────────────────────────────
        //   CheckInventorySafe
        // ─────────────────────────────────────────────────────────────────
        async Task<Result<int, AppError>> CheckInventoryAsync(string productId, int requested)
        {
            var available = await InventoryServiceCheckAsync(productId);
            return requested > available
                ? Result.Err<int, AppError>(
                    AppError.InsufficientStockErr(productId, requested, available)
                )
                : Result.Ok<int, AppError>(available);
        }

        Task<Result<int, AppError>> CheckInventorySafe(string productId, int requested) =>
            Result.From(() => CheckInventoryAsync(productId, requested), AppError.InventoryCrashed);

        // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
        //   Business Logic
        // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄

        // ─────────────────────────────────────────────────────────────────
        //   Fluent style
        // ─────────────────────────────────────────────────────────────────
        Task<Result<string, AppError>> ProcessOrder(string orderId) =>
            GetOrderSafe(orderId)
                .AndThen(order => GetUserSafe(order.UserId).Map(user => (order, user)))
                .AndThen(x =>
                    CheckInventorySafe(x.order.ProductId, x.order.Quantity)
                        .Map(stock => (x.order, x.user, stock))
                )
                .Map(x =>
                    $"Order {x.order.Id} for {x.user.Name}: {x.order.Quantity} of {x.order.ProductId} (stock: {x.stock})"
                );

        // ─────────────────────────────────────────────────────────────────
        //   LINQ style (all bindings stay in scope)
        // ─────────────────────────────────────────────────────────────────
        Task<Result<string, AppError>> ProcessOrderLinq(string orderId) =>
            from order in GetOrderSafe(orderId)
            from user in GetUserSafe(order.UserId)
            from stock in CheckInventorySafe(order.ProductId, order.Quantity)
            select $"Order {order.Id} for {user.Name}: {order.Quantity} of {order.ProductId} (stock: {stock})";

        // ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
        //   Test all scenarios
        // ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
        Console.WriteLine("\nProcessOrder scenarios (fluent):");

        var scenarios = new[]
        {
            "order-1", // SUCCESS
            "order-2", // InsufficientStock
            "order-3", // UserNotFound
            "order-4", // UserServiceCrashed
            "order-5", // InventoryServiceCrashed
            "order-missing", // OrderNotFound
            "crash", // OrderServiceCrashed
        };

        foreach (var scenario in scenarios)
        {
            var result = await Result.From(() => ProcessOrder(scenario), AppError.FromException);
            var output = result.Match(
                ok: msg => $"SUCCESS: {msg}",
                err: e =>
                    e switch
                    {
                        AppError.UserNotFound(var id) => $"ERROR: User '{id}' not found",
                        AppError.OrderNotFound(var id) => $"ERROR: Order '{id}' not found",
                        AppError.InsufficientStock(var p, var req, var avail) =>
                            $"ERROR: Not enough {p} (need {req}, have {avail})",
                        AppError.UserServiceCrashed(var ex) =>
                            $"CRASH: User service - {ex.Message}",
                        AppError.OrderServiceCrashed(var ex) =>
                            $"CRASH: Order service - {ex.Message}",
                        AppError.InventoryServiceCrashed(var ex) =>
                            $"CRASH: Inventory service - {ex.Message}",
                        AppError.UnknownCrash(var ex) => $"CRASH: Unknown - {ex.Message}",
                        _ => "ERROR: Unknown",
                    }
            );
            Console.WriteLine($"  ProcessOrder(\"{scenario}\"): {output}");
        }

        // Same test with LINQ style
        Console.WriteLine("\nProcessOrder scenarios (LINQ):");

        foreach (var scenario in scenarios)
        {
            var result = await Result.From(
                () => ProcessOrderLinq(scenario),
                AppError.FromException
            );
            var output = result.Match(
                ok: msg => $"SUCCESS: {msg}",
                err: e =>
                    e switch
                    {
                        AppError.UserNotFound(var id) => $"ERROR: User '{id}' not found",
                        AppError.OrderNotFound(var id) => $"ERROR: Order '{id}' not found",
                        AppError.InsufficientStock(var p, var req, var avail) =>
                            $"ERROR: Not enough {p} (need {req}, have {avail})",
                        AppError.UserServiceCrashed(var ex) =>
                            $"CRASH: User service - {ex.Message}",
                        AppError.OrderServiceCrashed(var ex) =>
                            $"CRASH: Order service - {ex.Message}",
                        AppError.InventoryServiceCrashed(var ex) =>
                            $"CRASH: Inventory service - {ex.Message}",
                        AppError.UnknownCrash(var ex) => $"CRASH: Unknown - {ex.Message}",
                        _ => "ERROR: Unknown",
                    }
            );
            Console.WriteLine($"  ProcessOrderLinq(\"{scenario}\"): {output}");
        }
    }
}
