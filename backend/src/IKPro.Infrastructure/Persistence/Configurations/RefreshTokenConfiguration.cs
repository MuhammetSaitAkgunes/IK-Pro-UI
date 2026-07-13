using IKPro.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IKPro.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        // Token kolonunda ham değer değil SHA-256 hash saklanır (Base64, 44 karakter).
        b.Property(t => t.Token).IsRequired().HasMaxLength(256);
        b.Property(t => t.UserId).IsRequired().HasMaxLength(450);
        b.Ignore(t => t.IsActive);

        b.HasIndex(t => t.Token).IsUnique();

        b.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.Property(u => u.DisplayName).IsRequired().HasMaxLength(128);
        b.Property(u => u.Initials).HasMaxLength(8);
    }
}
