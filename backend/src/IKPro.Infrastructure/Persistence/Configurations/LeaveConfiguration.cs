using IKPro.Domain.Entities.Leaves;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IKPro.Infrastructure.Persistence.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        // Audit trigger'lı tablo: EF'in OUTPUT-clause tabanlı kaydetme stratejisiyle çakışmasın.
        b.ToTable(tb => tb.HasTrigger("TR_LeaveRequests_Audit"));

        b.Property(l => l.Description).HasMaxLength(1000);
        b.Property(l => l.DecisionNote).HasMaxLength(1000);
        b.Property(l => l.DecisionByUserId).HasMaxLength(450);

        b.HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(l => l.SubstituteEmployee)
            .WithMany()
            .HasForeignKey(l => l.SubstituteEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(l => l.LeaveType)
            .WithMany(t => t.Requests)
            .HasForeignKey(l => l.LeaveTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(l => new { l.EmployeeId, l.Status });
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> b)
    {
        b.Property(h => h.Name).IsRequired().HasMaxLength(200);
        // Multi-tenant: tatil benzersizliği kiracı başına (iki şirket aynı tarihi tutabilir).
        b.HasIndex(h => new { h.TenantId, h.Date }).IsUnique();
    }
}

public class LeaveBalanceSummaryConfiguration : IEntityTypeConfiguration<Domain.ReadModels.LeaveBalanceSummary>
{
    public void Configure(EntityTypeBuilder<Domain.ReadModels.LeaveBalanceSummary> b)
    {
        // Salt-okunur view eşlemesi; migration'larda tablo üretilmez.
        b.HasNoKey().ToView("vw_LeaveBalanceSummary");
    }
}

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> b)
    {
        b.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.EmployeeId, x.Year }).IsUnique();
    }
}
