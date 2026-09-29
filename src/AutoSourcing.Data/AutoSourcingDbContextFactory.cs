using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoSourcing.Data;

// Used only by the EF Core tools (e.g. `dotnet ef migrations add`). Not used at runtime.
public class AutoSourcingDbContextFactory : IDesignTimeDbContextFactory<AutoSourcingDbContext>
{
    public AutoSourcingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutoSourcingDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=AutoSourcing;Trusted_Connection=True;TrustServerCertificate=True");
        return new AutoSourcingDbContext(optionsBuilder.Options);
    }
}
