using ArchUnitNET.Domain;
using ArchUnitNET.Fluent.Syntax.Elements.Types;

namespace WhatsEnvio.ArchitectureTests.Extensions;

internal static class ArchitectureGuards
{
    public static GivenTypesConjunction NaoVazia(this GivenTypesConjunction selecao,
        Architecture arquitetura,
        string descricao)
    {
        Assert.True(selecao.GetObjects(arquitetura).Any(),
            @$"Seleção vacuosa: {descricao} não
                          casou com nenhum tipo carregado
                           — a regra passaria sem checar nada.");
        return selecao;
    }
}
