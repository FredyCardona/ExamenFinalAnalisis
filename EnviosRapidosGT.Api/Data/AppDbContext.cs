using EnviosRapidosGT.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace EnviosRapidosGT.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Envio> Envios => Set<Envio>();
    public DbSet<HistorialEnvio> HistorialEnvios => Set<HistorialEnvio>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Envio>()
            .HasIndex(e => e.CodigoRastreo)
            .IsUnique();

        modelBuilder.Entity<Envio>()
            .Property(e => e.EstadoActual)
            .HasConversion<string>();

        modelBuilder.Entity<HistorialEnvio>()
            .Property(h => h.Estado)
            .HasConversion<string>();

        modelBuilder.Entity<HistorialEnvio>()
            .HasOne(h => h.Envio)
            .WithMany(e => e.Historial)
            .HasForeignKey(h => h.EnvioId);
    }
}
