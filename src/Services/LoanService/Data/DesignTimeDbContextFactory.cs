using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibHub.LoanService.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LoanDbContext>
{
    public LoanDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LoanDbContext>();

        var connectionString = "Server=localhost;Port=3306;Database=loan_db;User=libhub_user;Password=LibHub@Dev2025;";
        
        optionsBuilder.UseMySql(
            connectionString,
            ServerVersion.AutoDetect(connectionString)
        );

        return new LoanDbContext(optionsBuilder.Options);
    }
}

