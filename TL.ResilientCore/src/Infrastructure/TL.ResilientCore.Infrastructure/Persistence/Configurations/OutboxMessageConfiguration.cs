using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TL.ResilientCore.Infrastructure.Outbox;

namespace TL.ResilientCore.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.OccurredOnUtc)
               .HasFilter("\"ProcessedOnUtc\" IS NULL");
    }
}