using Xunit;

namespace ChatDistribuido.Tests.Integration;

public class OrdemTotalTests
{
    [Fact]
    public async Task Filas_de_entrega_sao_identicas_em_todos_os_nos()
    {
        using var cluster = new Cluster(3);

        // Aguarda o bootstrap de liderança estabilizar.
        await Cluster.Esperar(() => cluster.Nos.Values.All(n => n.State.LiderAtual == 3));

        // Difunde mensagens concorrentes de nós diferentes.
        cluster.Nos[1].EnviarGrupo("a");
        cluster.Nos[2].EnviarGrupo("b");
        cluster.Nos[3].EnviarGrupo("c");
        cluster.Nos[1].EnviarGrupo("d");
        cluster.Nos[2].EnviarGrupo("e");

        var ok = await Cluster.Esperar(() =>
            cluster.Nos.Values.All(n => n.State.OrdemGlobal().Count == 5));
        Assert.True(ok, "nem todos os nós entregaram as 5 mensagens de grupo");

        // Todas as filas (por seq) devem ser idênticas.
        var referencia = SeqConteudo(cluster.Nos[1]);
        foreach (var n in cluster.Nos.Values)
            Assert.Equal(referencia, SeqConteudo(n));

        // Sem lacunas: seqs contíguos 1..5.
        Assert.Equal(new long[] { 1, 2, 3, 4, 5 }, referencia.Select(x => x.Item1));
    }

    private static List<(long, string)> SeqConteudo(ChatDistribuido.Servicos.NodeService n) =>
        n.State.OrdemGlobal().OrderBy(g => g.Seq).Select(g => (g.Seq, g.Conteudo)).ToList();
}
