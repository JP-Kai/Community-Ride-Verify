using Microsoft.EntityFrameworkCore;
using RideSafeSA.Api.Models;

namespace RideSafeSA.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Driver> Drivers => Set<Driver>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Driver>()
            .HasIndex(d => d.LicensePlateNormalized)
            .IsUnique();

        modelBuilder.Entity<Report>()
            .HasOne(r => r.Driver)
            .WithMany(d => d.Reports)
            .HasForeignKey(r => r.DriverId);
    }
}
