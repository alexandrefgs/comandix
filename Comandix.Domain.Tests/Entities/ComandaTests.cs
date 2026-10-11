using Comandix.Domain.Entities;
using Comandix.Domain.Enums;
using Comandix.Domain.Exceptions;

namespace Comandix.Domain.Tests.Entities;

public class ComandaTests
{
    [Fact]
    public void Abrir_DeveOcuparMesaECriarUmaPessoa()
    {
        var mesa = NovaMesa();

        var comanda = new Comanda(mesa, NovoGarcom());

        Assert.Equal(StatusComanda.Aberta, comanda.Status);
        Assert.Equal(StatusMesa.Ocupada, mesa.Status);
        var pessoa = Assert.Single(comanda.Pessoas);
        Assert.Equal("Pessoa 1", pessoa.Nome);
    }

    [Theory]
    [InlineData(Cargo.Cozinha)]
    [InlineData(Cargo.Caixa)]
    [InlineData(Cargo.Gerente)]
    public void Abrir_ComFuncionarioQueNaoEhGarcom_DeveLancarExcecao(Cargo cargo)
    {
        var funcionario = new Funcionario("Ana", cargo);

        Assert.Throws<DomainException>(() => new Comanda(NovaMesa(), funcionario));
    }

    [Fact]
    public void Abrir_ComGarcomInativo_DeveLancarExcecao()
    {
        var garcom = NovoGarcom();
        garcom.Desativar();

        Assert.Throws<DomainException>(() => new Comanda(NovaMesa(), garcom));
    }

    [Fact]
    public void Abrir_EmMesaOcupada_DeveLancarExcecao()
    {
        var mesa = NovaMesa();
        _ = new Comanda(mesa, NovoGarcom());

        var excecao = Assert.Throws<DomainException>(() => new Comanda(mesa, NovoGarcom()));

        Assert.Equal("A mesa já está ocupada.", excecao.Message);
    }

    [Fact]
    public void AdicionarItem_ComProdutoIndisponivel_DeveLancarExcecao()
    {
        var comanda = NovaComanda();
        var produto = NovoProduto("Pizza", 60m);
        produto.MarcarIndisponivel();

        Assert.Throws<DomainException>(() => comanda.AdicionarItem(produto, 1, null));
    }

    [Fact]
    public void AdicionarItem_DeveCongelarPrecoDoProduto()
    {
        var comanda = NovaComanda();
        var produto = NovoProduto("Pizza", 60m);
        var item = comanda.AdicionarItem(produto, 2, null);

        produto.Atualizar("Pizza", null, 80m, produto.CategoriaId);

        Assert.Equal(60m, item.PrecoUnitario);
        Assert.Equal(120m, comanda.Consumo);
    }

    [Fact]
    public void CancelarItem_NaoDeveEntrarNoConsumo()
    {
        var comanda = NovaComanda();
        comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);
        var cerveja = comanda.AdicionarItem(NovoProduto("Cerveja", 12m), 1, null);

        comanda.CancelarItem(cerveja.Id);

