using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsEnvio.Modules.Tenancy.Domain;

namespace WhatsEnvio.Modules.Tenancy.Infrastructure.Mapping;

public class TenantMap : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.Name).IsRequired().HasMaxLength(100);
        builder.Property(t => t.TimeZoneId).IsRequired().HasMaxLength(50);
        builder.Property(t => t.CreatedAtUtc).IsRequired();
    }
}
