using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibHub.CatalogService.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();

        var connectionString = "Server=localhost;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev2025;";
        
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString)
        );

        return new CatalogDbContext(optionsBuilder.Options);
    }
}

