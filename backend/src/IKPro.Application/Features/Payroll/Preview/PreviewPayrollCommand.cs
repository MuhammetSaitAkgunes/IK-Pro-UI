using FluentValidation;
using IKPro.Application.Common.Interfaces;
using IKPro.Application.Features.Payroll.Settings;
using IKPro.Domain.Services;
using MediatR;

namespace IKPro.Application.Features.Payroll.Preview;

/// <summary>
/// Tekil bordro hesabı (payroll.js calculateSinglePayrollScenario) — kalıcılaştırılmaz.
/// Çarpan verilirse dönem varsayılanının yerine geçer.
/// </summary>
public sealed record PreviewPayrollCommand(
    decimal GrossSalary,
    int WorkedDays,
    decimal OvertimeHours = 0m,
    decimal? OvertimeMultiplier = null,
    decimal PremiumPay = 0m,
    decimal RoadAllowance = 0m,
    decimal MealAllowance = 0m,
    decimal BenefitPay = 0m,
    decimal SpecialDeductions = 0m,
    decimal PreviousTaxBase = 0m,
    /// <summary>Hangi tarihte yürürlükteki parametrelerle hesaplansın (boşsa bugün).</summary>
    DateOnly? AsOf = null) : IRequest<PayrollCalculation>;

public sealed class PreviewPayrollCommandValidator : AbstractValidator<PreviewPayrollCommand>
{
    public PreviewPayrollCommandValidator()
    {
        RuleFor(x => x.GrossSalary).GreaterThan(0m);
        RuleFor(x => x.WorkedDays).InclusiveBetween(1, 31);
        RuleFor(x => x.OvertimeHours).InclusiveBetween(0m, 300m);
        RuleFor(x => x.OvertimeMultiplier).InclusiveBetween(1m, 5m).When(x => x.OvertimeMultiplier is not null);
        RuleFor(x => x.PreviousTaxBase).GreaterThanOrEqualTo(0m);
    }
}

public sealed class PreviewPayrollCommandHandler(IApplicationDbContext context, IPayrollEngine engine)
    : IRequestHandler<PreviewPayrollCommand, PayrollCalculation>
{
    public async Task<PayrollCalculation> Handle(
        PreviewPayrollCommand request, CancellationToken cancellationToken)
    {
        var settings = await PayrollSettingsResolver.ResolveAsync(context, request.AsOf, cancellationToken);

        var input = new PayrollInput(
            request.GrossSalary,
            request.WorkedDays,
            request.OvertimeHours,
            request.PremiumPay,
            request.RoadAllowance,
            request.MealAllowance,
            request.BenefitPay,
            request.SpecialDeductions,
            request.PreviousTaxBase,
            OvertimeMultiplierOverride: request.OvertimeMultiplier);

        return engine.Calculate(input, settings.ToEngineParameters());
    }
}
