using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C3Voetbal.Model;
using Microsoft.EntityFrameworkCore;

namespace C3Voetbal.Data
{
    public class C3VoetbalDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet <Team> Teams { get; set; }
        public DbSet <Game> Games { get; set; }

        public DbSet<Bet> Bets { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(
               "server=localhost;" +                     // Server name
               "port=3306;" +                            // Server port
               "user=root;" +                     // Username
               "password=;" +                 // Password
               "database=c3schoolvoetbal;"      // Database name
               , Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.21-mysql") // Version
               );
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Game>(entity =>
            {
                entity.ToTable("games");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Team1Id).HasColumnName("team1_id");
                entity.Property(e => e.Team2Id).HasColumnName("team2_id");
                entity.Property(e => e.Team1Score).HasColumnName("team1_score");
                entity.Property(e => e.Team2Score).HasColumnName("team2_score");
                entity.Property(e => e.Field).HasColumnName("field");
                entity.Property(e => e.RefereeId).HasColumnName("referee_id");
                entity.Property(e => e.Time).HasColumnName("time");
            });

            modelBuilder.Entity<Team>(entity =>
            {
                entity.ToTable("teams");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Points).HasColumnName("points");
                entity.Property(e => e.UserId).HasColumnName("user_id");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Password).HasColumnName("password");
                entity.Property(e => e.IsAdmin).HasColumnName("is_admin");
                entity.Property(e => e.TeamId).HasColumnName("team_id");
            });



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