        Assert.Equal(StatusItem.Cancelado, cerveja.Status);
        Assert.Equal(60m, comanda.Consumo);
    }

    [Fact]
    public void CalcularDivisao_SemAtribuicao_DivideIgualmenteComTaxaIndividual()
    {
        var comanda = NovaComanda();
        comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);
        comanda.AdicionarItem(NovoProduto("Cerveja", 12m), 1, null);
        comanda.DefinirQuantidadePessoas(3);

        var divisao = comanda.CalcularDivisao();

        Assert.Equal(3, divisao.Count);
        Assert.All(
            divisao,
            d =>
            {
                Assert.Equal(24m, d.Consumo);
                Assert.Equal(2.40m, d.TaxaServico);
                Assert.Equal(26.40m, d.Total);
            }
        );
        Assert.Equal(79.20m, comanda.TotalComTaxa);
    }

    [Fact]
    public void CalcularDivisao_ComItemAtribuido_CobraSoDeQuemConsumiu()
    {
        var comanda = NovaComanda();
        comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);
        var cerveja = comanda.AdicionarItem(NovoProduto("Cerveja", 12m), 1, null);
        comanda.DefinirQuantidadePessoas(3);
        var joao = comanda.Pessoas.Single(p => p.Ordem == 1);

        comanda.AtribuirItem(cerveja.Id, new[] { joao.Id });

        var divisao = comanda.CalcularDivisao();
        Assert.Equal(new[] { 35.20m, 22.00m, 22.00m }, divisao.Select(d => d.Total));
        Assert.Equal(7.20m, comanda.CalcularTaxaServico());
    }

    [Fact]
    public void CalcularDivisao_ComCentavosQuebrados_SobraVaiParaPrimeiraPessoa()
    {
        var comanda = NovaComanda();
        comanda.DefinirCobrancaTaxaServico(false);
        comanda.AdicionarItem(NovoProduto("Suco", 10m), 1, null);
        comanda.DefinirQuantidadePessoas(3);

        var divisao = comanda.CalcularDivisao();

        Assert.Equal(new[] { 3.34m, 3.33m, 3.33m }, divisao.Select(d => d.Consumo));
        Assert.Equal(10m, divisao.Sum(d => d.Consumo));
    }

    [Fact]
    public void DefinirCobrancaTaxaServico_Desligada_DeveZerarTaxa()
    {
        var comanda = NovaComanda();
        comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);

        comanda.DefinirCobrancaTaxaServico(false);

        Assert.Equal(0m, comanda.CalcularTaxaServico());
        Assert.Equal(60m, comanda.TotalComTaxa);
    }

    [Fact]
    public void DefinirQuantidadePessoas_AoReduzir_RemoveAtribuicoesDaPessoaRemovida()
    {
        var comanda = NovaComanda();
        var cerveja = comanda.AdicionarItem(NovoProduto("Cerveja", 12m), 1, null);
        comanda.DefinirQuantidadePessoas(2);
        var pessoa2 = comanda.Pessoas.Single(p => p.Ordem == 2);
        comanda.AtribuirItem(cerveja.Id, new[] { pessoa2.Id });

        comanda.DefinirQuantidadePessoas(1);

        Assert.Empty(cerveja.Pagantes);
        Assert.Equal(12m, comanda.CalcularDivisao().Single().Consumo);
    }

    [Fact]
    public void AvancarStatusItem_DeveSeguirFluxoAteEntregue()
    {
        var comanda = NovaComanda();
        var item = comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);

        comanda.AvancarStatusItem(item.Id);
        Assert.Equal(StatusItem.EmPreparo, item.Status);

        comanda.AvancarStatusItem(item.Id);
        Assert.Equal(StatusItem.Pronto, item.Status);

        comanda.AvancarStatusItem(item.Id);
        Assert.Equal(StatusItem.Entregue, item.Status);

        Assert.Throws<DomainException>(() => comanda.AvancarStatusItem(item.Id));
    }

    [Fact]
    public void Fechar_ComItensNaoEntregues_DeveLancarExcecao()
    {
        var comanda = NovaComanda();
        comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);

        var excecao = Assert.Throws<DomainException>(comanda.Fechar);

        Assert.Equal("Ainda há itens que não foram entregues.", excecao.Message);
    }

    [Fact]
    public void Fechar_SemItens_DeveLancarExcecao()
    {
        var comanda = NovaComanda();

        Assert.Throws<DomainException>(comanda.Fechar);
    }

    [Fact]
    public void Fechar_DeveGravarTaxaELiberarMesa()
    {
        var mesa = NovaMesa();
        var comanda = new Comanda(mesa, NovoGarcom());
        var item = comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);
        Entregar(comanda, item);

        comanda.Fechar();

        Assert.Equal(StatusComanda.Fechada, comanda.Status);
        Assert.Equal(6m, comanda.ValorTaxaServico);
        Assert.NotNull(comanda.FechadaEm);
        Assert.Equal(StatusMesa.Livre, mesa.Status);
    }

    [Fact]
    public void ComandaFechada_NaoPodeSerAlterada()
    {
        var comanda = NovaComanda();
        var item = comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);
        Entregar(comanda, item);
        comanda.Fechar();

        Assert.Throws<DomainException>(() => comanda.DefinirCobrancaTaxaServico(false));
        Assert.Throws<DomainException>(() =>
            comanda.AdicionarItem(NovoProduto("Cerveja", 12m), 1, null)
        );
        Assert.Throws<DomainException>(() => comanda.DefinirQuantidadePessoas(2));
    }

    [Fact]
    public void Cancelar_ComItensAtivos_DeveLancarExcecao()
    {
        var comanda = NovaComanda();
        comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);

        Assert.Throws<DomainException>(comanda.Cancelar);
    }

    [Fact]
    public void Cancelar_SemItensAtivos_DeveLiberarMesaSemTaxa()
    {
        var mesa = NovaMesa();
        var comanda = new Comanda(mesa, NovoGarcom());
        var item = comanda.AdicionarItem(NovoProduto("Pizza", 60m), 1, null);
        comanda.CancelarItem(item.Id);

        comanda.Cancelar();

        Assert.Equal(StatusComanda.Cancelada, comanda.Status);
        Assert.Equal(0m, comanda.ValorTaxaServico);
        Assert.Equal(StatusMesa.Livre, mesa.Status);
    }

    private static Mesa NovaMesa() => new(1, 4);

    private static Funcionario NovoGarcom() => new("Carlos", Cargo.Garcom);

    private static Produto NovoProduto(string nome, decimal preco) =>
        new(nome, null, preco, Guid.NewGuid());

    private static Comanda NovaComanda() => new(NovaMesa(), NovoGarcom());

    private static void Entregar(Comanda comanda, ItemComanda item)
    {
        for (var i = 0; i < 3; i++)
            comanda.AvancarStatusItem(item.Id);
    }
}
