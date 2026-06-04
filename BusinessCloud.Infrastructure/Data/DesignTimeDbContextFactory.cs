using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BusinessCloud.Infrastructure.Data;

public class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    public IdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../BusinessCloud.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<IdentityDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("PaymentsConnection"));
        return new IdentityDbContext(optionsBuilder.Options);
    }
}

public class BazaresDbContextFactory : IDesignTimeDbContextFactory<BazaresDbContext>
{
    public BazaresDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../BusinessCloud.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<BazaresDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("BazaresConnection"));
        return new BazaresDbContext(optionsBuilder.Options, new DummyCurrentUserService());
    }
}
