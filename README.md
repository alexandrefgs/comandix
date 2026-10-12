# 🍽️ Comandix

[![CI](https://img.shields.io/github/actions/workflow/status/alexandrefgs/comandix/ci.yml?branch=main&style=for-the-badge&label=CI&logo=githubactions&logoColor=white)](https://github.com/alexandrefgs/comandix/actions/workflows/ci.yml)
![Último commit](https://img.shields.io/github/last-commit/alexandrefgs/comandix?style=for-the-badge&label=%C3%BAltimo%20commit)

Sistema de comandas para restaurantes, com cardápio, controle de mesas, fila da cozinha, divisão de conta, taxa de serviço por garçom, caixa com conferência, pagamentos por pessoa e emissão de NFC-e (simulada).

## Funcionalidades

### Salão e cozinha

- **Cardápio:** categorias e produtos, com controle de disponibilidade
- **Mesas e funcionários:** cadastro, ativação e cargos (garçom, cozinha, caixa, gerente)
- **Comandas:** uma por mesa, aberta por um garçom, com itens e observações
- **Preço congelado:** o item guarda o preço do momento do pedido; mudanças no cardápio não afetam comandas abertas
- **Fila da cozinha:** itens seguem o fluxo Pendente → Em preparo → Pronto → Entregue
- **Divisão de conta:** defina quantas pessoas, atribua itens a uma ou mais delas; itens sem atribuição são divididos entre todos
- **Taxa de serviço:** 10% por padrão, calculada individualmente sobre o consumo de cada pessoa, podendo ser dispensada
- **Relatório mensal:** total de taxa de serviço por garçom, considerando o fuso de Brasília

### Caixa e pagamentos

- **Caixa do dia:** abertura com fundo de troco e fechamento com conferência, mostrando a diferença entre o dinheiro esperado e o contado
- **Sangria e suprimento:** retiradas e reforços de dinheiro na gaveta, com motivo obrigatório
- **Pagamentos por pessoa:** cada pessoa da divisão paga a sua parte, em dinheiro (com cálculo de troco), Pix, débito ou crédito
- **Estorno:** pagamentos lançados errado podem ser estornados com motivo; o registro original é mantido no histórico
- **Fechamento seguro:** a comanda só fecha com todos os itens entregues e saldo zerado
- **Resumo do caixa:** total vendido por forma de pagamento, calculado diretamente no banco

### Nota fiscal

- **NFC-e simulada:** emitida automaticamente no fechamento da comanda, com numeração sequencial por série
- **Chave de acesso de 44 dígitos:** montada com a estrutura oficial (UF, ano/mês, CNPJ, modelo 65, série, número, código numérico) e dígito verificador calculado pelo módulo 11
- **Pronta para emissão real:** o emissor é desacoplado pela interface `IEmissorNotaFiscal`; trocar pela integração com um provedor exige apenas uma nova implementação e uma linha de configuração
- Toda nota simulada é marcada como **"SIMULAÇÃO - DOCUMENTO SEM VALOR FISCAL"**

### Acesso e segurança

- **Autenticação com JWT:** gerente e caixa entram com usuário e senha; garçom e cozinha com QR Code + PIN de 4 dígitos
- **Permissões por cargo:** cada endpoint libera apenas os cargos que fazem sentido
- **Proteção das credenciais:** senhas e PINs com hash PBKDF2, código do QR com SHA-256, bloqueio após 5 PINs errados e regeneração do QR em caso de perda do crachá

## Perfis de acesso

| Área | Gerente | Garçom | Cozinha | Caixa |
|---|:---:|:---:|:---:|:---:|
| Consultar cardápio, mesas, comandas e caixa atual | ✅ | ✅ | ✅ | ✅ |
| Cadastrar cardápio, mesas e funcionários | ✅ | | | |
| Marcar produto disponível/indisponível | ✅ | ✅ | ✅ | |
| Abrir comanda, lançar itens, dividir conta | ✅ | ✅ | | |
| Fila da cozinha e avanço de status | ✅ | ✅ | ✅ | |
| Registrar pagamento, fechar e cancelar comanda | ✅ | ✅ | | ✅ |
| Consultar nota fiscal | ✅ | ✅ | | ✅ |
| Estornar pagamento | ✅ | | | ✅ |
| Abrir e fechar caixa, sangria e suprimento | ✅ | | | ✅ |
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
Comandix.Domain          → entidades, regras de negócio, exceções de domínio, contrato do emissor fiscal
Comandix.Infrastructure  → EF Core, mapeamentos, migrations, emissor de NFC-e simulado
Comandix.Api             → controllers, DTOs, autenticação, tratamento de erros
Comandix.Domain.Tests    → testes de unidade das regras de negócio
Comandix.Api.Tests       → testes de integração da API com banco real
```

As regras de negócio ficam no domínio (por exemplo, `Comanda.Fechar()` impede fechar com itens não entregues ou saldo pendente). Violações viram `DomainException`, que um handler global converte em respostas `400` no padrão `ProblemDetails` (RFC 9110).

A API é protegida por padrão: todo endpoint exige autenticação, e apenas o login é público.

Garantias importantes também ficam no banco, com índices únicos filtrados: uma comanda aberta por mesa, um único caixa aberto por vez e numeração de notas fiscais sem repetição.

## Nota fiscal: simulação e emissão real

O emissor é escolhido pela configuração `NotaFiscal:Modo` no `appsettings.json`:

```json
"NotaFiscal": {
  "Modo": "Simulacao",
  "Serie": 1,
  "CodigoUf": 42,
  "CnpjEmitente": "00000000000000",
  "RazaoSocialEmitente": "Comandix Restaurante (Simulação)"
}
```

Sem um modo válido configurado, nenhuma nota é emitida. Para integrar com um provedor real de NFC-e, basta criar uma nova implementação de `IEmissorNotaFiscal` e registrá-la no `Program.cs` com o modo correspondente. Cada nota guarda se foi simulada, então notas emitidas antes da integração nunca se confundem com notas reais.

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

São mais de 100 testes entre unidade e integração. Os testes de integração criam e destroem o banco `ComandixDb_Tests` a cada execução, sem tocar no banco de desenvolvimento. No GitHub Actions, eles rodam contra um SQL Server em container a cada push.

## Roadmap

- [x] Cardápio, mesas e funcionários
- [x] Comandas com divisão de conta e taxa de serviço
- [x] Fila da cozinha e relatório de taxa por garçom
- [x] Testes automatizados e CI
- [x] Autenticação com JWT, permissões por cargo e login por QR Code + PIN
- [x] **Caixa e pagamentos**
  - [x] Abertura do caixa com fundo de troco e fechamento com conferência (diferença entre esperado e contado)
  - [x] Sangria e suprimento
  - [x] Pagamentos por pessoa da divisão (dinheiro com troco, Pix, débito e crédito)
  - [x] Estorno de pagamento com motivo
  - [x] Comanda só fecha com saldo zerado
- [ ] **Nota fiscal (NFC-e)**
  - [x] Emissão simulada no fechamento da comanda, com chave de acesso de 44 dígitos e dígito verificador (módulo 11), marcada como documento sem valor fiscal
  - [x] Emissor desacoplado por interface (`IEmissorNotaFiscal`), pronto para troca por um provedor real
  - [ ] Integração com provedor real de NFC-e
- [ ] Front-end em Angular
- [ ] Deploy
- [ ] Múltiplos caixas simultâneos
- [ ] Estoque e ficha técnica

## Autor

**Alexandre**

[![GitHub](https://img.shields.io/badge/GitHub-alexandrefgs-181717?style=for-the-badge&logo=github&logoColor=white)](https://github.com/alexandrefgs)
[![LinkedIn](https://img.shields.io/badge/LinkedIn-alexandre--fgs-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white)](https://www.linkedin.com/in/alexandre-fgs)