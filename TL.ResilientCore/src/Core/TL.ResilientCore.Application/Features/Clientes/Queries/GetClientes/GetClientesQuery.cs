using MediatR;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Application.Features.Clientes.Queries.GetClientes;

/// <summary>
/// DTO de resposta para listagem simplificada de clientes.
/// </summary>
/// <param name="Id">Identificador do cliente.</param>
/// <param name="Nome">Nome do cliente.</param>
/// <param name="Email">E-mail do cliente.</param>
/// <param name="Ativo">Status de ativação.</param>
public record ClienteResponse(Guid Id, string Nome, string Email, bool Ativo);

/// <summary>
/// Query CQRS para consulta paginada e filtrada de clientes.
/// </summary>
/// <param name="Nome">Filtro opcional por nome parcial.</param>
/// <param name="Ativo">Filtro opcional por status.</param>
/// <param name="PageNumber">Número da página (padrão 1).</param>
/// <param name="PageSize">Quantidade de registros por página (padrão 10).</param>
public record GetClientesQuery(
    string? Nome = null, 
    bool? Ativo = null, 
    int PageNumber = 1, 
    int PageSize = 10
) : IRequest<Result<IReadOnlyList<ClienteResponse>>>;