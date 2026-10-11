# 🍽️ Comandix

[![CI](https://img.shields.io/github/actions/workflow/status/alexandrefgs/comandix/ci.yml?branch=main&style=for-the-badge&label=CI&logo=githubactions&logoColor=white)](https://github.com/alexandrefgs/comandix/actions/workflows/ci.yml)
![Último commit](https://img.shields.io/github/last-commit/alexandrefgs/comandix?style=for-the-badge&label=%C3%BAltimo%20commit)

Sistema de comandas para restaurantes, com cardápio, controle de mesas, fila da cozinha, divisão de conta, taxa de serviço por garçom e controle de acesso por cargo.

## Funcionalidades

- **Cardápio:** categorias e produtos, com controle de disponibilidade
- **Mesas e funcionários:** cadastro, ativação e cargos (garçom, cozinha, caixa, gerente)
- **Comandas:** uma por mesa, aberta por um garçom, com itens e observações
- **Preço congelado:** o item guarda o preço do momento do pedido; mudanças no cardápio não afetam comandas abertas
- **Fila da cozinha:** itens seguem o fluxo Pendente → Em preparo → Pronto → Entregue
- **Divisão de conta:** defina quantas pessoas, atribua itens a uma ou mais delas; itens sem atribuição são divididos entre todos
- **Taxa de serviço:** 10% por padrão, calculada individualmente sobre o consumo de cada pessoa, podendo ser dispensada
- **Relatório mensal:** total de taxa de serviço por garçom, considerando o fuso de Brasília
- **Autenticação com JWT:** gerente e caixa entram com usuário e senha; garçom e cozinha com QR Code + PIN de 4 dígitos
- **Permissões por cargo:** cada endpoint libera apenas os cargos que fazem sentido
- **Segurança do acesso:** senhas e PINs com hash PBKDF2, código do QR com SHA-256, bloqueio após 5 PINs errados e regeneração do QR em caso de perda do crachá

## Perfis de acesso

| Área | Gerente | Garçom | Cozinha | Caixa |
|---|:---:|:---:|:---:|:---:|
| Consultar cardápio, mesas e comandas | ✅ | ✅ | ✅ | ✅ |
| Cadastrar cardápio, mesas e funcionários | ✅ | | | |
| Marcar produto disponível/indisponível | ✅ | ✅ | ✅ | |
| Abrir comanda, lançar itens, dividir conta | ✅ | ✅ | | |
| Fila da cozinha e avanço de status | ✅ | ✅ | ✅ | |
| Fechar e cancelar comanda | ✅ | ✅ | | ✅ |
| Relatório de taxa de serviço | ✅ | só o próprio | | |

O garçom que abre uma comanda é sempre o usuário logado, garantindo que a taxa de serviço vá para quem de fato atendeu a mesa.

## Tecnologias

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-14-239120?style=for-the-badge)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-Web_API-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=for-the-badge)
![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-testes-5E1F87?style=for-the-badge)
![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-CI-2088FF?style=for-the-badge&logo=githubactions&logoColor=white)

## Arquitetura

```
Comandix.Domain          → entidades, regras de negócio, exceções de domínio
Comandix.Infrastructure  → EF Core, mapeamentos, migrations
Comandix.Api             → controllers, DTOs, autenticação, tratamento de erros
Comandix.Domain.Tests    → testes de unidade das regras de negócio
Comandix.Api.Tests       → testes de integração da API com banco real
```

As regras de negócio ficam no domínio (por exemplo, `Comanda.Fechar()` impede fechar com itens não entregues). Violações viram `DomainException`, que um handler global converte em respostas `400` no padrão `ProblemDetails` (RFC 9110).

A API é protegida por padrão: todo endpoint exige autenticação, e apenas o login é público.

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

Em desenvolvimento, um gerente inicial é criado automaticamente (usuário `admin`, senha `Admin@123456`, configurável em `appsettings.Development.json`). Faça login em `POST /api/auth/login` e use o botão **Authorize** do Swagger com o token retornado.

A connection string fica em `Comandix.Api/appsettings.json`. Em produção, a chave de assinatura dos tokens deve ser informada pela variável de ambiente `Jwt__Chave` (mínimo de 32 caracteres).

## Testes

```bash
dotnet test
```

Os testes de integração criam e destroem o banco `ComandixDb_Tests` a cada execução, sem tocar no banco de desenvolvimento. No GitHub Actions, eles rodam contra um SQL Server em container a cada push.

## Roadmap

- [x] Cardápio, mesas e funcionários
- [x] Comandas com divisão de conta e taxa de serviço
- [x] Fila da cozinha e relatório de taxa por garçom
- [x] Testes automatizados e CI
- [x] Autenticação com JWT, permissões por cargo e login por QR Code + PIN
- [ ] **Caixa e pagamentos**
  - [ ] Abertura do caixa com fundo de troco e fechamento com conferência (diferença entre esperado e contado)
  - [ ] Sangria e suprimento
  - [ ] Pagamentos por pessoa da divisão (dinheiro com troco, Pix, débito e crédito)
  - [ ] Estorno de pagamento com motivo
  - [ ] Comanda só fecha com saldo zerado
- [ ] **Nota fiscal (NFC-e)**
  - [ ] Emissão simulada no fechamento da comanda, com chave de acesso de 44 dígitos e dígito verificador (módulo 11), marcada como documento sem valor fiscal
  - [ ] Emissor desacoplado por interface (`IEmissorNotaFiscal`), pronto para troca por um provedor real
  - [ ] Integração com provedor real de NFC-e
- [ ] Front-end em Angular
- [ ] Deploy
- [ ] Múltiplos caixas simultâneos
- [ ] Estoque e ficha técnica

## Autor

**Alexandre**

[![GitHub](https://img.shields.io/badge/GitHub-alexandrefgs-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/alexandrefgs)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-alexandre--fgs-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/alexandre-fgs)