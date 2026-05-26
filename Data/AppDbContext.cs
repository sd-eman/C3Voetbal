using Microsoft.EntityFrameworkCore;
using C3Voetbal.Model;

namespace C3Voetbal.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Bet> Bets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
                "server=localhost;port=3306;user=root;password=;database=c3voetbal_app;",
                Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.21-mysql")
            );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bet>(entity =>
            {
                entity.ToTable("bets");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.GameId).HasColumnName("game_id");
                entity.Property(e => e.PredictedOutcome).HasColumnName("predicted_outcome");
                entity.Property(e => e.Won).HasColumnName("won");
            });
        }
    }
}