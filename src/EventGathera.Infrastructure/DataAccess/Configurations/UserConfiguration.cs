using EventGathera.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventGathera.Infrastructure.DataAccess.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Role).HasConversion<string>();

        builder.Property(e => e.Login)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(u => u.Login)
            .IsUnique();
    }
}
