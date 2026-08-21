using EventGathera.Bookings.Domain.Entities;
using EventGathera.Bookings.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventGathera.Bookings.Infrastructure.DataAccess.Configurations;

public class OutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(o => o.PublishedAt)
            .IsRequired(false);

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasDefaultValue(OutboxStatus.Pending);

        builder.HasIndex(o => new { o.Status, o.CreatedAt });

        builder.HasIndex(o => o.CreatedAt);
    }
}
