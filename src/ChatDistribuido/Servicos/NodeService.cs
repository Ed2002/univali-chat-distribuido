using System.Collections.Concurrent;
using ChatDistribuido.Nucleo;
using ChatDistribuido.Rede;

namespace ChatDistribuido.Servicos;

/// <summary>
/// Fachada do núcleo + ponte de eventos para a UI. Orquestra transporte e algoritmos
/// (ordem total, eleição, snapshot) e mantém o <see cref="NodeState"/> observável.
/// Todo o estado do núcleo é protegido por um lock; os envios de rede são despachados
/// FORA do lock para não bloquear o processamento (e nós indisponíveis não travam os demais).
/// </summary>
public sealed class NodeService : IDisposable
{
    private readonly int _id;
    private readonly CatalogoNos _catalogo;
    private readonly IMessageChannel _channel;
    private readonly VectorClock _vc;
    private readonly DeliveryBuffer _buffer = new();
    private readonly TotalOrderSequencer _sequencer = new();
    private readonly BullyElection _election;
    private readonly object _gate = new();

    // Snapshots em andamento (por snapshotId) e agregação no iniciador.
    private readonly Dictionary<string, ChandyLamportSnapshot> _snapshots = new();
    private readonly Dictionary<string, Dictionary<int, FragmentoSnapshot>> _agregados = new();

    private DateTime _ultimoHeartbeat = DateTime.UtcNow;
    private Timer? _timerHeartbeat;
    private Timer? _timerDeteccao;

    private static readonly TimeSpan IntervaloHeartbeat = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan TimeoutDeteccao = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan TimeoutEleicao = TimeSpan.FromMilliseconds(1500);

    public NodeState State { get; }

    /// <summary>Ids dos demais nós do catálogo (exceto o próprio), para a lista de conversas.</summary>
    public IReadOnlyList<int> OutrosNos => _catalogo.Ids.Where(x => x != _id).ToList();

    public NodeService(int id, CatalogoNos catalogo, IMessageChannel channel)
    {
        _id = id;
        _catalogo = catalogo;
        _channel = channel;
        _vc = new VectorClock(catalogo.Total, catalogo.Indice(id));
        _election = new BullyElection(id, catalogo.Ids);
        State = new NodeState(id, catalogo.Total);
        _channel.OnEnvelope += OnEnvelope;
    }

    public void Start()
    {
        _channel.Start();

        // Bootstrap: o maior id do catálogo assume como líder inicial.
        var maiorId = _catalogo.Ids.Max();
        lock (_gate)
        {
            if (_id == maiorId) _election.TornarSeLider();
            else _election.ReconhecerLider(maiorId);
            _ultimoHeartbeat = DateTime.UtcNow;
        }
        State.AtualizarLideranca(_election.LiderAtual, false);

        _timerHeartbeat = new Timer(_ => TickHeartbeat(), null, IntervaloHeartbeat, IntervaloHeartbeat);
        _timerDeteccao = new Timer(_ => TickDeteccao(), null, TimeoutDeteccao, IntervaloHeartbeat);
    }

    // ==================== Ações da UI ====================

    public void EnviarUnicast(int destId, string conteudo)
    {
        int[] vc;
        lock (_gate)
        {
            vc = _vc.Tick();
            State.RegistrarPrivada(_id, destId, conteudo, vc, enviada: true);
            State.RegistrarEmissaoLocal($"unicast → nó {destId}: {conteudo}", vc);
        }
        var env = new Envelope { Tipo = TipoMensagem.UNICAST, De = _id, Para = destId, Vc = vc, Conteudo = conteudo };
        _ = _channel.SendAsync(destId, env);
    }

    public void EnviarGrupo(string conteudo)
    {
        Envelope groupEnv;
        lock (_gate)
        {
            var vc = _vc.Tick();
            State.RegistrarEmissaoLocal($"grupo: {conteudo}", vc);
            groupEnv = new Envelope
            {
                Tipo = TipoMensagem.GROUP,
                De = _id,
                DeOriginal = _id,
                Vc = vc,
                Conteudo = conteudo,
                MsgId = Guid.NewGuid().ToString("N")
            };
        }
        RotearGroup(groupEnv);
    }

