namespace WhatsEnvio.ArchitectureTests.Rules;

public class ProjectReferenceRule
{
    public static readonly Dictionary<string, string[]> AllowedReferences = new()
    {
        ["WhatsEnvio.Core"] = [],
        ["WhatsEnvio.Modules.Identity"] = ["WhatsEnvio.Core"],
        ["WhatsEnvio.Modules.Tenancy"] = ["WhatsEnvio.Core"],
        ["WhatsEnvio.Api"] = ["WhatsEnvio.Modules.Identity", "WhatsEnvio.Modules.Tenancy"],
        ["WhatsEnvio.Worker"] = ["WhatsEnvio.Modules.Tenancy"],
    };
}
