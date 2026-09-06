namespace ChatDistribuido.Nucleo;

/// <summary>
/// Relógio vetorial mantido por nó, para capturar causalidade entre eventos.
/// Roda em paralelo à ordem total (que é dada pelo sequenciador).
/// </summary>
public sealed class VectorClock
{
    private readonly int[] _vc;
    private readonly int _indiceProprio;

    /// <param name="n">Número de nós (tamanho do vetor).</param>
    /// <param name="indiceProprio">Índice posicional deste nó (0..n-1).</param>
    public VectorClock(int n, int indiceProprio)
    {
        _vc = new int[n];
        _indiceProprio = indiceProprio;
    }

    /// <summary>Evento local (ex.: emissão): incrementa o próprio componente.</summary>
    public int[] Tick()
    {
        _vc[_indiceProprio]++;
        return Snapshot();
    }

    /// <summary>
    /// Recepção/entrega: faz merge (máximo componente a componente) e incrementa o próprio.
    /// </summary>
    public int[] Merge(int[]? outro)
    {
        if (outro is not null)
        {
            var n = Math.Min(_vc.Length, outro.Length);
            for (var i = 0; i < n; i++)
                _vc[i] = Math.Max(_vc[i], outro[i]);
        }
        _vc[_indiceProprio]++;
        return Snapshot();
    }

    public int[] Snapshot() => (int[])_vc.Clone();

    public override string ToString() => "[" + string.Join(", ", _vc) + "]";
}
