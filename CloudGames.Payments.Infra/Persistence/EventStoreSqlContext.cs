using CloudGames.Payments.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace CloudGames.Payments.Infra.Persistence;

public class EventStoreSqlContext : DbContext
{
    public EventStoreSqlContext(DbContextOptions<EventStoreSqlContext> options) : base(options) { }

    public DbSet<StoredEvent> StoredEvents => Set<StoredEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StoredEvent>(b =>
        {
            b.ToTable("StoredEvents");
            b.HasKey(x => x.Id);
            b.Property(x => x.AggregateId).IsRequired();
            b.Property(x => x.Type).IsRequired().HasMaxLength(128);
            b.Property(x => x.Data).IsRequired();
            b.Property(x => x.OccurredOn).IsRequired();
            b.Property(x => x.Metadata);
        });
    }
}
