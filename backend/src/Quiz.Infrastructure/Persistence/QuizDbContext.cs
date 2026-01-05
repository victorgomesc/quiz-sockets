using Microsoft.EntityFrameworkCore;
using Quiz.Domain.Entities;

namespace Quiz.Infrastructure.Persistence;

public sealed class QuizDbContext : DbContext
{
    public QuizDbContext(DbContextOptions<QuizDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<MatchParticipant> MatchParticipants => Set<MatchParticipant>();
    public DbSet<UserStats> UserStats => Set<UserStats>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Users
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(50).IsRequired();
            e.Property(x => x.Email).HasMaxLength(120).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();

            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Username).IsUnique();
        });

        // Matches
        modelBuilder.Entity<Match>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RoomCode).HasMaxLength(16).IsRequired();

            e.HasMany(x => x.Participants)
             .WithOne()
             .HasForeignKey(p => p.MatchId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.StartedAtUtc);
        });

        // MatchParticipants
        modelBuilder.Entity<MatchParticipant>(e =>
        {
            e.HasKey(x => new { x.MatchId, x.UserId });
            e.Property(x => x.Score).IsRequired();
        });

        // UserStats (Ranking)
        modelBuilder.Entity<UserStats>(e =>
        {
            e.HasKey(x => x.UserId);
            e.Property(x => x.TotalScore).IsRequired();
            e.Property(x => x.MatchesPlayed).IsRequired();
            e.Property(x => x.Wins).IsRequired();
            e.Property(x => x.UpdatedAtUtc).IsRequired();
        });
    }
}
