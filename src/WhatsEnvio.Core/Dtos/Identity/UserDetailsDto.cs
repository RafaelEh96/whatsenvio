using WhatsEnvio.Core.Dtos.Tenancy;

namespace WhatsEnvio.Core.Dtos.Identity;

public record UserDetailsDto(
    string Id,
    string Name,
    IReadOnlyList<TenantSummary> Tenants);
