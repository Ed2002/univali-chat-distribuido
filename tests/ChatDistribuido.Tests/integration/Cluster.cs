using System.Diagnostics;
using ChatDistribuido.Nucleo;
using ChatDistribuido.Rede;
using ChatDistribuido.Servicos;

namespace ChatDistribuido.Tests.Integration;

/// <summary>
/// Harness de teste: sobe N nós reais em 127.0.0.1 (transporte TCP), cada um em processo
/// lógico independente dentro do teste, com faixa de portas isolada por instância.
/// </summary>
public sealed class Cluster : IDisposable
{
    private static int _offset = 0;
    private readonly List<TcpTransport> _transportes = new();

    public IReadOnlyDictionary<int, NodeService> Nos { get; }
    public CatalogoNos Catalogo { get; }

    public Cluster(int n)
    {
        // Faixa de portas única por cluster para evitar conflitos entre testes.
        var basePorta = 20000 + Interlocked.Add(ref _offset, 100);
        var nos = Enumerable.Range(1, n)
            .Select(id => new NoInfo(id, "127.0.0.1", basePorta + id))
            .ToList();
        Catalogo = new CatalogoNos(nos);

        var mapa = new Dictionary<int, NodeService>();
        foreach (var id in Catalogo.Ids)
        {
            var t = new TcpTransport(id, Catalogo);
            _transportes.Add(t);
            var svc = new NodeService(id, Catalogo, t);
            mapa[id] = svc;
        }
        Nos = mapa;

        foreach (var svc in Nos.Values) svc.Start();
    }

    public NodeService Lider => Nos[Catalogo.Ids.Max()];

    /// <summary>Espera até a condição ficar verdadeira ou estourar o timeout.</summary>
    public static async Task<bool> Esperar(Func<bool> cond, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            if (cond()) return true;
            await Task.Delay(50);
        }
        return cond();
    }

    public void Dispose()
    {
        foreach (var svc in Nos.Values) svc.Dispose();
    }
}
