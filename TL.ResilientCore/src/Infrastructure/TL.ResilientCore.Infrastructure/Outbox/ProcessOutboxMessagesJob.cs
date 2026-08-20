using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TL.ResilientCore.Application.Messaging;
using TL.ResilientCore.Domain.Primitives;
using TL.ResilientCore.Infrastructure.Persistence;

namespace TL.ResilientCore.Infrastructure.Outbox;

public class ProcessOutboxMessagesJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessOutboxMessagesJob> _logger;

    public ProcessOutboxMessagesJob(IServiceProvider serviceProvider, ILogger<ProcessOutboxMessagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessMessagesAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro catastrófico no processamento da Outbox.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

        }
    }

    private async Task ProcessMessagesAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.OccurredOnUtc)
            .Take(20)
            .ToListAsync(stoppingToken);

        if (!messages.Any()) return;

        foreach (var message in messages)
        {
            try
            {
                var domainEventType = Type.GetType(message.Type);
                
                if (domainEventType is null)
                {
                    _logger.LogWarning("Tipo do evento não encontrado: {Type}", message.Type);
                    continue;
                }

                var domainEvent = JsonSerializer.Deserialize(message.Content, domainEventType) as IDomainEvent;

                if (domainEvent is null) continue;

                var notificationType = typeof(DomainEventNotification<>)
                    .MakeGenericType(domainEventType);

                var notification = Activator.CreateInstance(notificationType, domainEvent) as INotification;

                await publisher.Publish(notification!, stoppingToken);

                message.ProcessedOnUtc = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem outbox {Id}", message.Id);
                message.Error = ex.Message;
            }
        }

        await dbContext.SaveChangesAsync(stoppingToken);
    }
}