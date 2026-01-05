namespace RopStarterPack;

/// <summary>
/// Error types must implement this to support automatic exception capture in async chains.
/// </summary>
public interface IFromException<E>
{
    static abstract E FromException(Exception ex);
}

/// <summary>
/// Discriminated union: either Ok(value) or Err(error). Never both, never neither.
/// </summary>
public abstract record Result<T, E>
{
    // Private constructor prevents external inheritance - only Ok and Err can exist
    private Result() { }

    // The two cases
    public sealed record Ok(T Value) : Result<T, E>;

    public sealed record Err(E Error) : Result<T, E>;

    // Exhaustive pattern match - forces you to handle both cases
    public TResult Match<TResult>(Func<T, TResult> ok, Func<E, TResult> err) =>
        this switch
        {
            Ok(var value) => ok(value),
            Err(var error) => err(error),
            _ => throw new InvalidOperationException("Unreachable"),
        };

    // Transform success value (can't fail)
    public Result<U, E> Map<U>(Func<T, U> f) =>
        this switch
        {
            Ok(var value) => new Result<U, E>.Ok(f(value)),
            Err(var error) => new Result<U, E>.Err(error),
            _ => throw new InvalidOperationException("Unreachable"),
        };

    // Chain operations that can fail (aka FlatMap)
    public Result<U, E> AndThen<U>(Func<T, Result<U, E>> f) =>
        this switch
        {
            Ok(var value) => f(value),
            Err(var error) => new Result<U, E>.Err(error),
            _ => throw new InvalidOperationException("Unreachable"),
        };

    // LINQ support
    public Result<U, E> SelectMany<U>(Func<T, Result<U, E>> bind) => AndThen(bind);

    public Result<V, E> SelectMany<U, V>(Func<T, Result<U, E>> bind, Func<T, U, V> project) =>
        this switch
        {
            Ok(var t) => bind(t) switch
            {
                Result<U, E>.Ok(var u) => new Result<V, E>.Ok(project(t, u)),
                Result<U, E>.Err(var e) => new Result<V, E>.Err(e),
                _ => throw new InvalidOperationException("Unreachable"),
            },
            Err(var e) => new Result<V, E>.Err(e),
            _ => throw new InvalidOperationException("Unreachable"),
        };
}

/// <summary>
/// Static factory for creating Results.
/// </summary>
public static class Result
{
    // Direct constructors - cleaner than new Result<T,E>.Ok/Err
    public static Result<T, E> Ok<T, E>(T value) => new Result<T, E>.Ok(value);

    public static Result<T, E> Err<T, E>(E error) => new Result<T, E>.Err(error);

    // Sync - wrap throwing operations
    public static Result<T, E> From<T, E>(Func<T> operation, Func<Exception, E> toError)
    {
        try
        {
            return new Result<T, E>.Ok(operation());
        }
        catch (Exception ex)
        {
            return new Result<T, E>.Err(toError(ex));
        }
    }

    // Async (raw value)
    public static async Task<Result<T, E>> From<T, E>(
        Func<Task<T>> operation,
        Func<Exception, E> toError
    )
    {
        try
        {
            return new Result<T, E>.Ok(await operation());
        }
        catch (Exception ex)
        {
            return new Result<T, E>.Err(toError(ex));
        }
    }

    // Async (Result)
    public static async Task<Result<T, E>> From<T, E>(
        Func<Task<Result<T, E>>> operation,
        Func<Exception, E> toError
    )
    {
        try
        {
            return await operation();
        }
        catch (Exception ex)
        {
            return new Result<T, E>.Err(toError(ex));
        }
    }

    // Convert nullable to Result - Ok if value, Err if null
    public static Result<T, E> FromNullable<T, E>(T? value, E errorIfNull)
        where T : struct =>
        value.HasValue ? new Result<T, E>.Ok(value.Value) : new Result<T, E>.Err(errorIfNull);

