using LoginAnomaly.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoginAnomaly.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<LoginEvent> LoginEvents => Set<LoginEvent>();
    public DbSet<RuleHit> RuleHits => Set<RuleHit>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<KnownDevice> KnownDevices => Set<KnownDevice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}