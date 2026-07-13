using IKPro.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IKPro.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.Property(d => d.Name).IsRequired().HasMaxLength(128);
        b.Property(d => d.Code).HasMaxLength(32);
        b.HasIndex(d => d.Name).IsUnique();
    }
}
