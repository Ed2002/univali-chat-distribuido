namespace ChatDistribuido.Servicos;

public sealed record EntregaGlobal(long Seq, int DeOriginal, string Conteudo, string Vc);
public sealed record EmissaoLocal(int Ordem, string Descricao, string Vc);
public sealed record RegistroPrivado(int De, int Para, string Conteudo, string Vc, bool Enviada);

/// <summary>
/// Estado observável do nó, consumido pelo painel local. Reflete APENAS o próprio nó —
/// nenhum estado cruza entre nós fora do canal de rede (Constituição, Princípio V).
/// Snapshots são retornados como cópias imutáveis para leitura segura pela UI.
/// </summary>
public sealed class NodeState
{
    private readonly object _gate = new();

    private readonly List<EmissaoLocal> _ordemLocal = new();
    private readonly List<EntregaGlobal> _ordemGlobal = new();
    private readonly List<RegistroPrivado> _privadas = new();
    private int _contadorEmissao;

    public int Id { get; }
    public int? LiderAtual { get; private set; }
    public bool EmEleicao { get; private set; }
    public int[] Vc { get; private set; }
    public IReadOnlyList<long> BufferSeqs { get; private set; } = Array.Empty<long>();
    public string? UltimoSnapshot { get; private set; }

    /// <summary>Disparado a cada mudança de estado, para a UI reagir em tempo real.</summary>
    public event Action? Changed;

    public NodeState(int id, int n)
    {
        Id = id;
        Vc = new int[n];
    }

    public void RegistrarEmissaoLocal(string descricao, int[] vc)
    {
        lock (_gate)
        {
            _contadorEmissao++;
            _ordemLocal.Add(new EmissaoLocal(_contadorEmissao, descricao, Fmt(vc)));
            Vc = vc;
        }
        Changed?.Invoke();
    }

    public void RegistrarEntregaGlobal(long seq, int deOriginal, string conteudo, int[] vc)
    {
        lock (_gate)
        {
            _ordemGlobal.Add(new EntregaGlobal(seq, deOriginal, conteudo, Fmt(vc)));
            Vc = vc;
        }
        Changed?.Invoke();
    }

    public void RegistrarPrivada(int de, int para, string conteudo, int[] vc, bool enviada)
    {
        lock (_gate)
        {
            _privadas.Add(new RegistroPrivado(de, para, conteudo, Fmt(vc), enviada));
            Vc = vc;
        }
        Changed?.Invoke();
    }

    public void AtualizarBuffer(IEnumerable<long> seqs)
    {
        lock (_gate) { BufferSeqs = seqs.OrderBy(x => x).ToList(); }
        Changed?.Invoke();
    }

    public void AtualizarLideranca(int? lider, bool emEleicao)
    {
        lock (_gate) { LiderAtual = lider; EmEleicao = emEleicao; }
        Changed?.Invoke();
    }

    public void DefinirSnapshot(string texto)
    {
        lock (_gate) { UltimoSnapshot = texto; }
        Changed?.Invoke();
    }

    public IReadOnlyList<EmissaoLocal> OrdemLocal()
    {
        lock (_gate) return _ordemLocal.ToList();
    }

    public IReadOnlyList<EntregaGlobal> OrdemGlobal()
    {
        lock (_gate) return _ordemGlobal.ToList();
    }

    public IReadOnlyList<RegistroPrivado> Privadas()
    {
        lock (_gate) return _privadas.ToList();
    }

    /// <summary>
    /// Mensagens privadas trocadas com um nó específico (par), na ordem de registro.
    /// O par de cada registro é o destinatário se enviada, ou o remetente se recebida.
    /// </summary>
    public IReadOnlyList<RegistroPrivado> PrivadasCom(int peer)
    {
        lock (_gate)
            return _privadas.Where(p => (p.Enviada ? p.Para : p.De) == peer).ToList();
    }

    /// <summary>Último seq entregue na ordem global (0 se nada foi entregue).</summary>
    public long UltimoSeqEntregue()
    {
        lock (_gate) return _ordemGlobal.Count == 0 ? 0 : _ordemGlobal[^1].Seq;
    }

    private static string Fmt(int[] vc) => "[" + string.Join(",", vc) + "]";
}
