# 🍽️ Comandix

[![CI](https://img.shields.io/github/actions/workflow/status/alexandrefgs/comandix/ci.yml?branch=main&style=for-the-badge&label=CI&logo=githubactions&logoColor=white)](https://github.com/alexandrefgs/comandix/actions/workflows/ci.yml)
![Último commit](https://img.shields.io/github/last-commit/alexandrefgs/comandix?style=for-the-badge&label=%C3%BAltimo%20commit)

Sistema de comandas para restaurantes, com cardápio, controle de mesas, fila da cozinha, divisão de conta e taxa de serviço por garçom.

## Funcionalidades

- **Cardápio:** categorias e produtos, com controle de disponibilidade
- **Mesas e funcionários:** cadastro, ativação e cargos (garçom, cozinha, caixa, gerente)
- **Comandas:** uma por mesa, aberta por um garçom, com itens e observações
- **Preço congelado:** o item guarda o preço do momento do pedido; mudanças no cardápio não afetam comandas abertas
- **Fila da cozinha:** itens seguem o fluxo Pendente → Em preparo → Pronto → Entregue
- **Divisão de conta:** defina quantas pessoas, atribua itens a uma ou mais delas; itens sem atribuição são divididos entre todos
- **Taxa de serviço:** 10% por padrão, calculada individualmente sobre o consumo de cada pessoa, podendo ser dispensada
- **Relatório mensal:** total de taxa de serviço por garçom, considerando o fuso de Brasília

## Tecnologias

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge)
![xUnit](https://img.shields.io/badge/xUnit-testes-5E1F87?style=for-the-badge)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-CI-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)

## Arquitetura

```
Comandix.Domain          → entidades, regras de negócio, exceções de domínio
Comandix.Infrastructure  → EF Core, mapeamentos, migrations
Comandix.Api             → controllers, DTOs, tratamento de erros
Comandix.Domain.Tests    → testes de unidade das regras de negócio
Comandix.Api.Tests       → testes de integração da API com banco real
```

As regras de negócio ficam no domínio (por exemplo, `Comanda.Fechar()` impede fechar com itens não entregues). Violações viram `DomainException`, que um handler global converte em respostas `400` no padrão `ProblemDetails` (RFC 9110).

## Como executar

Pré-requisitos: .NET 10 SDK e SQL Server (ou LocalDB).

```bash
git clone https://github.com/alexandrefgs/comandix.git
cd comandix
dotnet tool restore
dotnet ef database update -p Comandix.Infrastructure -s Comandix.Api
dotnet run --project Comandix.Api --launch-profile https
```

Acesse `https://localhost:<porta>` e você será redirecionado para o Swagger.

A connection string fica em `Comandix.Api/appsettings.json`.

## Testes

```bash
dotnet test
```

Os testes de integração criam e destroem o banco `ComandixDb_Tests` a cada execução, sem tocar no banco de desenvolvimento.

## Roadmap

- [x] Cardápio, mesas e funcionários
- [x] Comandas com divisão de conta e taxa de serviço
- [x] Fila da cozinha e relatório de taxa por garçom
- [x] Testes automatizados e CI
- [ ] Autenticação e perfis com JWT
- [ ] Caixa e pagamentos
- [ ] Front-end em Angular
- [ ] Estoque e ficha técnica

## Autor

**Alexandre**

[![GitHub](https://img.shields.io/badge/GitHub-alexandrefgs-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/alexandrefgs)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-alexandre--fgs-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/alexandre-fgs)