using RopStarterPack;

namespace Demo;

public static class AsyncDemo
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== AsyncResult Type Demo ===");

        // Fake async operation (simulates DB call)
        async Task<Result<int, DemoError>> GetNumberAsync(bool shouldFail)
        {
            await Task.Delay(10);
            return shouldFail
                ? Result.Err<int, DemoError>(new DemoError("DB unavailable"))
                : Result.Ok<int, DemoError>(42);
        }

        // Raw async operation - might throw
        async Task<int> GetNumber(bool shouldCrash)
        {
            await Task.Delay(10);
            if (shouldCrash)
                throw new Exception("Connection timeout!");
            return 42;
        }

        // Safe wrapper - never throws, returns Result
        Task<Result<int, DemoError>> GetNumberSafe(bool shouldCrash) =>
            Result.From(() => GetNumber(shouldCrash), DemoError.FromException);

        // Async Map (sync f)
        Console.WriteLine("\nAsync Map (sync f) demo:");

        var result12 = await GetNumberAsync(false).Map(n => n * 2);
        var result13 = await GetNumberAsync(true).Map(n => n * 2);
        var result14 = await GetNumberSafe(true).Map(n => n * 2);

        Console.WriteLine(
            $"  GetNumberAsync(ok).Map(n => n * 2): {result12.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).Map(n => n * 2): {result13.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberSafe(crash).Map(...): {result14.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );

        // Async AndThen (sync f) - chains a sync function that returns Result
        Console.WriteLine("\nAsync AndThen (sync f) demo:");

        Result<string, DemoError> ValidatePositive(int n) =>
            n > 0
                ? Result.Ok<string, DemoError>($"Valid: {n}")
                : Result.Err<string, DemoError>(new DemoError("Number must be positive"));

        var resultSync1 = await GetNumberAsync(false).AndThen(ValidatePositive);
        var resultSync2 = await GetNumberAsync(true).AndThen(ValidatePositive);
        var resultSync3 = await GetNumberSafe(true).AndThen(ValidatePositive);

        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidatePositive): {resultSync1.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).AndThen(ValidatePositive): {resultSync2.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberSafe(crash).AndThen(ValidatePositive): {resultSync3.Match(ok: s => s, err: e => e.Message)}"
        );

        // Async AndThen (async f) - chains an async function that returns Task<Result>
        Console.WriteLine("\nAsync AndThen (async f) demo:");

        // Raw async validation - might throw
        async Task<Result<string, DemoError>> ValidateAsyncRaw(int n, bool shouldFail)
        {
            await Task.Delay(10);
            if (shouldFail)
                throw new Exception("Validation service crashed!");
            return n >= 10
                ? Result.Ok<string, DemoError>($"Valid: {n}")
                : Result.Err<string, DemoError>(new DemoError("Number too small"));
        }

        // Safe wrapper - catches exceptions
        Task<Result<string, DemoError>> ValidateAsyncSafe(int n, bool shouldFail) =>
            Result.From(() => ValidateAsyncRaw(n, shouldFail), DemoError.FromException);

        var result19 = await GetNumberAsync(false).AndThen(n => ValidateAsyncSafe(n, false));
        var result20 = await GetNumberAsync(true).AndThen(n => ValidateAsyncSafe(n, false));
        var result21 = await GetNumberSafe(true).AndThen(n => ValidateAsyncSafe(n, false));
        var result22 = await GetNumberAsync(false).AndThen(n => ValidateAsyncSafe(n, true));

        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateAsyncSafe ok): {result19.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).AndThen(ValidateAsyncSafe ok): {result20.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberSafe(crash).AndThen(ValidateAsyncSafe ok): {result21.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateAsyncSafe crash): {result22.Match(ok: s => s, err: e => e.Message)}"
        );

        // Async Result.From
        Console.WriteLine("\nAsync Result.From demo:");

        async Task<int> ExternalApiAsync(bool shouldThrow)
        {
            await Task.Delay(10);
            if (shouldThrow)
                throw new Exception("Service unavailable!");
            return 100;
        }

        var result24 = await Result.From(
            () => ExternalApiAsync(false),
            ex => new DemoError(ex.Message)
        );
        var result25 = await Result.From(
            () => ExternalApiAsync(true),
            ex => new DemoError(ex.Message)
        );

        Console.WriteLine(
            $"  Result.From(() => ExternalApiAsync(ok), ...): {result24.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  Result.From(() => ExternalApiAsync(throws), ...): {result25.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );

        // Result.From wrapping Task<Result> - handles both typed errors AND crashes
        Console.WriteLine("\nResult.From with Task<Result> (typed errors + crash handling):");

        // Raw DB call - returns nullable, might throw
        async Task<int?> DbQueryRaw(string mode)
        {
            await Task.Delay(10);
            return mode switch
            {
                "ok" => 42,
                "notfound" => null,
                "crash" => throw new Exception("DB connection lost!"),
                _ => throw new ArgumentException("Invalid mode"),
            };
        }

        // Safe wrapper - handles null -> typed error, crash -> crash error
        Task<Result<int, DemoError>> DbQuerySafe(string mode) =>
            Result.From(
                async () =>
                {
                    var value = await DbQueryRaw(mode);
                    return Result.FromNullable(value, new DemoError("Number not found"));
                },
                ex => new DemoError($"DB crashed: {ex.Message}")
            );

        var result35 = await DbQuerySafe("ok");
        var result36 = await DbQuerySafe("notfound");
        var result37 = await DbQuerySafe("crash");

        Console.WriteLine(
            $"  DbQuerySafe(ok): {result35.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  DbQuerySafe(notfound): {result36.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  DbQuerySafe(crash): {result37.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );

        // Result.AndThen(async f) - sync result calling async fallible op
        Console.WriteLine("\nResult.AndThen(async f) demo:");

        var syncResult = Result.Ok<int, DemoError>(42);
        var syncErr = Result.Err<int, DemoError>(new DemoError("Already failed"));

        var result38 = await syncResult.AndThen(n => ValidateAsyncSafe(n, false));
        var result39 = await syncErr.AndThen(n => ValidateAsyncSafe(n, false));
        var result40 = await syncResult.AndThen(n => ValidateAsyncSafe(n, true));

        Console.WriteLine(
            $"  Ok(42).AndThen(ValidateAsyncSafe ok): {result38.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  Err.AndThen(ValidateAsyncSafe ok): {result39.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  Ok(42).AndThen(ValidateAsyncSafe crash): {result40.Match(ok: s => s, err: e => e.Message)}"
        );

        // Async LINQ syntax
        Console.WriteLine("\nAsync LINQ syntax demo:");

        var result41 = await (
            from n in GetNumberSafe(false)
            from validated in ValidateAsyncSafe(n, false)
            select $"Got {n}, {validated}"
        );

        var result42 = await (
            from n in GetNumberSafe(true)
            from validated in ValidateAsyncSafe(n, false)
            select $"Got {n}, {validated}"
        );

        Console.WriteLine(
            $"  from n in GetNumberSafe(ok) ...: {result41.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  from n in GetNumberSafe(crash) ...: {result42.Match(ok: s => s, err: e => e.Message)}"
        );

        // Async Match
        Console.WriteLine("\nAsync Match demo:");

        var output1 = await GetNumberAsync(false).Match(ok: n => $"Got {n}", err: e => e.Message);
        var output2 = await GetNumberAsync(true).Match(ok: n => $"Got {n}", err: e => e.Message);
        var output3 = await GetNumberSafe(true).Match(ok: n => $"Got {n}", err: e => e.Message);

        Console.WriteLine($"  GetNumberAsync(ok).Match(...): {output1}");
        Console.WriteLine($"  GetNumberAsync(fail).Match(...): {output2}");
        Console.WriteLine($"  GetNumberSafe(crash).Match(...): {output3}");
    }
}
