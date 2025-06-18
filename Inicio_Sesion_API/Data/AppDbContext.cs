using Inicio_Sesion_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Inicio_Sesion_API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Define DbSet properties for your entities
        public DbSet<Usuario> Usuario { get; set; }
        public DbSet<Administrador> Administrador { get; set; }
        public DbSet<Token> Token { get; set; }
        public DbSet<LogApi> LogApi { get; set; }
        public DbSet<LogSistema> LogSistema { get; set; }
        public DbSet<Configuracion> Configuracion { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Mapear Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(u => u.usuario_id);
                entity.ToTable("usuario");
            });

            // Mapear Administrador
            modelBuilder.Entity<Administrador>(entity =>
            {
                entity.HasKey(a => a.administrador_id);
                entity.ToTable("administrador");
            });

            // Mapear Token
            modelBuilder.Entity<Token>(entity =>
            {
                entity.HasKey(t => t.token_id);
                entity.ToTable("token");
            });

            // Mapear LogApi
            modelBuilder.Entity<LogApi>(entity =>
            {
                entity.HasKey(l => l.log_api_id);
                entity.ToTable("log_api");
            });

            // Mapear LogSistema
            modelBuilder.Entity<LogSistema>(entity =>
            {
                entity.HasKey(l => l.log_sistema_id);
                entity.ToTable("log_sistema");

                // Relación con Administrador
                entity.HasOne(l => l.Administrador)
                    .WithMany()
                    .HasForeignKey(l => l.administrador_id)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Mapear Configuracion
            modelBuilder.Entity<Configuracion>(entity =>
            {
                entity.HasKey(c => c.configuracion_id);
                entity.ToTable("configuracion");
            });
        }
    }
}