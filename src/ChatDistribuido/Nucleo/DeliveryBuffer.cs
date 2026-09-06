namespace ChatDistribuido.Nucleo;

/// <summary>Mensagem de grupo já sequenciada, aguardando entrega em ordem.</summary>
public sealed record MensagemSequenciada(long Seq, int DeOriginal, string Conteudo, int[]? Vc);

/// <summary>
/// Buffer de reordenação por número de sequência global. Garante entrega estrita e
/// monotônica (sem lacunas nem duplicatas) das mensagens de grupo.
/// </summary>
public sealed class DeliveryBuffer
{
    private readonly SortedDictionary<long, MensagemSequenciada> _pendentes = new();

    /// <summary>Próximo seq que pode ser entregue.</summary>
    public long ProximoSeqEsperado { get; private set; } = 1;

    /// <summary>Seqs atualmente retidos no buffer.</summary>
    public IReadOnlyCollection<long> SeqsPendentes => _pendentes.Keys;

    /// <summary>
    /// Insere uma mensagem sequenciada e retorna, em ordem, todas as que se tornaram
    /// entregáveis a partir de <see cref="ProximoSeqEsperado"/>. Duplicatas de seqs já
    /// entregues são descartadas.
    /// </summary>
    public IReadOnlyList<MensagemSequenciada> Receber(MensagemSequenciada msg)
    {
        var entregaveis = new List<MensagemSequenciada>();

        if (msg.Seq < ProximoSeqEsperado)
            return entregaveis; // duplicata / atrasada já entregue

        _pendentes[msg.Seq] = msg; // idempotente para seqs futuros repetidos

        while (_pendentes.TryGetValue(ProximoSeqEsperado, out var proxima))
        {
            entregaveis.Add(proxima);
            _pendentes.Remove(ProximoSeqEsperado);
            ProximoSeqEsperado++;
        }

        return entregaveis;
    }
}
