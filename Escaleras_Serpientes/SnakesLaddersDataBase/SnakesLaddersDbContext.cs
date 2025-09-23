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
        public DbSet<RoomPlayers> RoomPlayers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Room 1 - * Resume (Room is principal)
            modelBuilder.Entity<Room>()
                .HasMany(r => r.resumes)
                .WithOne(res => res.Room)
                .HasForeignKey(r => r.RoomId);

            // Resume 1 - * ResumePlayer
            modelBuilder.Entity<ResumePlayer>()
                .HasOne(rp => rp.Resume)
                .WithMany()
                .HasForeignKey(rp => rp.ResumeId);

            // Player * - * Resume (ResumePlayer)
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

            // Room * - * Player (RoomPlayers)
            modelBuilder.Entity<RoomPlayers>()
                .HasKey(cs => cs.Id);

            modelBuilder.Entity<RoomPlayers>()
                .Property(cs => cs.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<RoomPlayers>()
                .HasOne(rp => rp.Player)
                .WithMany(p => p.RoomPlayers)
                .HasForeignKey(rp => rp.PlayerId);

            modelBuilder.Entity<RoomPlayers>()
                .HasOne(rp => rp.Room)
                .WithMany(r => r.RoomPlayers)
                .HasForeignKey(rp => rp.RoomId);

        }

    }
}
