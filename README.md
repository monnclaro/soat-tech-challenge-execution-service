# SOAT — Execution Service

Microsserviço responsável pela **Execução/Produção** dentro da arquitetura de microsserviços da Fase 4 do Tech Challenge (FIAP). Extraído do monolito [`soat-tech-challenge`](https://github.com/monnclaro/soat-tech-challenge), que permanece como referência histórica das Fases 1-3.

Plano completo da migração (arquitetura, saga, infraestrutura, ordem de execução): [`PLANO-FASE-4-MICROSSERVICOS.md`](../PLANO-FASE-4-MICROSSERVICOS.md) na raiz do workspace.

## Responsabilidades

- Fila de execução da oficina (o que os mecânicos acompanham).
- Diagnóstico: registrar os serviços/produtos identificados numa OS e finalizar o diagnóstico.
- Reparo: iniciar/finalizar a execução de cada serviço identificado, item a item.
- Comunicar finalização do diagnóstico e da execução ao OS Service/Billing Service (via mensageria, num follow-up PR).

Este serviço nunca cria uma Ordem de Serviço — ele recebe o `IdOrdemServico` (dono é o OS Service) e passa a gerenciar, a partir daí, o ciclo de diagnóstico e execução daquela OS.

## Por que MongoDB

Esta é a única base não-relacional das 3 (decisão validada no plano de migração, seção 1.3), e o domínio se encaixa bem no modelo de documentos:

- Um documento por Ordem de Serviço (coleção `execucoes`), chaveado pelo próprio `IdOrdemServico`.
- `Servicos[]`/`Produtos[]` são **arrays embutidos** no mesmo documento — cada um é lido/escrito sempre junto com o "cabeçalho" da execução (nunca há uma consulta que traga só os produtos de uma OS sem o resto), então não há motivo para normalizar em tabelas/coleções separadas.
- Todo o histórico de status por item (datas de início/fim de execução) é um "snapshot" que já foi decidido no monolito de origem (`OrdemServicoServico`/`OrdemServicoProduto`) — sem necessidade de joins, integridade referencial entre serviços, ou transações multi-documento.
- É exatamente o tipo de agregado ("fila de trabalho com sub-itens de estado") onde um banco de documentos elimina o overhead de mapeamento objeto-relacional que o EF Core exige no OS Service/Billing Service.

## Arquitetura

Clean Architecture, mesmo padrão validado no monolito de origem e replicado no OS Service:

```
src/
  Domain/         # Entidades, regras de negócio, sem dependências externas
  Application/    # Casos de uso, ports, controllers de aplicação
  Infrastructure/ # MongoDB.Driver, segurança (validação JWT)
  Api/            # ASP.NET Core host, controllers, presenters, middlewares
  SharedKernel/   # Tipos cross-cutting (marcadores de DI)
```

Regras de dependência entre camadas garantidas por testes de arquitetura (NetArchTest) em `tests/Tests/Camadas`.

### Agregado de domínio

`ExecucaoOrdemServico` é o agregado raiz, chaveado por `IdOrdemServico`. Reúne, adaptadas a um único agregado, duas responsabilidades que viviam separadas no monolito (`soat-tech-challenge`):

- **Edição de diagnóstico** — portada de `OrdemServico.InserirServicos/InserirProdutos/RemoverServico/RemoverProduto/FinalizarDiagnostico`: `AdicionarServicoDiagnosticado`, `AdicionarProdutoDiagnosticado`, `RemoverServicoDiagnosticado`, `RemoverProdutoDiagnosticado` (só permitido enquanto `Status == EmDiagnostico`) e `FinalizarDiagnostico()`.
- **Execução por item** — portada quase inalterada de `OrdemServicoServico.IniciarExecucao/FinalizarExecucao` (hoje em `Domain/Execucoes/Itens/ItemServico.cs`): `IniciarExecucaoServico(idServico)`/`FinalizarExecucaoServico(idServico)` no agregado delegam para o item e promovem o agregado para `EmExecucao` na primeira chamada e para `Finalizada` quando o último serviço termina.
- `Cancelar()` é o caminho de compensação da saga (veículo não atendível durante o diagnóstico, ou falha na execução).

### Persistência (MongoDB)

`MongoContext` é um wrapper fino sobre `IMongoDatabase`/`IMongoCollection<ExecucaoOrdemServicoDocument>` — não existe (nem faz sentido existir) um equivalente a `DbContext`/migrations do EF Core, já que MongoDB é schemaless.

Optamos por um **documento de persistência separado** (`ExecucaoOrdemServicoDocument` + `ExecucaoOrdemServicoMapper`) em vez de anotar o agregado de domínio diretamente com atributos `MongoDB.Bson`:

- mantém o `Domain` livre de qualquer dependência de infraestrutura (garantido pelos testes de arquitetura);
- preserva o desenho de construtores privados/factory methods (`Abrir`, `Reidratar`) do agregado, sem precisar relaxar encapsulamento para o serializador do driver conseguir materializar o objeto.

## Autenticação

Este serviço **nunca emite tokens** — não há login/`AuthenticationController` aqui. Ele é um resource server puro: valida o JWT emitido pelo OS Service (ou pela Lambda de auth), usando o mesmo segredo simétrico compartilhado (`JwtSettings:Secret`, ADR 0005 do monolito de origem).

## Mensageria (RabbitMQ/MassTransit)

Ligada: `IniciarDiagnosticoConsumer` e `IniciarExecucaoConsumer` reagem aos comandos
publicados pelo OS Service, reaproveitando os use cases já existentes (mesma regra de
negócio dos endpoints REST internos). Como o comando `IniciarExecucao` só carrega o
`IdOrdemServico` (o OS Service não acompanha os serviços item a item), o consumer busca a
própria `ExecucaoOrdemServico` e inicia a execução de todos os serviços ainda
`AguardandoExecucao`. Ao finalizar o diagnóstico ou o último serviço, publica
`DiagnosticoFinalizado`/`ExecucaoFinalizada` (via novos handlers de domain event
`PublicarDiagnosticoFinalizadoHandler`/`PublicarExecucaoFinalizadaHandler`). Contratos em
`Soat.Contracts.Saga` (`src/Application/Messaging/Contracts/SagaContracts.cs`), cópia
idêntica à dos outros dois serviços (sem pacote NuGet compartilhado — ver plano).

**Verificado contra infraestrutura real** (RabbitMQ local, sem mocks): um publisher
standalone simulando o OS Service publicou `IniciarDiagnostico`, e o consumer efetivamente
recebeu a mensagem, chamou `IniciarDiagnosticoUseCase` e tentou persistir no MongoDB —
sem um MongoDB rodando neste ambiente, a chamada expirou após 30s com um erro de conexão
real do driver (não um erro de desserialização ou de roteamento), confirmando que o
pipeline RabbitMQ → consumer → use case → gateway está corretamente ligado até a fronteira
do banco.

## Status / escopo deste PR

Clean Architecture completa, domínio com a máquina de estados real (não é stub), casos de
uso, API REST, mensageria RabbitMQ/MassTransit ligada, testes de arquitetura e testes
unitários do domínio.

**Fora de escopo, propositalmente adiado para follow-ups** (ver `PLANO-FASE-4-MICROSSERVICOS.md`):
- **Kubernetes** (incluindo o StatefulSet do MongoDB) e **CI/CD**: infraestrutura de deploy fica para as fases de infra do plano.
- Testes de integração com Testcontainers (Mongo) — os testes deste PR são testes unitários de domínio + arquitetura; não há MongoDB local neste ambiente de scaffold.
- `RemoverServicoDiagnosticado`/`RemoverProdutoDiagnosticado` existem no agregado mas não têm endpoint/consumer próprio (fora da lista fixa de rotas do plano).

## Rodando localmente

```bash
cp .env.example .env   # ajuste os segredos
docker compose up --build
```

API em `http://localhost:8083`, documentação OpenAPI (Scalar) em `/scalar` (ambiente de desenvolvimento), health check em `/health`. MongoDB exposto em `localhost:27018` (banco `soat_execucao`).

## Endpoints

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/v1/fila` | Fila de execução (registros não finalizados/cancelados) |
| GET | `/api/v1/execucoes/{idOrdemServico}` | Detalhe da execução de uma OS |
| PATCH | `/api/v1/execucoes/{idOrdemServico}/diagnostico/iniciar` | Inicia o diagnóstico |
| PATCH | `/api/v1/execucoes/{idOrdemServico}/diagnostico/finalizar` | Registra serviços/produtos identificados e finaliza o diagnóstico |
| PATCH | `/api/v1/execucoes/{idOrdemServico}/servicos/{idServico}/iniciar-execucao` | Inicia a execução de um serviço |
| PATCH | `/api/v1/execucoes/{idOrdemServico}/servicos/{idServico}/finalizar-execucao` | Finaliza a execução de um serviço |

Todos exigem `Authorization: Bearer <jwt>`.

## Testes

```bash
dotnet test
```

## CI/CD

`.github/workflows/ci-cd.yml`, mesmo padrão dos outros 2 serviços:

1. **Gate de cobertura (80%)** — `dotnet test` com Coverlet (`/p:Threshold=80 /p:ThresholdType=line`), falha o job se ficar abaixo.
2. **Quality Gate do SonarCloud** — `dotnet-sonarscanner begin/end` em volta do build, consumindo o relatório OpenCover do Coverlet.

Em `pull_request`, roda só `build-test`. Em `push` para `main`, roda também `deploy`: aplica o MongoDB (StatefulSet + PVC + Service headless, auto-hospedado — sem custo de serviço gerenciado adicional), aguarda ele ficar pronto, build/push da imagem no ECR e `kubectl apply` dos manifests da API em `k8s/` (namespace `soat-execucao`, NodePort `30083`).

### Secrets necessários no repositório GitHub

| Secret | Uso |
|---|---|
| `AWS_ACCESS_KEY_ID` / `AWS_SECRET_ACCESS_KEY` / `AWS_SESSION_TOKEN` | Credenciais de sessão temporária da AWS Academy (deploy) |
| `SONAR_TOKEN` | Autenticação no SonarCloud (org `monnclaro`) |
| `NEW_RELIC_LICENSE_KEY` | Injetada no Secret do deployment |

### Proteção da branch `main`

Configuração manual no GitHub (Settings > Branches): exigir PR antes do merge, exigir que o check `Build, Test & Quality Gate` passe, sem push direto.
