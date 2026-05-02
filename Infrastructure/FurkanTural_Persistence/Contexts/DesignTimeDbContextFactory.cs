using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FurkanTural_Persistence.Contexts;

// Used only by EF Core CLI tools (dotnet ef migrations add / update).
// Never instantiated at runtime — connection string is injected via appsettings in production.
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<FurkanTuralDbContext>
{
    public FurkanTuralDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<FurkanTuralDbContext>()
            .UseSqlServer(
                "Server = localhost; Database = FurkanTural_Dev; User ID = furkan.tural; Password = ^m88*Q; Trusted_Connection = True; TrustServerCertificate=True;",
                sql => sql.MigrationsAssembly(typeof(FurkanTuralDbContext).Assembly.FullName))
            .Options;

        return new FurkanTuralDbContext(options);
    }
}
