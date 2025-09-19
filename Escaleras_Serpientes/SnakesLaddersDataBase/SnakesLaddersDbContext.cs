using Escaleras_Serpientes.Entities;
using Microsoft.EntityFrameworkCore;

namespace Escaleras_Serpientes.SnakesLaddersDataBase
{
    public class SnakesLaddersDbContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseInMemoryDatabase(databaseName: "SnakesLaddersDataBase");
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<ResumePlayer> ResumePlayers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Room 1 - * Player
            modelBuilder.Entity<Player>()
                .HasOne(p => p.Room)
                .WithMany(r => r.Players)
                .HasForeignKey(p => p.RoomId);

            // Room 1 - 1 Resume (Room is principal)
            modelBuilder.Entity<Resume>()
                .HasOne(r => r.Room)
                .WithOne(room => room.Resume)
                .HasForeignKey<Resume>(r => r.RoomId);

            // Resume 1 - * ResumePlayer
            modelBuilder.Entity<ResumePlayer>()
                .HasOne(rp => rp.Resume)
                .WithMany()
                .HasForeignKey(rp => rp.ResumeId);

            // Player 1 - * ResumePlayer
            modelBuilder.Entity<ResumePlayer>()
                .HasKey(cs => cs.Id);

            modelBuilder.Entity<ResumePlayer>()
                .Property(cs => cs.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<ResumePlayer>()
                .HasOne(rp => rp.Player)
                .WithMany(p => p.ResumePlayers)
                .HasForeignKey(rp => rp.PlayerId);

            modelBuilder.Entity<ResumePlayer>()
                .HasOne(rp => rp.Resume)
                .WithMany(r => r.ResumePlayers)
                .HasForeignKey(rp => rp.ResumeId);

        }

    }
}
