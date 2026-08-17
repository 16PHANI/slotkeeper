using Microsoft.EntityFrameworkCore;
using SlotKeeper.Domain.Entities;

namespace SlotKeeper.Infrastructure;

public class SlotKeeperDbContext : DbContext
{
    public SlotKeeperDbContext(DbContextOptions<SlotKeeperDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSlot> BookingSlots => Set<BookingSlot>();
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Resource>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Name).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.RowVersion).IsConcurrencyToken();

            entity.HasIndex(b => new { b.ResourceId, b.StartUtc, b.EndUtc });
            entity.HasIndex(b => new { b.UserId, b.StartUtc });

            entity.HasOne(b => b.Resource)
                .WithMany()
                .HasForeignKey(b => b.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(b => b.Slots)
                .WithOne(s => s.Booking)
                .HasForeignKey(s => s.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingSlot>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.ResourceId, s.SlotStartUtc }).IsUnique();
        });

        modelBuilder.Entity<WaitlistEntry>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => new { w.ResourceId, w.Status });
        });

        modelBuilder.Entity<AuditLogEntry>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.TimestampUtc);
        });
    }
}
