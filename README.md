# SOAT — Execution Service

[![Quality gate status](https://sonarcloud.io/api/project_badges/measure?project=soat-tech-challenge-execution-service&metric=alert_status&token=70f4a02ca386c0bb2ca42db64130e18906b5698a)](https://sonarcloud.io/summary/new_code?id=soat-tech-challenge-execution-service)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=soat-tech-challenge-execution-service&metric=coverage&token=70f4a02ca386c0bb2ca42db64130e18906b5698a)](https://sonarcloud.io/summary/new_code?id=soat-tech-challenge-execution-service)

Microsserviço responsável pela **Execução/Produção** dentro da arquitetura de microsserviços da Fase 4 do Tech Challenge (FIAP). Extraído do monolito [`soat-tech-challenge`](https://github.com/monnclaro/soat-tech-challenge), que permanece como referência histórica das Fases 1-3.

## Responsabilidades

- Fila de execução da oficina (o que os mecânicos acompanham).
- Diagnóstico: registrar os serviços/produtos identificados numa OS e finalizar o diagnóstico.
- Reparo: iniciar/finalizar a execução de cada serviço identificado, item a item.
- Comunicar a finalização do diagnóstico e da execução ao OS Service via mensageria (RabbitMQ/MassTransit), como parte da saga orquestrada por ele.

Este serviço nunca cria uma Ordem de Serviço — ele recebe o `IdOrdemServico` (dono é o OS Service) e passa a gerenciar, a partir daí, o ciclo de diagnóstico e execução daquela OS.

## Papel na saga

O OS Service é o orquestrador: seu próprio agregado `OrdemServico` guarda o estado da saga e reage a domain events publicando comandos. Este serviço só reage a comandos e publica eventos de volta — não conhece os outros passos da saga (orçamento, pagamento), só os dois que lhe dizem respeito:

| Direção | Mensagem | Efeito neste serviço |
|---|---|---|
| OS Service → Execução (comando) | `IniciarDiagnostico` | Abre a fila de execução para a OS (`IniciarDiagnosticoUseCase`) |
| Execução → OS Service (evento) | `DiagnosticoFinalizado` | Publicado ao finalizar o registro dos itens diagnosticados |
| Execução → OS Service (evento) | `DiagnosticoFalhou` | **Compensação**: publicado por `Cancelar(motivo)` se ainda estiver na fase de diagnóstico (veículo não atendível) |
| OS Service → Execução (comando) | `IniciarExecucao` | Inicia a execução de todos os serviços ainda `AguardandoExecucao` (ver "Mensageria" abaixo) |
| Execução → OS Service (evento) | `ExecucaoFinalizada` | Publicado quando o último serviço termina a execução |
| Execução → OS Service (evento) | `ExecucaoFalhou` | **Compensação**: publicado por `Cancelar(motivo)` se o diagnóstico já tiver sido finalizado (ex.: peça indisponível durante a execução) |

Justificativa completa do desenho da saga (por que a orquestração vive no OS Service, sem um saga state machine separado): [ADR 0001 no repositório do OS Service](https://github.com/monnclaro/soat-tech-challenge-os-service/blob/main/docs/adr/0001-saga-orquestrada-sem-state-machine-separado.md).

## Por que MongoDB

Esta é a única base não-relacional das 3 (requisito do desafio: pelo menos 1 banco relacional e 1 não-relacional), e o domínio se encaixa bem no modelo de documentos:

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
  Infrastructure/ # MongoDB.Driver, mensageria (RabbitMQ/MassTransit), segurança (validação JWT)
  Api/            # ASP.NET Core host, controllers, presenters, middlewares
  SharedKernel/   # Tipos cross-cutting (marcadores de DI)
```

Regras de dependência entre camadas garantidas por testes de arquitetura (NetArchTest) em `tests/Tests/Camadas`.

Documentação completa (diagramas de camadas, modelo de domínio, máquinas de estado, sequência de diagnóstico/execução com compensação, mensageria): [docs/architecture.md](./docs/architecture.md).

### Agregado de domínio

`ExecucaoOrdemServico` é o agregado raiz, chaveado por `IdOrdemServico`. Reúne, adaptadas a um único agregado, duas responsabilidades que viviam separadas no monolito (`soat-tech-challenge`):

- **Edição de diagnóstico** — portada de `OrdemServico.InserirServicos/InserirProdutos/RemoverServico/RemoverProduto/FinalizarDiagnostico`: `AdicionarServicoDiagnosticado`, `AdicionarProdutoDiagnosticado`, `RemoverServicoDiagnosticado`, `RemoverProdutoDiagnosticado` (só permitido enquanto `Status == EmDiagnostico`) e `FinalizarDiagnostico()`.
- **Execução por item** — portada quase inalterada de `OrdemServicoServico.IniciarExecucao/FinalizarExecucao` (hoje em `Domain/Execucoes/Itens/ItemServico.cs`): `IniciarExecucaoServico(idServico)`/`FinalizarExecucaoServico(idServico)` no agregado delegam para o item e promovem o agregado para `EmExecucao` na primeira chamada e para `Finalizada` quando o último serviço termina.
- `Cancelar(motivo)` é o caminho de compensação da saga (veículo não atendível durante o diagnóstico, ou falha na execução) — exposto via `PATCH .../cancelar` e publica `DiagnosticoFalhou`/`ExecucaoFalhou` (via `PublicarExecucaoCanceladaHandler`, reagindo ao domain event `ExecucaoCanceladaDomainEvent`) dependendo da fase em que ocorreu, para o OS Service cancelar a OS.

### Persistência (MongoDB)

`MongoContext` é um wrapper fino sobre `IMongoDatabase`/`IMongoCollection<ExecucaoOrdemServicoDocument>` — não existe (nem faz sentido existir) um equivalente a `DbContext`/migrations do EF Core, já que MongoDB é schemaless.

Optamos por um **documento de persistência separado** (`ExecucaoOrdemServicoDocument` + `ExecucaoOrdemServicoMapper`) em vez de anotar o agregado de domínio diretamente com atributos `MongoDB.Bson`:

- mantém o `Domain` livre de qualquer dependência de infraestrutura (garantido pelos testes de arquitetura);
- preserva o desenho de construtores privados/factory methods (`Abrir`, `Reidratar`) do agregado, sem precisar relaxar encapsulamento para o serializador do driver conseguir materializar o objeto.

## Autenticação

Este serviço **nunca emite tokens** — não há login/`AuthenticationController` aqui. Ele é um resource server puro: valida o JWT emitido pelo OS Service, usando o mesmo segredo simétrico compartilhado (`JwtSettings:Secret`).

## Mensageria (RabbitMQ/MassTransit)

`IniciarDiagnosticoConsumer` e `IniciarExecucaoConsumer` reagem aos comandos publicados pelo OS Service, reaproveitando os use cases já existentes (mesma regra de negócio dos endpoints REST internos — nenhuma lógica duplicada entre a via HTTP e a via mensageria). Como o comando `IniciarExecucao` só carrega o `IdOrdemServico` (o OS Service não acompanha os serviços item a item), o consumer busca a própria `ExecucaoOrdemServico` e inicia a execução de todos os serviços ainda `AguardandoExecucao` (não existe um método de domínio para iniciar todos de uma vez — o consumer itera e chama `IniciarExecucaoServicoUseCase` por item). Ao finalizar o diagnóstico ou o último serviço, publica `DiagnosticoFinalizado`/`ExecucaoFinalizada` (via `PublicarDiagnosticoFinalizadoHandler`/`PublicarExecucaoFinalizadaHandler`, reagindo a domain events).

Contratos em `Soat.Contracts.Saga` (`src/Application/Messaging/Contracts/SagaContracts.cs`), cópia idêntica à dos outros dois serviços — mantida por convenção em cada repo em vez de um pacote NuGet compartilhado, para evitar a complexidade de um feed privado nesta fase do projeto (são DTOs puros, marcados com as interfaces `ISagaCommand`/`ISagaEvent` para deixar explícito no próprio tipo se é um comando ou um evento da saga).

**Verificado contra infraestrutura real** (RabbitMQ local, sem mocks): um publisher standalone simulando o OS Service publicou `IniciarDiagnostico`, e o consumer efetivamente recebeu a mensagem, chamou `IniciarDiagnosticoUseCase` e tentou persistir no MongoDB — sem um MongoDB rodando neste ambiente, a chamada expirou após 30s com um erro de conexão real do driver (não um erro de desserialização ou de roteamento), confirmando que o pipeline RabbitMQ → consumer → use case → gateway está corretamente ligado até a fronteira do banco.

## Banco de dados

MongoDB (`soat_execucao`) — instância própria e isolada (nenhum outro serviço acessa este banco diretamente). Em produção, roda auto-hospedado em um `StatefulSet` + `PersistentVolumeClaim` dentro do próprio namespace deste serviço (`k8s/mongodb.yaml`) — não um serviço gerenciado (DocumentDB/Atlas), para não gerar custo adicional na conta AWS Academy usada no desenvolvimento.

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
| PATCH | `/api/v1/execucoes/{idOrdemServico}/cancelar` | Cancela a execução (corpo `{ "motivo": "..." }`) — compensação da saga |

Todos exigem `Authorization: Bearer <jwt>`. Os passos de diagnóstico/execução também são disparados automaticamente pelos comandos de mensageria (ver "Mensageria" acima) — as rotas REST ficam mantidas para depuração/teste manual.

Especificação OpenAPI (Swagger) exportada em [`docs/openapi.json`](./docs/openapi.json) — importável direto no Postman (File > Import) ou em qualquer ferramenta compatível com OpenAPI 3. Com a API rodando localmente, a versão sempre atualizada também fica disponível em `/openapi/v1.json` (e a UI interativa do Scalar em `/scalar`).

## Testes e cobertura

```bash
dotnet test
```

Cobre: regras de arquitetura (NetArchTest, `tests/Tests/Camadas`), transições de estado do agregado `ExecucaoOrdemServico`/itens — incluindo `Cancelar(motivo)` e os dois ramos de compensação (`DiagnosticoFalhou`/`ExecucaoFalhou`) — e os use cases/consumers/presenters da Application/Infrastructure/Api (Moq).

### Evidência de cobertura

**106/106 testes passando**, gerado localmente com Coverlet
(`dotnet test -p:CollectCoverage=true -p:CoverletOutputFormat=opencover`):

| Módulo | Linha | Branch | Método |
|---|---|---|---|
| Api | 96,49% | 100% | 92,85% |
| Application | 90,1% | 100% | 77,9% |
| Domain | 97,8% | 93,75% | 96,77% |
| Infrastructure | 78,91% | 83,33% | 84% |
| SharedKernel | 100% | 100% | 100% |
| **Total** | **90,31%** | **94,25%** | **86,28%** |

Cobertura contínua nos badges no topo deste README e no
[dashboard do SonarCloud](https://sonarcloud.io/summary/new_code?id=soat-tech-challenge-execution-service).

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

## Escopo — o que ainda fica de fora deste repositório

- `RemoverServicoDiagnosticado`/`RemoverProdutoDiagnosticado` existem no agregado mas não têm endpoint/consumer próprio (fora da lista fixa de rotas do MVP).
- Testes de integração com Testcontainers (MongoDB) — os testes hoje são unitários (domínio + Application com Moq) e de arquitetura; não há um ambiente com MongoDB real neste repositório de testes ainda.
