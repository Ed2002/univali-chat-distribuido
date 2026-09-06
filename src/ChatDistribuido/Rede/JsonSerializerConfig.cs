using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChatDistribuido.Rede;

/// <summary>Opções de serialização JSON compartilhadas para os envelopes.</summary>
public static class JsonSerializerConfig
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };
}
