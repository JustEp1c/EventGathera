using EventGathera.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventGathera.Events.Infrastructure.DataAccess.Configurations;

public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(pm => pm.Id);

        builder.Property(pm => pm.MessageId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pm => pm.MessageType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pm => pm.ProcessedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.HasIndex(pm => new { pm.MessageId, pm.MessageType })
            .IsUnique();
    }
}
