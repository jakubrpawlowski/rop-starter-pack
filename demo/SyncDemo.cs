using RopStarterPack;

namespace Demo;

public static class SyncDemo
{
    public static void Run()
    {
        Console.WriteLine("=== Result Type Demo ===\n");

        // Simulate operations that might fail
        Result<int, string> ParseNumber(string input) =>
            int.TryParse(input, out var n)
                ? Result.Ok<int, string>(n)
                : Result.Err<int, string>($"'{input}' is not a valid number");

        // Two results - we don't know what's inside until we Match
        var result1 = ParseNumber("42");
        var result2 = ParseNumber("abc");

        // Match: forces you to handle both cases
        Console.WriteLine("Match demo:");
        Console.WriteLine(
            $"  ParseNumber(\"42\"): {result1.Match(ok: n => $"Got {n}", err: e => e)}"
        );
        Console.WriteLine(
            $"  ParseNumber(\"abc\"): {result2.Match(ok: n => $"Got {n}", err: e => e)}"
        );

        // Map: transform the value inside (if Ok)
        Console.WriteLine("\nMap demo:");
        var result3 = result1.Map(n => n * 2);
        var result4 = result2.Map(n => n * 2);

        Console.WriteLine(
            $"  result1.Map(n => n * 2): {result3.Match(ok: n => $"Got {n}", err: e => e)}"
        );
        Console.WriteLine(
            $"  result2.Map(n => n * 2): {result4.Match(ok: n => $"Got {n}", err: e => e)}"
        );

        // AndThen: chain operations that can fail
        Console.WriteLine("\nAndThen demo:");

        Result<int, string> Divide(int a, int b) =>
            b == 0 ? Result.Err<int, string>("division by zero") : Result.Ok<int, string>(a / b);

        var result5 = ParseNumber("50").AndThen(n => Divide(n, 2));
        var result6 = ParseNumber("50").AndThen(n => Divide(n, 0));
        var result7 = ParseNumber("abc").AndThen(n => Divide(n, 2));

        Console.WriteLine(
            $"  ParseNumber(\"50\").AndThen(n => Divide(n, 2)): {result5.Match(ok: n => $"Got {n}", err: e => e)}"
        );
        Console.WriteLine(
            $"  ParseNumber(\"50\").AndThen(n => Divide(n, 0)): {result6.Match(ok: n => $"Got {n}", err: e => e)}"
        );
        Console.WriteLine(
            $"  ParseNumber(\"abc\").AndThen(n => Divide(n, 2)): {result7.Match(ok: n => $"Got {n}", err: e => e)}"
        );

        // Result.From: convert throwing operations to Result
        Console.WriteLine("\nResult.From demo:");

        var result8 = Result.From(() => int.Parse("42"), ex => ex.Message);
        var result9 = Result.From(() => int.Parse("abc"), ex => ex.Message);

        Console.WriteLine(
            $"  Result.From(() => int.Parse(\"42\"), ...): {result8.Match(ok: n => $"Got {n}", err: e => e)}"
        );
        Console.WriteLine(
            $"  Result.From(() => int.Parse(\"abc\"), ...): {result9.Match(ok: n => $"Got {n}", err: e => e)}"
        );

        // Result.FromNullable: convert nullable to Result
        Console.WriteLine("\nResult.FromNullable demo:");

        // Value type (int?)
        int? maybeNumber1 = 42;
        int? maybeNumber2 = null;
        var result10 = Result.FromNullable(maybeNumber1, "number was null");
        var result11 = Result.FromNullable(maybeNumber2, "number was null");

        Console.WriteLine(
            $"  FromNullable(42, ...): {result10.Match(ok: n => $"Got {n}", err: e => e)}"
        );
        Console.WriteLine(
            $"  FromNullable(null, ...): {result11.Match(ok: n => $"Got {n}", err: e => e)}"
        );

        // Reference type (string?)
        string? maybeName1 = "Alice";
        string? maybeName2 = null;
        var result12 = Result.FromNullable(maybeName1, "name was null");
        var result13 = Result.FromNullable(maybeName2, "name was null");

        Console.WriteLine(
            $"  FromNullable(\"Alice\", ...): {result12.Match(ok: s => $"Got {s}", err: e => e)}"
        );
        Console.WriteLine(
            $"  FromNullable(null, ...): {result13.Match(ok: s => $"Got {s}", err: e => e)}"
        );
    }
}