    public void DispararSnapshot()
    {
        var snapshotId = $"{_id}-{Guid.NewGuid():N}";
        var envios = new List<Envelope>();
        lock (_gate)
        {
            var snap = ObterOuCriarSnapshot(snapshotId, iniciadorId: _id);
            snap.GravarEstadoLocal(_vc.Snapshot(), _buffer.ProximoSeqEsperado,
                _buffer.SeqsPendentes.Count, _election.LiderAtual);
            envios.Add(new Envelope
            {
                Tipo = TipoMensagem.MARKER, De = _id,
                SnapshotId = snapshotId, IniciadorId = _id
            });
        }
        BroadcastPares(envios[0]);
    }

    // ==================== Roteamento de recepção ====================

    private void OnEnvelope(Envelope env)
    {
        try
        {
            switch (env.Tipo)
            {
                case TipoMensagem.UNICAST: TratarUnicast(env); break;
                case TipoMensagem.GROUP: RotearGroup(env); break;
                case TipoMensagem.SEQUENCED: TratarSequenced(env); break;
                case TipoMensagem.HEARTBEAT: TratarHeartbeat(env); break;
                case TipoMensagem.ELECTION: TratarElection(env); break;
                case TipoMensagem.OK: TratarOk(); break;
                case TipoMensagem.COORDINATOR: TratarCoordinator(env); break;
                case TipoMensagem.LAST_SEQ_QUERY: TratarLastSeqQuery(env); break;
                case TipoMensagem.LAST_SEQ_REPLY: TratarLastSeqReply(env); break;
                case TipoMensagem.MARKER: TratarMarker(env); break;
                case TipoMensagem.SNAPSHOT_STATE: TratarSnapshotState(env); break;
            }
        }
        catch
        {
            // Uma mensagem malformada de um par não deve derrubar o nó.
        }
    }

    // ---- US2: mensagem privada ----
    private void TratarUnicast(Envelope env)
    {
        lock (_gate)
        {
            var vc = _vc.Merge(env.Vc);
            State.RegistrarPrivada(env.De, _id, env.Conteudo ?? "", vc, enviada: false);
            RegistrarEmTransito(env.De, $"UNICAST de {env.De}: {env.Conteudo}");
        }
    }

    // ---- US1: ordem total ----
    private void RotearGroup(Envelope groupEnv)
    {
        bool souLider;
        int? lider;
        lock (_gate) { souLider = _election.SouLider; lider = _election.LiderAtual; }

        if (souLider) SequenciarEBroadcast(groupEnv);
        else if (lider is int l) _ = _channel.SendAsync(l, groupEnv);
    }

    private void SequenciarEBroadcast(Envelope groupEnv)
    {
        Envelope seqEnv;
        lock (_gate)
        {
            var seq = _sequencer.Atribuir();
            seqEnv = new Envelope
            {
                Tipo = TipoMensagem.SEQUENCED, De = _id, Seq = seq,
                Conteudo = groupEnv.Conteudo, MsgId = groupEnv.MsgId,
                DeOriginal = groupEnv.DeOriginal ?? groupEnv.De, Vc = groupEnv.Vc
            };
        }
        TratarSequenced(seqEnv);         // entrega local
        BroadcastPares(seqEnv);          // aos demais
    }

    private void TratarSequenced(Envelope env)
    {
        lock (_gate)
        {
            var msg = new MensagemSequenciada(env.Seq!.Value, env.DeOriginal ?? env.De, env.Conteudo ?? "", env.Vc);
            RegistrarEmTransito(env.De, $"SEQUENCED seq={env.Seq} de {msg.DeOriginal}");
            foreach (var m in _buffer.Receber(msg))
            {
                var vc = _vc.Merge(m.Vc);
                State.RegistrarEntregaGlobal(m.Seq, m.DeOriginal, m.Conteudo, vc);
            }
            State.AtualizarBuffer(_buffer.SeqsPendentes);
        }
    }

