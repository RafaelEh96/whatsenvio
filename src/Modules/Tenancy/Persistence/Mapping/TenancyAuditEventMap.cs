using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsEnvio.Modules.Tenancy.Audit;

namespace WhatsEnvio.Modules.Tenancy.Persistence.Mapping;

public class TenancyAuditEventMap : IEntityTypeConfiguration<TenancyAuditEvent>
{
    public void Configure(EntityTypeBuilder<TenancyAuditEvent> builder)
    {
        builder.ToTable("tenancy_events", "audit");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Data)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(128)
            .IsRequired();
    }
}
