using System.Text.Json;

namespace ChatDistribuido.Nucleo;

/// <summary>Fragmento de estado local de um nó, parte de um snapshot global.</summary>
public sealed class FragmentoSnapshot
{
    public int NoId { get; set; }
    public int[]? Vc { get; set; }
    public long ProximoSeqEsperado { get; set; }
    public int TamanhoBuffer { get; set; }
    public int? LiderConhecido { get; set; }

    /// <summary>Mensagens capturadas em trânsito por canal de entrada (origem → mensagens).</summary>
    public Dictionary<int, List<string>> EstadoDosCanais { get; set; } = new();
}

/// <summary>
/// Registro em andamento de UM snapshot de Chandy-Lamport neste nó. Grava o estado local ao
/// receber o primeiro marcador e captura mensagens em trânsito por canal até o marcador
/// chegar por aquele canal. Produz um corte consistente sem parar o sistema.
/// </summary>
public sealed class ChandyLamportSnapshot
{
    private readonly HashSet<int> _canaisAguardandoMarcador;
    private readonly Dictionary<int, List<string>> _estadoDosCanais = new();

    public string SnapshotId { get; }
    public int IniciadorId { get; }
    public bool EstadoLocalGravado { get; private set; }
    public FragmentoSnapshot Fragmento { get; } = new();

    /// <param name="canaisEntrada">Ids dos nós dos quais recebemos mensagens (canais de entrada).</param>
    public ChandyLamportSnapshot(string snapshotId, int iniciadorId, int noId, IEnumerable<int> canaisEntrada)
    {
        SnapshotId = snapshotId;
        IniciadorId = iniciadorId;
        Fragmento.NoId = noId;
        _canaisAguardandoMarcador = new HashSet<int>(canaisEntrada);
    }

    /// <summary>Grava o estado local (uma única vez) no início do snapshot para este nó.</summary>
    public void GravarEstadoLocal(int[] vc, long proximoSeqEsperado, int tamanhoBuffer, int? liderConhecido)
    {
        if (EstadoLocalGravado) return;
        EstadoLocalGravado = true;
        Fragmento.Vc = vc;
        Fragmento.ProximoSeqEsperado = proximoSeqEsperado;
        Fragmento.TamanhoBuffer = tamanhoBuffer;
        Fragmento.LiderConhecido = liderConhecido;
    }

    /// <summary>
    /// Registra a chegada de um marcador pelo canal <paramref name="origem"/>: fecha a
    /// gravação daquele canal. Retorna true se o snapshot está concluído neste nó.
    /// </summary>
    public bool MarcadorRecebido(int origem)
    {
        _canaisAguardandoMarcador.Remove(origem);
        Fragmento.EstadoDosCanais[origem] =
            _estadoDosCanais.TryGetValue(origem, out var lista) ? lista : new List<string>();
        return _canaisAguardandoMarcador.Count == 0;
    }

    /// <summary>
    /// Registra uma mensagem de aplicação recebida pelo canal <paramref name="origem"/>
    /// enquanto esse canal ainda está sendo gravado (estado do canal).
    /// </summary>
    public void RegistrarMensagemEmTransito(int origem, string descricao)
    {
        if (!EstadoLocalGravado) return;
        if (!_canaisAguardandoMarcador.Contains(origem)) return;
        if (!_estadoDosCanais.TryGetValue(origem, out var lista))
            _estadoDosCanais[origem] = lista = new List<string>();
        lista.Add(descricao);
    }

    public string Serializar() => JsonSerializer.Serialize(Fragmento, JsonSerializerConfig());

    public static FragmentoSnapshot? Desserializar(string json) =>
        JsonSerializer.Deserialize<FragmentoSnapshot>(json, JsonSerializerConfig());

    private static JsonSerializerOptions JsonSerializerConfig() =>
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
