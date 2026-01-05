using System.Text.Json;
using System.Text.Json.Serialization;

namespace RopStarterPack;

/// <summary>
/// JSON converter for Result&lt;T, E&gt;. Serializes as {"$result": "ok", "value": ...} or {"$result": "err", "error": ...}.
/// </summary>
public class ResultJsonConverter<T, E> : JsonConverter<Result<T, E>>
{
    private const string TypeProperty = "$result";
    private const string OkType = "ok";
    private const string ErrType = "err";
    private const string ValueProperty = "value";
    private const string ErrorProperty = "error";

    public override Result<T, E> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected StartObject");

        string? resultType = null;
        T? value = default;
        E? error = default;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName");

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case TypeProperty:
                    resultType = reader.GetString();
                    break;
                case ValueProperty:
                    value = JsonSerializer.Deserialize<T>(ref reader, options);
                    break;
                case ErrorProperty:
                    error = JsonSerializer.Deserialize<E>(ref reader, options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return resultType switch
        {
            OkType => new Result<T, E>.Ok(value!),
            ErrType => new Result<T, E>.Err(error!),
            null => throw new JsonException(
                $"Missing '$result' property. Expected JSON format: {{\"$result\":\"ok\",\"value\":...}} or {{\"$result\":\"err\",\"error\":...}}"
            ),
            _ => throw new JsonException(
                $"Unknown result type: '{resultType}'. Expected 'ok' or 'err'."
            ),
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        Result<T, E> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        switch (value)
        {
            case Result<T, E>.Ok(var v):
                writer.WriteString(TypeProperty, OkType);
                writer.WritePropertyName(ValueProperty);
                JsonSerializer.Serialize(writer, v, options);
                break;

            case Result<T, E>.Err(var e):
                writer.WriteString(TypeProperty, ErrType);
                writer.WritePropertyName(ErrorProperty);
                JsonSerializer.Serialize(writer, e, options);
                break;
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// Factory that creates ResultJsonConverter for any Result&lt;T, E&gt; type.
/// Register with: options.Converters.Add(new ResultJsonConverterFactory());
/// </summary>
public class ResultJsonConverterFactory : JsonConverterFactory
{
    private static Type? GetResultType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Result<,>))
            return type;

        if (
            type.BaseType is { IsGenericType: true } baseType
            && baseType.GetGenericTypeDefinition() == typeof(Result<,>)
        )
            return baseType;

        return null;
    }

    public override bool CanConvert(Type typeToConvert) => GetResultType(typeToConvert) is not null;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var resultType =
            GetResultType(typeToConvert)
            ?? throw new InvalidOperationException($"Cannot create converter for {typeToConvert}");

        var typeArgs = resultType.GetGenericArguments();
        var converterType = typeof(ResultJsonConverter<,>).MakeGenericType(typeArgs);
        return Activator.CreateInstance(converterType) as JsonConverter
            ?? throw new InvalidOperationException(
                $"Failed to create ResultJsonConverter for {typeToConvert}"
            );
    }
}
