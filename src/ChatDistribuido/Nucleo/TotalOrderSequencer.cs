namespace ChatDistribuido.Nucleo;

/// <summary>
/// Sequenciador de ordem total (executa no líder). Atribui números de sequência globais
/// monotônicos às mensagens de grupo. Após reeleição, a numeração é retomada a partir do
/// maior seq já entregue conhecido + 1, evitando lacunas e duplicatas.
/// </summary>
public sealed class TotalOrderSequencer
{
    private long _proximoSeq = 1;

    /// <summary>Próximo seq que será atribuído.</summary>
    public long ProximoSeq => _proximoSeq;

    /// <summary>Atribui e consome o próximo número de sequência.</summary>
    public long Atribuir() => _proximoSeq++;

    /// <summary>
    /// Retoma a sequência após reeleição: o novo líder continua a partir do maior seq já
    /// entregue conhecido entre os nós ativos + 1.
    /// </summary>
    public void Retomar(long maiorSeqEntregue)
    {
        var candidato = maiorSeqEntregue + 1;
        if (candidato > _proximoSeq)
            _proximoSeq = candidato;
    }
}
