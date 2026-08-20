# 🚀 TL.ResilientCore: Guia de Início Rápido (Getting Started)

Bem-vindo ao **TL.ResilientCore**. Este documento destina-se aos desenvolvedores que acabaram de gerar um novo projeto a partir deste template e precisam entender onde colocar as mãos primeiro e como a arquitetura funciona por baixo dos panos.

## 🛠️ 1. Ambiente Local 

Antes de escrever qualquer código, você precisa do ambiente de dados (Banco de Dados e Cache) rodando. Para garantir consistência entre as máquinas de todos os desenvolvedores, não instale bancos de dados manualmente na sua máquina. Use o Docker.

Na raiz do seu projeto recém-criado, rode:

```bash
docker-compose up -d
```

Isso subirá instantaneamente o PostgreSQL (Porta 5432) e o Redis (Porta 6379), perfeitamente configurados para receberem conexões da sua API.

## 🏗️ 2. Como criar uma nova funcionalidade (O Fluxo de Trabalho)
Este template usa Clean Architecture e CQRS rigorosamente. Para criar uma nova funcionalidade (ex: "Criar Cliente"), siga este fluxo de fora para dentro:

### Passo A: Domínio (src/Core/Domain)

1. Crie a entidade Cliente herdando de AggregateRoot (ou Entity).

2.  Centralize as regras de negócio nos métodos da entidade.

Regra de Ouro: NUNCA lance exceções (throw). Se algo falhar (ex: e-mail inválido), retorne um `Result.Failure(DomainErrors.EmailInvalido`). (Consulte a ADR-002 para mais detalhes).

### Passo B: Aplicação (src/Core/Application)
1. Crie o contrato de escrita herdando de `ICommand` (ex: `CreateClientCommand`).

2. Crie o caso de uso implementando `ICommandHandler`. Aqui você irá orquestrar o fluxo: injetar o repositório, chamar as regras da entidade e confirmar a transação.

3. Mágica da Validação: Crie um validator do FluentValidation (`CreateClientCommandValidator`). Você NÃO precisa validar isso manualmente no seu handler. O nosso `ValidationBehavior` interceptará o comando antes dele rodar, executará o FluentValidation e, se falhar, retornará o erro encapsulado no Result automaticamente!

### Passo C: Apresentação (src/Presentation/Api)
Crie o seu endpoint na Minimal API (no `Program.cs` ou usando pacotes como FastEndpoints).
Use o nosso método de extensão `ToHttpResult()` para converter instantaneamente o retorno do caso de uso em uma resposta HTTP padronizada (200 OK ou 400 Bad Request):

```c#
app.MapPost("/clientes", async (CreateClientCommand command, ISender sender) => 
{
    var result = await sender.Send(command);
    return result.ToHttpResult(); // Converte Result<T> em HTTP Response sem try/catch
});
```
## ⚙️ 3. Segredos da Infraestrutura (O que você precisa saber)
O código deste template possui proteções nativas contra falhas distribuídas. Entenda as engrenagens principais:

### O "Milagre" do UnitOfWork e da Outbox
Quando o seu `CommandHandler` chamar o `IUnitOfWork.SaveChangesAsync()`, duas coisas incríveis acontecem na camada de Infraestrutura, invisíveis para a sua regra de negócio:

1. Interceptor do EF Core: O `InsertOutboxMessagesInterceptor` varrerá sua entidade, extrairá todos os Domain Events que você gerou (`RaiseDomainEvent()`) e os salvará na tabela de Outbox na mesma transação do banco. Isso garante que nunca perderemos eventos.

2. Índice Filtrado (Performance Extrema): O nosso worker em background (`ProcessOutboxMessagesJob`) roda a cada 10 segundos buscando mensagens na Outbox. Graças à nossa configuração de Filtered Index no EF Core (`.HasFilter("\"ProcessedOnUtc\" IS NULL")`), essa busca na fila demora menos de 1 milissegundo, mesmo que a tabela tenha milhões de linhas antigas! (Consulte a ADR-001).