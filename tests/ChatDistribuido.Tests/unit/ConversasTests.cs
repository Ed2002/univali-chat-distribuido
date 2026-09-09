using ChatDistribuido.Servicos;
using Xunit;

namespace ChatDistribuido.Tests.Unit;

public class ConversasTests
{
    private static readonly int[] Vc = { 0, 0, 0 };

    [Fact]
    public void PrivadasCom_agrupa_por_par_enviadas_e_recebidas()
    {
        var state = new NodeState(id: 1, n: 3); // eu sou o nó 1

        state.RegistrarPrivada(de: 1, para: 2, conteudo: "oi 2", Vc, enviada: true);
        state.RegistrarPrivada(de: 3, para: 1, conteudo: "oi de 3", Vc, enviada: false);
        state.RegistrarPrivada(de: 1, para: 2, conteudo: "tudo bem 2?", Vc, enviada: true);

        var com2 = state.PrivadasCom(2);
        var com3 = state.PrivadasCom(3);

        // Par 2: as duas enviadas ao nó 2, na ordem.
        Assert.Equal(new[] { "oi 2", "tudo bem 2?" }, com2.Select(p => p.Conteudo));
        // Par 3: a recebida do nó 3.
        Assert.Equal(new[] { "oi de 3" }, com3.Select(p => p.Conteudo));
    }

    [Fact]
    public void Mensagens_de_grupo_nao_entram_nas_privadas()
    {
        var state = new NodeState(id: 1, n: 3);

        state.RegistrarEntregaGlobal(seq: 1, deOriginal: 2, conteudo: "grupo!", Vc);
        state.RegistrarPrivada(de: 1, para: 2, conteudo: "privada", Vc, enviada: true);

        // O grupo tem só a de grupo; as privadas com o nó 2 têm só a privada.
        Assert.Single(state.OrdemGlobal());
        Assert.DoesNotContain(state.PrivadasCom(2), p => p.Conteudo == "grupo!");
        Assert.Single(state.PrivadasCom(2));
    }

    [Fact]
    public void PrivadasCom_ignora_pares_diferentes()
    {
        var state = new NodeState(id: 1, n: 3);
        state.RegistrarPrivada(de: 1, para: 2, conteudo: "para 2", Vc, enviada: true);

        Assert.Empty(state.PrivadasCom(3));
    }
}