    // ---- US5: eleição (Bully) ----
    private void TratarHeartbeat(Envelope env)
    {
        lock (_gate)
        {
            _ultimoHeartbeat = DateTime.UtcNow;
            _election.ReconhecerLider(env.LiderId ?? env.De);
        }
        State.AtualizarLideranca(_election.LiderAtual, false);
    }

    private void TratarElection(Envelope env)
    {
        // Um id menor iniciou eleição: respondo OK e assumo a disputa (id maior vence).
        _ = _channel.SendAsync(env.De, new Envelope { Tipo = TipoMensagem.OK, De = _id });
        bool iniciar;
        lock (_gate) { iniciar = !_election.EmEleicao; }
        if (iniciar) IniciarEleicao();
    }

    private void TratarOk()
    {
        lock (_gate) { _election.MarcarOkRecebido(); }
    }

    private void TratarCoordinator(Envelope env)
    {
        lock (_gate)
        {
            _ultimoHeartbeat = DateTime.UtcNow;
            _election.ReconhecerLider(env.LiderId ?? env.De);
        }
        State.AtualizarLideranca(_election.LiderAtual, false);
    }

    private void TratarLastSeqQuery(Envelope env)
    {
        var ultimo = State.UltimoSeqEntregue();
        _ = _channel.SendAsync(env.De, new Envelope
        {
            Tipo = TipoMensagem.LAST_SEQ_REPLY, De = _id, UltimoSeqEntregue = ultimo
        });
    }

    private void TratarLastSeqReply(Envelope env)
    {
        lock (_gate)
        {
            if (_election.SouLider && env.UltimoSeqEntregue is long s)
                _sequencer.Retomar(s);
        }
    }

    private void TickHeartbeat()
    {
        bool souLider;
        lock (_gate) { souLider = _election.SouLider; }
        if (souLider)
            BroadcastPares(new Envelope { Tipo = TipoMensagem.HEARTBEAT, De = _id, LiderId = _id });
    }

    private void TickDeteccao()
    {
        bool iniciar;
        lock (_gate)
        {
            iniciar = !_election.SouLider && !_election.EmEleicao
                      && DateTime.UtcNow - _ultimoHeartbeat > TimeoutDeteccao;
        }
        if (iniciar) IniciarEleicao();
    }

    private void IniciarEleicao()
    {
        bool virarLiderJa;
        List<int> maiores;
        lock (_gate)
        {
            virarLiderJa = _election.IniciarEleicao();
            maiores = _election.IdsMaiores.ToList();
        }
        State.AtualizarLideranca(null, true);

        if (virarLiderJa) { TornarSeLider(); return; }

        var election = new Envelope { Tipo = TipoMensagem.ELECTION, De = _id };
        foreach (var maior in maiores) _ = _channel.SendAsync(maior, election);

        _ = Task.Delay(TimeoutEleicao).ContinueWith(_ =>
        {
            bool assumir;
            lock (_gate) { assumir = _election.EmEleicao && !_election.RecebeuOk; }
            if (assumir) TornarSeLider();
        });
    }

    private void TornarSeLider()
    {
        lock (_gate)
        {
            _election.TornarSeLider();
            _ultimoHeartbeat = DateTime.UtcNow;
            _sequencer.Retomar(State.UltimoSeqEntregue());
        }
        State.AtualizarLideranca(_id, false);
        BroadcastPares(new Envelope { Tipo = TipoMensagem.COORDINATOR, De = _id, LiderId = _id });
        // Retoma a numeração sem lacunas: consulta o último seq entregue dos ativos.
        BroadcastPares(new Envelope { Tipo = TipoMensagem.LAST_SEQ_QUERY, De = _id });
    }

