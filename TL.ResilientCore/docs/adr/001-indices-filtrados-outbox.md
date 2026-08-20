# ADR 001: Uso de Índices Filtrados na Tabela Outbox

## 1. Contexto

A arquitetura do `TL.ResilientCore` utiliza o **Outbox Pattern** para garantir a entrega de eventos de domínio (At-Least-Once Delivery). Sempre que uma transação de negócio ocorre, uma mensagem é salva na tabela `OutboxMessages` na mesma transação de banco de dados. 

Um *Background Worker* pesquisa continuamente essa tabela por mensagens não processadas para enviá-las ao Message Broker (Kafka/RabbitMQ). 
O problema é que a tabela `OutboxMessages` cresce infinitamente (ou requer jobs de limpeza complexos). Com o tempo, a query `SELECT * FROM OutboxMessages WHERE ProcessedOnUtc IS NULL` torna-se lenta (O(N)), consumindo recursos do banco (CPU/IO) e atrasando a entrega dos eventos.

## 2. Decisão

Decidimos implementar **Índices Filtrados (Filtered Indexes / Partial Indexes)** na configuração do Entity Framework Core para a entidade `OutboxMessage`.

O índice será criado **apenas** sobre as linhas onde `ProcessedOnUtc IS NULL`.

Exemplo de configuração no EF Core:
```csharp
builder.HasIndex(m => m.OccurredOnUtc)
       .HasFilter("\"ProcessedOnUtc\" IS NULL");
```

## 3. Consequências

### Pontos Positivos (Ganhos):
* **Performance Extrema:** O tempo de busca das mensagens pendentes cai drasticamente (de dezenas de milissegundos para `< 1ms`), independentemente do tamanho da tabela.
* **Economia de Espaço:** Como o índice armazena apenas mensagens não processadas, o tamanho do índice no disco é minúsculo e cabe inteiramente em memória RAM.
* **Menos Overhead:** Remove a urgência de criar rotinas pesadas de deleção/limpeza histórica (purge jobs), já que o volume de dados antigos não afeta o desempenho da query do worker.

### Pontos Negativos (Trade-offs):
* **Acoplamento de Sintaxe:** A sintaxe do `.HasFilter()` aceita um raw SQL que pode variar sutilmente entre PostgreSQL, SQL Server e Oracle. É necessário garantir que o filtro escrito seja compatível com o provedor de banco de dados escolhido pela fábrica de software.