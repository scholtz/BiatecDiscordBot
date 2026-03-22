using BiatecDiscordBot.Models;
using Microsoft.EntityFrameworkCore;

namespace BiatecDiscordBot.Data;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    public DbSet<DiscordUserEntity> DiscordUsers => Set<DiscordUserEntity>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();
    public DbSet<TrackedMessage> TrackedMessages => Set<TrackedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DiscordUserEntity>(entity =>
        {
            entity.HasIndex(e => new { e.DiscordId, e.GuildId }).IsUnique();
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasMany(q => q.Options)
                  .WithOne(o => o.Question)
                  .HasForeignKey(o => o.QuestionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<QuestionOption>(entity =>
        {
            entity.HasMany(o => o.UserAnswers)
                  .WithOne(a => a.QuestionOption)
                  .HasForeignKey(a => a.QuestionOptionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAnswer>(entity =>
        {
            entity.HasOne(a => a.DiscordUser)
                  .WithMany(u => u.Answers)
                  .HasForeignKey(a => a.DiscordUserEntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TrackedMessage>(entity =>
        {
            entity.HasIndex(e => e.MessageDiscordId);
            entity.HasIndex(e => e.AuthorDiscordId);
            entity.HasIndex(e => e.GuildId);
        });
    }
}
