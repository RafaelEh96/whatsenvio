namespace WhatsEnvio.ArchitectureTests.Rules;

public class ProjectReferenceRule
{
    public static readonly Dictionary<string, string[]> AllowedReferences = new()
    {
        ["WhatsEnvio.Core"] = [],
        ["WhatsEnvio.Modules.Tenancy.Contracts"] = [],
        ["WhatsEnvio.Modules.Tenancy.Domain"] = ["WhatsEnvio.Core"],
        ["WhatsEnvio.Modules.Tenancy.Application"] =
        [
            "WhatsEnvio.Core",
            "WhatsEnvio.Modules.Tenancy.Contracts",
            "WhatsEnvio.Modules.Tenancy.Domain",
        ],
        ["WhatsEnvio.Modules.Tenancy.Infrastructure"] =
        [
            "WhatsEnvio.Core",
            "WhatsEnvio.Modules.Tenancy.Application",
            "WhatsEnvio.Modules.Tenancy.Domain",
        ],
        ["WhatsEnvio.Api"] =
        [
            "WhatsEnvio.Modules.Tenancy.Contracts",
            "WhatsEnvio.Modules.Tenancy.Application",
            "WhatsEnvio.Modules.Tenancy.Infrastructure"
        ],
        ["WhatsEnvio.Worker"] =
        [
            "WhatsEnvio.Modules.Tenancy.Contracts",
            "WhatsEnvio.Modules.Tenancy.Application",
            "WhatsEnvio.Modules.Tenancy.Infrastructure"
        ],
    };
}
