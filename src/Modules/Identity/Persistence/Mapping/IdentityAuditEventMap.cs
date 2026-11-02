using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsEnvio.Modules.Identity.Audit;

namespace WhatsEnvio.Modules.Identity.Persistence.Mapping;

public class IdentityAuditEventMap : IEntityTypeConfiguration<IdentityAuditEvent>
{
    public void Configure(EntityTypeBuilder<IdentityAuditEvent> builder)
    {
        builder.ToTable("identity_events", "audit");

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
