namespace ChatDistribuido.Nucleo;

/// <summary>
/// Estado e regras de decisão do Algoritmo do Valentão (Bully). O nó de MAIOR id ativo
/// vence. A parte de temporização (heartbeat/timeout) e o envio das mensagens ficam no
/// serviço; aqui concentram-se as decisões.
/// </summary>
public sealed class BullyElection
{
    private readonly int _id;
    private readonly IReadOnlyList<int> _todosIds;

    public BullyElection(int id, IReadOnlyList<int> todosIds)
    {
        _id = id;
        _todosIds = todosIds;
    }

    /// <summary>True enquanto uma eleição iniciada por este nó está em curso.</summary>
    public bool EmEleicao { get; private set; }

    /// <summary>Marca se algum id maior respondeu OK durante a eleição corrente.</summary>
    public bool RecebeuOk { get; private set; }

    public int? LiderAtual { get; private set; }

    /// <summary>Ids estritamente maiores que o próprio (destinatários de ELECTION).</summary>
    public IReadOnlyList<int> IdsMaiores => _todosIds.Where(x => x > _id).ToList();

    /// <summary>Este é o maior id do catálogo?</summary>
    public bool SouMaiorId => _todosIds.All(x => x <= _id);

    /// <summary>Inicia uma eleição; retorna true se deve virar líder imediatamente.</summary>
    public bool IniciarEleicao()
    {
        EmEleicao = true;
        RecebeuOk = false;
        LiderAtual = null;
        return IdsMaiores.Count == 0; // ninguém maior → líder direto
    }

    public void MarcarOkRecebido() => RecebeuOk = true;

    /// <summary>Encerra a eleição elegendo-se líder.</summary>
    public void TornarSeLider()
    {
        EmEleicao = false;
        RecebeuOk = false;
        LiderAtual = _id;
    }

    /// <summary>Reconhece outro nó como líder (recebeu COORDINATOR).</summary>
    public void ReconhecerLider(int liderId)
    {
        EmEleicao = false;
        RecebeuOk = false;
        LiderAtual = liderId;
    }

    public bool SouLider => LiderAtual == _id;
}
