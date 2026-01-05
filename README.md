# RopStarterPack

[![NuGet](https://img.shields.io/nuget/v/RopStarterPack.svg)](https://www.nuget.org/packages/RopStarterPack)

Simple Railway Oriented Programming library for C#.

## Rationale

Minimal by design. Just essentials. Beginner friendly. Two goals: enforce code
consistency and solve error handling.

## Install

```bash
dotnet add package RopStarterPack
```

## Usage

Define your error type with factories:

```csharp
using RopStarterPack;

abstract record AppError : IFromException<AppError>
{
    public record UserNotFound(string UserId) : AppError;
    public record OrderNotFound(string OrderId) : AppError;
    public record UnknownError(Exception Ex) : AppError;

    // Factories (help type inference)
    public static AppError UserNotFoundErr(string userId) => new UserNotFound(userId);
    public static AppError OrderNotFoundErr(string orderId) => new OrderNotFound(orderId);

    // Catch-all for unhandled exceptions
    public static AppError FromException(Exception ex) => new UnknownError(ex);
}
```

Wrap external calls:

```csharp
async Task<Result<Order, AppError>> GetOrderSafe(string orderId) =>
    await Result.From(
        async () => {
            var order = await db.GetOrder(orderId);
            return Result.FromNullable(order, AppError.OrderNotFoundErr(orderId));
        },
        AppError.FromException
    );

async Task<Result<User, AppError>> GetUserSafe(string userId) =>
    await Result.From(
        async () => {
            var user = await db.GetUser(userId);
            return Result.FromNullable(user, AppError.UserNotFoundErr(userId));
        },
        AppError.FromException
    );
```

Chain operations:

```csharp
Task<Result<string, AppError>> ProcessOrder(string orderId) =>
    GetOrderSafe(orderId)
        .AndThen(order => GetUserSafe(order.UserId))
        .Map(user => $"Order belongs to {user.Name}");
```

Handle at the boundary:

```csharp
var result = await ProcessOrder("order-123");

var response = result.Match(
    ok: msg => Ok(msg),
    err: e => e switch
    {
        AppError.UserNotFound(var id) => NotFound($"User {id} not found"),
        AppError.OrderNotFound(var id) => NotFound($"Order {id} not found"),
        AppError.UnknownError(var ex) => InternalError(ex.Message)
    }
);
```

## License

MIT
