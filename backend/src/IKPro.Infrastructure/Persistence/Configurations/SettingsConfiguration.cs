using IKPro.Domain.Entities.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IKPro.Infrastructure.Persistence.Configurations;

public class CompanyProfileConfiguration : IEntityTypeConfiguration<CompanyProfile>
{
    public void Configure(EntityTypeBuilder<CompanyProfile> b)
    {
        b.Property(c => c.Name).IsRequired().HasMaxLength(200);
        b.Property(c => c.Website).HasMaxLength(200);
        b.Property(c => c.SystemEmail).HasMaxLength(200);
        b.Property(c => c.Phone).HasMaxLength(32);
        b.Property(c => c.HeadquartersAddress).HasMaxLength(500);
        b.Property(c => c.LogoPath).HasMaxLength(500);
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.Property(s => s.Plan).IsRequired().HasMaxLength(64);
        b.Property(s => s.BillingCycle).HasMaxLength(32);
        b.Property(s => s.PaymentMethodMasked).HasMaxLength(64);
    }
}
