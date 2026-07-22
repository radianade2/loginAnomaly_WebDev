using LoginAnomaly.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoginAnomaly.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Username).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.PasswordHash).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
    }
}

public class LoginEventConfiguration : IEntityTypeConfiguration<LoginEvent>
{
    public void Configure(EntityTypeBuilder<LoginEvent> b)
    {
        b.ToTable("LoginEvents");
        b.HasKey(x => x.Id);

        b.Property(x => x.Username).HasMaxLength(100).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
        b.Property(x => x.DeviceFingerprint).HasMaxLength(200).IsRequired();
        b.Property(x => x.Decision).HasConversion<int>();

        // Relasi: User (1) -> LoginEvent (banyak). FK nullable.
        b.HasOne(x => x.User)
         .WithMany(u => u.LoginEvents)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.SetNull);   // user dihapus -> event tetap ada

        b.HasIndex(x => x.Username);
        b.HasIndex(x => x.IpAddress);
        b.HasIndex(x => x.TimestampUtc);
        b.HasIndex(x => new { x.Username, x.TimestampUtc });
    }
}

public class RuleHitConfiguration : IEntityTypeConfiguration<RuleHit>
{
    public void Configure(EntityTypeBuilder<RuleHit> b)
    {
        b.ToTable("RuleHits");
        b.HasKey(x => x.Id);
        b.Property(x => x.RuleName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Reason).HasMaxLength(300);

        // Relasi: LoginEvent (1) -> RuleHit (banyak)
        b.HasOne(x => x.LoginEvent)
         .WithMany(e => e.RuleHits)
         .HasForeignKey(x => x.LoginEventId)
         .OnDelete(DeleteBehavior.Cascade);   // event dihapus -> hit-nya ikut

        b.HasIndex(x => x.LoginEventId);
    }
}

public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> b)
    {
        b.ToTable("Alerts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Severity).HasConversion<int>();
        b.Property(x => x.Summary).HasMaxLength(300).IsRequired();

        // Relasi 1:0..1 -> FK unik
        b.HasOne(x => x.LoginEvent)
         .WithOne(e => e.Alert)
         .HasForeignKey<Alert>(x => x.LoginEventId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.LoginEventId).IsUnique();   // jamin maksimal 1 alert/event
        b.HasIndex(x => x.IsAcknowledged);
    }
}

public class KnownDeviceConfiguration : IEntityTypeConfiguration<KnownDevice>
{
    public void Configure(EntityTypeBuilder<KnownDevice> b)
    {
        b.ToTable("KnownDevices");
        b.HasKey(x => x.Id);
        b.Property(x => x.DeviceFingerprint).HasMaxLength(200).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);

        b.HasOne(x => x.User)
         .WithMany(u => u.KnownDevices)
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);

        // Satu device unik per user (cegah duplikat saat whitelist)
        b.HasIndex(x => new { x.UserId, x.DeviceFingerprint }).IsUnique();
    }
}