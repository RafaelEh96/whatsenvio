using WhatsEnvio.Core.Dtos.Membership;
using WhatsEnvio.Core.Enums;

namespace WhatsEnvio.Core.Dtos.Tenancy;

public record TenantSummary(
    Guid Id,
    string Name,
    MembershipRole Role);
