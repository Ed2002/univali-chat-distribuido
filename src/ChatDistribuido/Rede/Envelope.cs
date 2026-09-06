namespace ChatDistribuido.Rede;

/// <summary>
/// Tipos de mensagem do protocolo de rede (ver contracts/wire-protocol.md).
/// </summary>
public enum TipoMensagem
{
    UNICAST,
    GROUP,
    SEQUENCED,
    HEARTBEAT,
    ELECTION,
    OK,
    COORDINATOR,
    LAST_SEQ_QUERY,
    LAST_SEQ_REPLY,
    MARKER,
    SNAPSHOT_STATE
}

/// <summary>
/// Envelope único trafegado entre nós. É o ÚNICO contrato de coordenação (Constituição I).
/// Os campos de <c>payload</c> do contrato são achatados aqui como campos opcionais para
/// serialização direta e type-safe com System.Text.Json.
/// </summary>
public sealed class Envelope
{
    public TipoMensagem Tipo { get; set; }

    /// <summary>Id do nó remetente imediato.</summary>
    public int De { get; set; }

    /// <summary>Id do destinatário (unicast); ignorado em redistribuições.</summary>
    public int Para { get; set; }

    /// <summary>Relógio vetorial do remetente no envio.</summary>
    public int[]? Vc { get; set; }

    // ---- payload ----
    public string? Conteudo { get; set; }
    public string? MsgId { get; set; }
    public long? Seq { get; set; }
    public int? DeOriginal { get; set; }
    public long? UltimoSeqEntregue { get; set; }
    public int? LiderId { get; set; }
    public string? SnapshotId { get; set; }
    public int? IniciadorId { get; set; }

    /// <summary>Fragmento de estado local de um snapshot (serializado em JSON).</summary>
    public string? SnapshotPayload { get; set; }
}
