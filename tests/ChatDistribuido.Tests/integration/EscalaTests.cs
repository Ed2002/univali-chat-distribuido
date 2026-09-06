using Xunit;

namespace ChatDistribuido.Tests.Integration;

public class EscalaTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(15)]
    public async Task Ordem_total_identica_em_escala(int n)
    {
        using var cluster = new Cluster(n);
        var maiorId = n;

        await Cluster.Esperar(
            () => cluster.Nos.Values.All(x => x.State.LiderAtual == maiorId),
            timeoutMs: 10000);

        // Cada nó difunde uma mensagem.
        foreach (var id in cluster.Catalogo.Ids)
            cluster.Nos[id].EnviarGrupo($"m{id}");

        var ok = await Cluster.Esperar(
            () => cluster.Nos.Values.All(x => x.State.OrdemGlobal().Count == n),
            timeoutMs: 15000);
        Assert.True(ok, $"nem todos os {n} nós entregaram as {n} mensagens");

        // Filas idênticas por seq em todos os nós.
        var referencia = cluster.Nos[1].State.OrdemGlobal()
            .OrderBy(g => g.Seq).Select(g => (g.Seq, g.Conteudo)).ToList();
        foreach (var x in cluster.Nos.Values)
        {
            var fila = x.State.OrdemGlobal().OrderBy(g => g.Seq).Select(g => (g.Seq, g.Conteudo)).ToList();
            Assert.Equal(referencia, fila);
        }

        // Sem lacunas.
        Assert.Equal(Enumerable.Range(1, n).Select(i => (long)i), referencia.Select(r => r.Seq));
    }
}
