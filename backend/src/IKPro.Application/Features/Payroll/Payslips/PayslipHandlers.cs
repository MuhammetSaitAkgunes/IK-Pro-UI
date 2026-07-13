using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using IKPro.Domain.Constants;
using IKPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Application.Features.Payroll.Payslips;

// --- çalışanın kendi bordroları ---

public sealed record GetMyPayslipsQuery : IRequest<IReadOnlyList<MyPayslipDto>>;

public sealed class GetMyPayslipsQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetMyPayslipsQuery, IReadOnlyList<MyPayslipDto>>
{
    public async Task<IReadOnlyList<MyPayslipDto>> Handle(
        GetMyPayslipsQuery request, CancellationToken cancellationToken)
    {
        var employeeId = currentUser.EmployeeId
            ?? throw new ForbiddenAccessException("Hesabınıza bağlı personel kaydı yok.");

        return await context.PayrollEmployees
            .AsNoTracking()
            .Where(pe => pe.EmployeeId == employeeId && pe.Result != null)
            .OrderByDescending(pe => pe.Period!.Year).ThenByDescending(pe => pe.Period!.Month)
            .Select(pe => new MyPayslipDto(
                pe.PayrollPeriodId,
                pe.Id,
                pe.Period!.Name,
                pe.Result!.GrossEarnings,
                pe.Result.TotalDeductions,
                pe.Result.NetPay,
                pe.ApprovalStatus == PayrollApprovalStatus.Approved ? "Onaylandı" : "Kontrol"))
            .ToListAsync(cancellationToken);
    }
}

// --- bordro pusulası PDF ---

/// <summary>QuestPDF pusula üretici girdisi — payroll.js slip-paper alanları.</summary>
public sealed record PayslipModel(
    string PeriodName,
    string EmployeeName,
    string Title,
    string Department,
    int WorkedDays,
    decimal OvertimeHours,
    decimal OvertimePay,
    decimal BaseGross,
    decimal PremiumPay,
    decimal RoadAllowance,
    decimal MealAllowance,
    decimal BenefitPay,
    decimal GrossEarnings,
    decimal SgkEmployee,
    decimal UnemploymentEmployee,
    decimal IncomeTax,
    decimal StampTax,
    decimal SpecialDeductions,
    decimal TotalDeductions,
    decimal NetPay);

/// <summary>Bordro pusulası PDF üretimi (Infrastructure: QuestPDF).</summary>
public interface IPayslipGenerator
{
    byte[] Generate(PayslipModel model);
}

/// <summary>
/// Pusula PDF'i. hr-admin herkesinkini, çalışan yalnız kendisininkini indirebilir;
/// sonuç (onay snapshot'ı) olmayan satır için 409.
/// </summary>
public sealed record GetPayslipPdfQuery(int PeriodId, int RowId)
    : IRequest<(byte[] Content, string FileName)>;

public sealed class GetPayslipPdfQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IPayslipGenerator generator)
    : IRequestHandler<GetPayslipPdfQuery, (byte[] Content, string FileName)>
{
    public async Task<(byte[] Content, string FileName)> Handle(
        GetPayslipPdfQuery request, CancellationToken cancellationToken)
    {
        var row = await context.PayrollEmployees
            .AsNoTracking()
            .Include(pe => pe.Period)
            .Include(pe => pe.Employee)!.ThenInclude(e => e!.Department)
            .Include(pe => pe.Result)
            .FirstOrDefaultAsync(
                pe => pe.Id == request.RowId && pe.PayrollPeriodId == request.PeriodId, cancellationToken)
            ?? throw new NotFoundException("Bordro satırı", request.RowId);

        if (!currentUser.Roles.Contains(Roles.HrAdmin) && row.EmployeeId != currentUser.EmployeeId)
        {
            throw new ForbiddenAccessException("Yalnız kendi bordro pusulanızı indirebilirsiniz.");
        }

        var result = row.Result
            ?? throw new ConflictException("Bu satır için onaylanmış bordro sonucu yok.");

        var model = new PayslipModel(
            row.Period!.Name,
            row.Employee!.FullName,
            row.Employee.Title,
            row.Employee.Department?.Name ?? string.Empty,
            row.WorkedDays,
            row.OvertimeHours,
            result.OvertimePay,
            result.BaseGross,
            row.PremiumPay,
            row.RoadAllowance,
            row.MealAllowance,
            row.BenefitPay,
            result.GrossEarnings,
            result.SgkEmployee,
            result.UnemploymentEmployee,
            result.IncomeTax,
            result.StampTax,
            row.SpecialDeductions,
            result.TotalDeductions,
            result.NetPay);

        var fileName = $"bordro-{row.Period.Year}-{row.Period.Month:00}-{row.Employee.FullName
            .Replace(' ', '-').ToLowerInvariant()}.pdf";

        return (generator.Generate(model), fileName);
    }
}