    public static Result<T, E> FromNullable<T, E>(T? value, E errorIfNull)
        where T : class =>
        value is not null ? new Result<T, E>.Ok(value) : new Result<T, E>.Err(errorIfNull);
}

/// <summary>
/// Async extensions for Result.
/// </summary>
public static class ResultExtensions
{
    // Async Match
    public static async Task<TResult> Match<T, E, TResult>(
        this Task<Result<T, E>> self,
        Func<T, TResult> ok,
        Func<E, TResult> err
    )
        where E : IFromException<E>
    {
        try
        {
            var result = await self;
            return result.Match(ok, err);
        }
        catch (Exception ex)
        {
            return err(E.FromException(ex));
        }
    }

    // Async Map (sync f)
    public static async Task<Result<U, E>> Map<T, U, E>(this Task<Result<T, E>> self, Func<T, U> f)
        where E : IFromException<E>
    {
        try
        {
            var result = await self;
            return result.Map(f);
        }
        catch (Exception ex)
        {
            return new Result<U, E>.Err(E.FromException(ex));
        }
    }

    // Async Map (async f)
    public static async Task<Result<U, E>> Map<T, U, E>(
        this Task<Result<T, E>> self,
        Func<T, Task<U>> f
    )
        where E : IFromException<E>
    {
        try
        {
            var result = await self;
            return result switch
            {
                Result<T, E>.Ok(var v) => new Result<U, E>.Ok(await f(v)),
                Result<T, E>.Err(var e) => new Result<U, E>.Err(e),
                _ => throw new InvalidOperationException("Unreachable"),
            };
        }
        catch (Exception ex)
        {
            return new Result<U, E>.Err(E.FromException(ex));
        }
    }

    // Async AndThen (sync f)
    public static async Task<Result<U, E>> AndThen<T, U, E>(
        this Task<Result<T, E>> self,
        Func<T, Result<U, E>> f
    )
        where E : IFromException<E>
    {
        try
        {
            var result = await self;
            return result switch
            {
                Result<T, E>.Ok(var v) => f(v),
                Result<T, E>.Err(var e) => new Result<U, E>.Err(e),
                _ => throw new InvalidOperationException("Unreachable"),
            };
        }
        catch (Exception ex)
        {
            return new Result<U, E>.Err(E.FromException(ex));
        }
    }

    // Async AndThen (async f)
    public static async Task<Result<U, E>> AndThen<T, U, E>(
        this Task<Result<T, E>> self,
        Func<T, Task<Result<U, E>>> f
    )
        where E : IFromException<E>
    {
        try
        {
            var result = await self;
            return result switch
            {
                Result<T, E>.Ok(var v) => await f(v),
                Result<T, E>.Err(var e) => new Result<U, E>.Err(e),
                _ => throw new InvalidOperationException("Unreachable"),
            };
        }
        catch (Exception ex)
        {
            return new Result<U, E>.Err(E.FromException(ex));
        }
    }

    // Async LINQ support
    public static Task<Result<U, E>> SelectMany<T, U, E>(
        this Task<Result<T, E>> self,
        Func<T, Task<Result<U, E>>> bind
    )
        where E : IFromException<E> => self.AndThen(bind);

    public static async Task<Result<V, E>> SelectMany<T, U, V, E>(
        this Task<Result<T, E>> self,
        Func<T, Task<Result<U, E>>> bind,
        Func<T, U, V> project
    )
        where E : IFromException<E>
    {
        try
        {
            var result = await self;
            return result switch
            {
                Result<T, E>.Ok(var t) => await bind(t) switch
                {
                    Result<U, E>.Ok(var u) => new Result<V, E>.Ok(project(t, u)),
                    Result<U, E>.Err(var e) => new Result<V, E>.Err(e),
                    _ => throw new InvalidOperationException("Unreachable"),
                },
                Result<T, E>.Err(var e) => new Result<V, E>.Err(e),
                _ => throw new InvalidOperationException("Unreachable"),
            };
        }
        catch (Exception ex)
        {
            return new Result<V, E>.Err(E.FromException(ex));
        }
    }
}