    // ---- US4: snapshot (Chandy-Lamport) ----
    private void TratarMarker(Envelope env)
    {
        var snapshotId = env.SnapshotId!;
        var iniciador = env.IniciadorId ?? env.De;
        var origem = env.De;
        bool concluido;
        Envelope? propagacao = null;

        lock (_gate)
        {
            var novo = !_snapshots.ContainsKey(snapshotId);
            var snap = ObterOuCriarSnapshot(snapshotId, iniciador);
            if (novo)
            {
                snap.GravarEstadoLocal(_vc.Snapshot(), _buffer.ProximoSeqEsperado,
                    _buffer.SeqsPendentes.Count, _election.LiderAtual);
                propagacao = new Envelope
                {
                    Tipo = TipoMensagem.MARKER, De = _id,
                    SnapshotId = snapshotId, IniciadorId = iniciador
                };
            }
            concluido = snap.MarcadorRecebido(origem);
        }

        if (propagacao is not null) BroadcastPares(propagacao);
        if (concluido) FinalizarSnapshotLocal(snapshotId, iniciador);
    }

    private void FinalizarSnapshotLocal(string snapshotId, int iniciador)
    {
        FragmentoSnapshot fragmento;
        string json;
        lock (_gate)
        {
            var snap = _snapshots[snapshotId];
            fragmento = snap.Fragmento;
            json = snap.Serializar();
        }

        if (iniciador == _id)
        {
            AgregarFragmento(snapshotId, fragmento);
        }
        else
        {
            _ = _channel.SendAsync(iniciador, new Envelope
            {
                Tipo = TipoMensagem.SNAPSHOT_STATE, De = _id,
                SnapshotId = snapshotId, IniciadorId = iniciador, SnapshotPayload = json
            });
        }
    }

    private void TratarSnapshotState(Envelope env)
    {
        var frag = ChandyLamportSnapshot.Desserializar(env.SnapshotPayload ?? "{}");
        if (frag is not null) AgregarFragmento(env.SnapshotId!, frag);
    }

    private void AgregarFragmento(string snapshotId, FragmentoSnapshot frag)
    {
        bool completo;
        Dictionary<int, FragmentoSnapshot> mapa;
        lock (_gate)
        {
            if (!_agregados.TryGetValue(snapshotId, out mapa!))
                _agregados[snapshotId] = mapa = new Dictionary<int, FragmentoSnapshot>();
            mapa[frag.NoId] = frag;
            completo = mapa.Count == _catalogo.Total;
        }
        if (completo) State.DefinirSnapshot(FormatarSnapshot(snapshotId, mapa));
    }

    private string FormatarSnapshot(string snapshotId, Dictionary<int, FragmentoSnapshot> mapa)
    {
        var linhas = new List<string> { $"Snapshot {snapshotId} (corte consistente):" };
        foreach (var id in mapa.Keys.OrderBy(x => x))
        {
            var f = mapa[id];
            var vc = f.Vc is null ? "[]" : "[" + string.Join(",", f.Vc) + "]";
            var canais = f.EstadoDosCanais.Sum(c => c.Value.Count);
            linhas.Add($"  nó {id}: VC={vc}, próximoSeq={f.ProximoSeqEsperado}, " +
                       $"buffer={f.TamanhoBuffer}, líder={f.LiderConhecido}, emTrânsito={canais}");
        }
        return string.Join("\n", linhas);
    }

    private ChandyLamportSnapshot ObterOuCriarSnapshot(string snapshotId, int iniciadorId)
    {
        if (!_snapshots.TryGetValue(snapshotId, out var snap))
            _snapshots[snapshotId] = snap = new ChandyLamportSnapshot(snapshotId, iniciadorId, _id, _channel.Pares);
        return snap;
    }

    private void RegistrarEmTransito(int origem, string descricao)
    {
        foreach (var snap in _snapshots.Values)
            snap.RegistrarMensagemEmTransito(origem, descricao);
    }

    private void BroadcastPares(Envelope env)
    {
        foreach (var peer in _channel.Pares) _ = _channel.SendAsync(peer, env);
    }

    public void Dispose()
    {
        _timerHeartbeat?.Dispose();
        _timerDeteccao?.Dispose();
        (_channel as IDisposable)?.Dispose();
    }
}
