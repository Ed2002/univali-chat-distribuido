using Xunit;

namespace ChatDistribuido.Tests.Integration;

public class ReeleicaoTests
{
    [Fact]
    public async Task Queda_do_lider_dispara_reeleicao_e_seq_continua_sem_lacunas()
    {
        using var cluster = new Cluster(3);
        await Cluster.Esperar(() => cluster.Nos.Values.All(n => n.State.LiderAtual == 3));

        // Tráfego inicial sob o líder 3.
        cluster.Nos[1].EnviarGrupo("a");
        cluster.Nos[2].EnviarGrupo("b");
        await Cluster.Esperar(() =>
            cluster.Nos[1].State.OrdemGlobal().Count == 2 &&
            cluster.Nos[2].State.OrdemGlobal().Count == 2);

        // Derruba o líder (id 3).
        cluster.Nos[3].Dispose();

        // Os nós ativos devem eleger o nó 2 (maior id restante).
        var reeleito = await Cluster.Esperar(() =>
            cluster.Nos[1].State.LiderAtual == 2 && cluster.Nos[2].State.LiderAtual == 2,
            timeoutMs: 10000);
        Assert.True(reeleito, "o novo líder (nó 2) não foi eleito após a queda");

        // O novo líder retoma a numeração sem lacunas nem duplicatas.
        cluster.Nos[1].EnviarGrupo("c");
        cluster.Nos[2].EnviarGrupo("d");

        var entregou = await Cluster.Esperar(() =>
            cluster.Nos[1].State.OrdemGlobal().Count == 4 &&
            cluster.Nos[2].State.OrdemGlobal().Count == 4,
            timeoutMs: 10000);
        Assert.True(entregou, "as mensagens pós-reeleição não foram entregues");

        var seqs = cluster.Nos[1].State.OrdemGlobal().Select(g => g.Seq).OrderBy(x => x).ToList();
        Assert.Equal(seqs.Distinct().Count(), seqs.Count); // sem duplicatas
        Assert.Equal(new long[] { 1, 2, 3, 4 }, seqs);     // sem lacunas

        // Filas idênticas entre os nós ativos.
        Assert.Equal(
            cluster.Nos[1].State.OrdemGlobal().OrderBy(g => g.Seq).Select(g => g.Conteudo),
            cluster.Nos[2].State.OrdemGlobal().OrderBy(g => g.Seq).Select(g => g.Conteudo));
    }
}
