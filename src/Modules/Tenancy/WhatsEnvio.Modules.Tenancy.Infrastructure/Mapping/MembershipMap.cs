using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WhatsEnvio.Modules.Tenancy.Domain;

namespace WhatsEnvio.Modules.Tenancy.Infrastructure.Mapping;

public class MembershipMap : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.UserId).IsRequired();

        builder.Property(m => m.Role)
            .IsRequired()
            .HasConversion<string>();

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId);

        builder.HasIndex(m => new { m.TenantId, m.UserId })
            .IsUnique()
            .HasDatabaseName("ux_memberships_tenant_id_user_id");
    }
}
