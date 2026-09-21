# Arquitetura — Execution Service

Documento complementar ao [README](../README.md), com mais profundidade sobre camadas,
modelo de domínio e persistência em MongoDB. Justificativa do desenho da saga como um
todo (por que a orquestração vive no OS Service): [ADR 0001 no repositório do OS
Service](https://github.com/monnclaro/soat-tech-challenge-os-service/blob/main/docs/adr/0001-saga-orquestrada-sem-state-machine-separado.md).

## Camadas (Clean Architecture)

```mermaid
graph TD
    Api["Api<br/>Controllers · Presenters · Middlewares"]
    App["Application<br/>UseCases · Controller de aplicação · EventHandlers · Ports"]
    Dom["Domain<br/>ExecucaoOrdemServico, ItemServico, ItemProduto · Domain Events"]
    Infra["Infrastructure<br/>MongoDB.Driver · MassTransit/RabbitMQ · JWT"]
    SK["SharedKernel<br/>Entity, IDomainEvent, marcadores de DI"]

    Api --> App
    App --> Dom
    Infra -.implementa ports.-> App
    Infra --> Dom
    Api -.-> SK
    App -.-> SK
    Dom -.-> SK
    Infra -.-> SK
```

Regra de dependência garantida por testes de arquitetura (NetArchTest,
`tests/Tests/Camadas`) — `Infrastructure` nunca é referenciada por `Application`/`Domain`.
Sem EF Core/DbContext: `MongoContext` é um wrapper fino sobre o driver oficial do
MongoDB, já que o banco é schemaless (sem migrations).

## Modelo de domínio

```mermaid
classDiagram
    class ExecucaoOrdemServico {
        +Guid IdOrdemServico
        +StatusExecucaoOrdemServico Status
        +string MotivoCancelamento
        +List~ItemServico~ Servicos
        +List~ItemProduto~ Produtos
        +Abrir(idOrdemServico)$
        +IniciarDiagnostico()
        +AdicionarServicoDiagnosticado(id, nome, valor)
        +AdicionarProdutoDiagnosticado(id, nome, valorUnit, qtd)
        +RemoverServicoDiagnosticado(idItem)
        +RemoverProdutoDiagnosticado(idItem)
        +FinalizarDiagnostico()
        +IniciarExecucaoServico(idServico)
        +FinalizarExecucaoServico(idServico)
        +Cancelar(motivo)
    }
    class ItemServico {
        +Guid IdServico
        +string NomeServico
        +decimal Valor
        +StatusItemServico Status
        +IniciarExecucao()
        +FinalizarExecucao()
    }
    class ItemProduto {
        +Guid IdProduto
        +string NomeProduto
        +decimal ValorUnitario
        +decimal Quantidade
        +decimal Subtotal
    }

    ExecucaoOrdemServico "1" *-- "*" ItemServico : Servicos (embutido)
    ExecucaoOrdemServico "1" *-- "*" ItemProduto : Produtos (embutido)
```

`ItemServico`/`ItemProduto` são **arrays embutidos no mesmo documento** MongoDB (não
coleções próprias) — ver README > "Por que MongoDB" para a justificativa completa.

### Máquina de estados

```mermaid
stateDiagram-v2
    [*] --> AguardandoDiagnostico: Abrir()
    AguardandoDiagnostico --> EmDiagnostico: IniciarDiagnostico()
    EmDiagnostico --> DiagnosticoFinalizado: FinalizarDiagnostico()
    DiagnosticoFinalizado --> EmExecucao: IniciarExecucaoServico() [1ª chamada]
    EmExecucao --> Finalizada: FinalizarExecucaoServico() [último item]

    AguardandoDiagnostico --> Cancelada: Cancelar(motivo) → publica DiagnosticoFalhou
    EmDiagnostico --> Cancelada: Cancelar(motivo) → publica DiagnosticoFalhou
    DiagnosticoFinalizado --> Cancelada: Cancelar(motivo) → publica ExecucaoFalhou
    EmExecucao --> Cancelada: Cancelar(motivo) → publica ExecucaoFalhou
```

`Cancelar(motivo)` decide sozinho qual evento da saga publicar: se o status anterior
era `AguardandoDiagnostico`/`EmDiagnostico`, é uma falha de diagnóstico
(`DiagnosticoFalhou`); caso contrário, é uma falha de execução (`ExecucaoFalhou`) —
ver `PublicarExecucaoCanceladaHandler`.

### Máquina de estados de `ItemServico`

```mermaid
stateDiagram-v2
    [*] --> AguardandoExecucao
    AguardandoExecucao --> EmExecucao: IniciarExecucao()
    EmExecucao --> ExecucaoFinalizada: FinalizarExecucao()
```

## Fluxo — diagnóstico e execução

```mermaid
sequenceDiagram
    participant OS as OS Service
    participant Exec as Execução Service

    OS-->>Exec: IniciarDiagnostico (comando)
    Exec->>Exec: Abrir() → AguardandoDiagnostico
    Exec->>Exec: IniciarDiagnostico() → EmDiagnostico
    Note over Exec: Mecânico registra serviços/produtos<br/>(PATCH .../diagnostico/finalizar)

    alt Diagnóstico concluído
        Exec->>Exec: FinalizarDiagnostico() → DiagnosticoFinalizado
        Exec-->>OS: DiagnosticoFinalizado (evento)
    else Veículo não atendível
        Exec->>Exec: Cancelar(motivo) → Cancelada
        Note over Exec: Compensação
        Exec-->>OS: DiagnosticoFalhou (evento)
    end

    OS-->>Exec: IniciarExecucao (comando, só IdOrdemServico)
    Note over Exec: Consumer busca a própria ExecucaoOrdemServico e chama<br/>IniciarExecucaoServico() uma vez por item AguardandoExecucao

    alt Todos os serviços concluídos
        Exec->>Exec: FinalizarExecucaoServico() no último item → Finalizada
        Exec-->>OS: ExecucaoFinalizada (evento)
    else Falha durante a execução (ex.: peça indisponível)
        Exec->>Exec: Cancelar(motivo) → Cancelada
        Note over Exec: Compensação
        Exec-->>OS: ExecucaoFalhou (evento)
    end
```

## Mensageria — comandos e consumers

| Mensagem | Tipo | Direção | Consumer/Handler |
|---|---|---|---|
| `IniciarDiagnostico` | Comando | OS → Execução | `IniciarDiagnosticoConsumer` |
| `IniciarExecucao` | Comando | OS → Execução | `IniciarExecucaoConsumer` (itera os itens `AguardandoExecucao`) |
| `DiagnosticoFinalizado` | Evento | Execução → OS | — |
| `DiagnosticoFalhou` | Evento (compensação) | Execução → OS | — |
| `ExecucaoFinalizada` | Evento | Execução → OS | — |
| `ExecucaoFalhou` | Evento (compensação) | Execução → OS | — |

Publishers em `Infrastructure/Messaging/MassTransitSagaEventPublisher.cs`, reagindo a
domain events via `PublicarDiagnosticoFinalizadoHandler`/`PublicarExecucaoFinalizadaHandler`/
`PublicarExecucaoCanceladaHandler`. Contratos compartilhados (cópia idêntica nos 3
repos) em `Application/Messaging/Contracts/SagaContracts.cs`.

## Persistência (MongoDB)

Um documento por Ordem de Serviço (coleção `execucoes`), chaveado pelo próprio
`IdOrdemServico`. `ExecucaoOrdemServicoDocument` + `ExecucaoOrdemServicoMapper`
mantêm o `Domain` livre de qualquer dependência do driver MongoDB (`MongoDB.Bson`) —
ver README > "Persistência (MongoDB)" para a justificativa completa do documento de
persistência separado.

## Segurança

Nunca emite tokens — resource server puro, valida o JWT emitido pelo OS Service com
o mesmo segredo simétrico compartilhado.
