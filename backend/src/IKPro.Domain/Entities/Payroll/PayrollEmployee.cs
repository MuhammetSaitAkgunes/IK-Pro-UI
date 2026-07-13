using IKPro.Domain.Common;
using IKPro.Domain.Entities.Organization;
using IKPro.Domain.Enums;

namespace IKPro.Domain.Entities.Payroll;

/// <summary>
/// Bir dönemdeki bir çalışanın bordro girdisi. Brüt→net sonucu IPayrollEngine ile
/// hesaplanır (Faz 6); onaylandığında sonuç <see cref="PayrollResult"/> olarak saklanır.
/// </summary>
public class PayrollEmployee : AuditableEntity
{
    public int PayrollPeriodId { get; set; }
    public PayrollPeriod? Period { get; set; }

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public decimal GrossSalary { get; set; }
    public int WorkedDays { get; set; }
    public int OvertimeHours { get; set; }
    public decimal PremiumPay { get; set; }
    public decimal RoadAllowance { get; set; }
    public decimal MealAllowance { get; set; }
    public decimal BenefitPay { get; set; }
    public decimal SpecialDeductions { get; set; }

    /// <summary>Önceki dönemlere ait kümülatif gelir vergisi matrahı.</summary>
    public decimal PreviousTaxBase { get; set; }

    public bool IbanComplete { get; set; }
    public bool TimesheetComplete { get; set; }

    public PayrollApprovalStatus ApprovalStatus { get; set; } = PayrollApprovalStatus.PreCalc;
    public string? Notes { get; set; }

    public PayrollResult? Result { get; set; }
}
