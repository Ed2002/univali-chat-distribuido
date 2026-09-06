namespace ChatDistribuido.Rede;

/// <summary>
/// Canal de mensagens entre nós. Abstrai o transporte para o núcleo/serviços.
/// </summary>
public interface IMessageChannel
{
    /// <summary>Disparado a cada envelope recebido (em thread de rede).</summary>
    event Action<Envelope>? OnEnvelope;

    /// <summary>Inicia o listener de entrada.</summary>
    void Start();

    /// <summary>
    /// Envia um envelope ao nó destino (unicast). Retorna false se o destino está
    /// indisponível — a falha NÃO deve bloquear o envio aos demais (FR-016).
    /// </summary>
    Task<bool> SendAsync(int destId, Envelope env);

    /// <summary>Ids de todos os nós do catálogo, exceto o próprio.</summary>
    IReadOnlyList<int> Pares { get; }
}
