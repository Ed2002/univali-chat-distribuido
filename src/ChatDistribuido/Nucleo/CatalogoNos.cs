using System.Text.Json;

namespace ChatDistribuido.Nucleo;

/// <summary>Entrada estática do catálogo de nós (lida na inicialização).</summary>
public sealed record NoInfo(int Id, string Host, int PortaTcp);

/// <summary>
/// Catálogo estático de nós. É apenas uma lista de endereços lida na inicialização —
/// NÃO é canal de coordenação em tempo de execução (Constituição, Regra Inviolável).
/// </summary>
public sealed class CatalogoNos
{
    private readonly Dictionary<int, NoInfo> _porId;

    public CatalogoNos(IEnumerable<NoInfo> nos)
    {
        _porId = nos.ToDictionary(n => n.Id);
        Ids = _porId.Keys.OrderBy(x => x).ToList();
    }

    /// <summary>Ids de todos os nós, em ordem crescente.</summary>
    public IReadOnlyList<int> Ids { get; }

    public int Total => Ids.Count;

    public NoInfo this[int id] => _porId[id];

    public bool Contem(int id) => _porId.ContainsKey(id);

    /// <summary>Índice posicional do id no vetor de relógio vetorial (0..N-1).</summary>
    public int Indice(int id) => Ids.ToList().IndexOf(id);

    /// <summary>Carrega o catálogo de um arquivo JSON (formato: lista de {id,host,portaTcp}).</summary>
    public static CatalogoNos Carregar(string caminho)
    {
        var json = File.ReadAllText(caminho);
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var nos = JsonSerializer.Deserialize<List<NoInfo>>(json, opts)
                  ?? throw new InvalidDataException($"Catálogo vazio ou inválido: {caminho}");
        if (nos.Count == 0)
            throw new InvalidDataException($"Catálogo sem nós: {caminho}");
        return new CatalogoNos(nos);
    }
}
