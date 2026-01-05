using System.Text.Json;
using RopStarterPack;

namespace RopStarterPack.Tests;

public class ResultJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ResultJsonConverterFactory() },
    };

    [Fact]
    public void Ok_Bool_SerializesAndDeserializes()
    {
        var original = Result.Ok<bool, string>(true);

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<bool, string>>(json, _options);

        Assert.Equal("""{"$result":"ok","value":true}""", json);
        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void Err_String_SerializesAndDeserializes()
    {
        var original = Result.Err<bool, string>("something went wrong");

        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Result<bool, string>>(json, _options);

        Assert.Equal("""{"$result":"err","error":"something went wrong"}""", json);
        Assert.Equal(original, deserialized);
    }

    [Fact]
    public void Ok_SerializesCorrectly_WhenUsingRuntimeType()
    {
        object original = Result.Ok<string, string>("hello");

        var json = JsonSerializer.Serialize(original, original.GetType(), _options);
        var deserialized = JsonSerializer.Deserialize<Result<string, string>>(json, _options);

        Assert.StartsWith("""{"$result":"ok",""", json);
        Assert.IsType<Result<string, string>.Ok>(deserialized);
    }

    [Fact]
    public void Err_SerializesCorrectly_WhenUsingRuntimeType()
    {
        object original = Result.Err<string, string>("error");

        var json = JsonSerializer.Serialize(original, original.GetType(), _options);
        var deserialized = JsonSerializer.Deserialize<Result<string, string>>(json, _options);

        Assert.StartsWith("""{"$result":"err",""", json);
        Assert.IsType<Result<string, string>.Err>(deserialized);
    }
}
