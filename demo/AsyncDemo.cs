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

        // Fake async operation that THROWS (simulates crash)
        async Task<Result<int, DemoError>> GetNumberAsyncThrows()
        {
            await Task.Delay(10);
            throw new Exception("Connection timeout!");
        }

        // Async Map (sync f)
        Console.WriteLine("\nAsync Map (sync f) demo:");

        var result12 = await GetNumberAsync(false).Map(n => n * 2);
        var result13 = await GetNumberAsync(true).Map(n => n * 2);
        var result14 = await GetNumberAsyncThrows().Map(n => n * 2);

        Console.WriteLine(
            $"  GetNumberAsync(ok).Map(n => n * 2): {result12.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).Map(n => n * 2): {result13.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsyncThrows().Map(...): {result14.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );

        // Async Map (async f)
        Console.WriteLine("\nAsync Map (async f) demo:");

        async Task<string> FormatAsync(int n)
        {
            await Task.Delay(10);
            return $"Formatted: {n}";
        }

        async Task<string> FormatAsyncThrows(int n)
        {
            await Task.Delay(10);
            throw new Exception("Formatter service down!");
        }

        var result15 = await GetNumberAsync(false).Map(async n => await FormatAsync(n));
        var result16 = await GetNumberAsync(true).Map(async n => await FormatAsync(n));
        var result17 = await GetNumberAsync(false).Map(async n => await FormatAsyncThrows(n));
        var result18 = await GetNumberAsyncThrows().Map(async n => await FormatAsync(n));

        Console.WriteLine(
            $"  GetNumberAsync(ok).Map(FormatAsync): {result15.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).Map(FormatAsync): {result16.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(ok).Map(FormatAsyncThrows): {result17.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsyncThrows().Map(FormatAsync): {result18.Match(ok: s => s, err: e => e.Message)}"
        );

        // Async AndThen (sync f) - chains a sync function that returns Result
        Console.WriteLine("\nAsync AndThen (sync f) demo:");

        Result<string, DemoError> ValidateSync(int n, bool shouldFail) =>
            shouldFail
                ? Result.Err<string, DemoError>(new DemoError("Number too small"))
                : Result.Ok<string, DemoError>($"Valid: {n}");

        var resultSync1 = await GetNumberAsync(false).AndThen(n => ValidateSync(n, false));
        var resultSync2 = await GetNumberAsync(false).AndThen(n => ValidateSync(n, true));
        var resultSync3 = await GetNumberAsync(true).AndThen(n => ValidateSync(n, false));
        var resultSync4 = await GetNumberAsyncThrows().AndThen(n => ValidateSync(n, false));

        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateSync ok): {resultSync1.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateSync fail): {resultSync2.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).AndThen(ValidateSync ok): {resultSync3.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsyncThrows().AndThen(ValidateSync ok): {resultSync4.Match(ok: s => s, err: e => e.Message)}"
        );

        // Async AndThen (async f) - chains an async function that returns Task<Result>
        Console.WriteLine("\nAsync AndThen (async f) demo:");

        async Task<Result<string, DemoError>> ValidateAsync(
            int n,
            bool shouldFail,
            bool shouldThrow
        )
        {
            await Task.Delay(10);
            if (shouldThrow)
                throw new Exception("Validation service crashed!");
            if (shouldFail)
                return Result.Err<string, DemoError>(new DemoError("Number too small"));
            return Result.Ok<string, DemoError>($"Valid: {n}");
        }

        var result19 = await GetNumberAsync(false).AndThen(n => ValidateAsync(n, false, false));
        var result20 = await GetNumberAsync(false).AndThen(n => ValidateAsync(n, true, false));
        var result21 = await GetNumberAsync(true).AndThen(n => ValidateAsync(n, false, false));
        var result22 = await GetNumberAsync(false).AndThen(n => ValidateAsync(n, false, true));
        var result23 = await GetNumberAsyncThrows().AndThen(n => ValidateAsync(n, false, false));

        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateAsync ok): {result19.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateAsync fail): {result20.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(fail).AndThen(ValidateAsync ok): {result21.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsync(ok).AndThen(ValidateAsync throws): {result22.Match(ok: s => s, err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumberAsyncThrows().AndThen(ValidateAsync ok): {result23.Match(ok: s => s, err: e => e.Message)}"
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

        // Result.From with typed errors + crash handling
        Console.WriteLine("\nResult.From with typed errors + crash handling:");

        async Task<int?> FakeDbQuery(string mode)
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

        async Task<Result<int, DemoError>> FakeDbQueryWithResult(string mode)
        {
            var value = await FakeDbQuery(mode);
            return Result.FromNullable(value, new DemoError("Number not found"));
        }

        Task<Result<int, DemoError>> GetNumber(string mode) =>
            Result.From(
                () => FakeDbQueryWithResult(mode),
                ex => new DemoError($"DB crashed: {ex.Message}")
            );

        var result35 = await GetNumber("ok");
        var result36 = await GetNumber("notfound");
        var result37 = await GetNumber("crash");

        Console.WriteLine(
            $"  GetNumber(ok): {result35.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumber(notfound): {result36.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );
        Console.WriteLine(
            $"  GetNumber(crash): {result37.Match(ok: n => $"Got {n}", err: e => e.Message)}"
        );

        // Async Match
        Console.WriteLine("\nAsync Match demo:");

        var output1 = await GetNumberAsync(false).Match(ok: n => $"Got {n}", err: e => e.Message);
        var output2 = await GetNumberAsync(true).Match(ok: n => $"Got {n}", err: e => e.Message);
        var output3 = await GetNumberAsyncThrows().Match(ok: n => $"Got {n}", err: e => e.Message);

        Console.WriteLine($"  GetNumberAsync(ok).Match(...): {output1}");
        Console.WriteLine($"  GetNumberAsync(fail).Match(...): {output2}");
        Console.WriteLine($"  GetNumberAsyncThrows().Match(...): {output3}");
    }
}
