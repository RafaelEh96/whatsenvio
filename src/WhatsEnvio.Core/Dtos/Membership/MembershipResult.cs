using WhatsEnvio.Core.Enums;

namespace WhatsEnvio.Core.Dtos.Membership;

public record MembershipResult(
    Guid TenantId,
    Guid UserId,
    MembershipRole Role);
