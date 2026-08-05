using FluentValidation;
using IKPro.Application.Common.Exceptions;
using IKPro.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PayrollSettingsEntity = IKPro.Domain.Entities.Payroll.PayrollSettings;

namespace IKPro.Application.Features.Payroll.Settings;

public static class PayrollSettingsResolver
{
    /// <summary>
    /// Verilen tarihte YÜRÜRLÜKTE olan ayar seti: yürürlük tarihi o tarihi geçmeyen
    /// setlerin EN YENİSİ. Böylece yıl ortası değişimlerde (temmuz asgari ücret)
    /// haziran dönemi eski seti, temmuz dönemi yeni seti kullanır.
    /// Tarih verilmezse bugün esas alınır. Hiçbir set o tarihten önce başlamıyorsa
    /// varsayılan set, o da yoksa en eski set kullanılır.
    /// </summary>
    public static async Task<PayrollSettingsEntity> ResolveAsync(
        IApplicationDbContext context, DateOnly? asOf, CancellationToken cancellationToken)
    {
        var query = context.PayrollSettings.Include(s => s.TaxBrackets);
        var effectiveDate = asOf ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var settings = await query
            .Where(s => s.EffectiveFrom <= effectiveDate)
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        settings ??= await query.FirstOrDefaultAsync(s => s.IsDefault, cancellationToken)
            ?? await query.OrderBy(s => s.EffectiveFrom).FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Bordro ayarları", effectiveDate.ToString("yyyy-MM-dd"));

        return settings;
    }

    public static PayrollSettingsDto ToDto(PayrollSettingsEntity s) => new(
        s.EffectiveFrom, s.OvertimeMultiplier, s.MonthlyWorkingHours, s.DefaultWorkedDays,
        s.SgkEmployeeRate, s.UnemploymentEmployeeRate, s.SgkEmployerRate, s.UnemploymentEmployerRate,
        s.StampTaxRate, s.SgkBaseMin, s.SgkBaseMax,
        s.MonthlyMinWageIncomeTaxExemption, s.MonthlyMinWageStampTaxExemption, s.MinWageGross,
        s.TaxBrackets.OrderBy(b => b.Order)
            .Select(b => new TaxBracketDto(b.Order, b.Limit, b.Base, b.BaseTax, b.Rate))
            .ToList());
}

// --- görüntüle ---

public sealed record GetPayrollSettingsQuery(DateOnly? AsOf = null) : IRequest<PayrollSettingsDto>;

public sealed class GetPayrollSettingsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPayrollSettingsQuery, PayrollSettingsDto>
{
    public async Task<PayrollSettingsDto> Handle(
        GetPayrollSettingsQuery request, CancellationToken cancellationToken)
        => PayrollSettingsResolver.ToDto(
            await PayrollSettingsResolver.ResolveAsync(context, request.AsOf, cancellationToken));
}

// --- güncelle (yürürlük tarihine göre versiyonlu upsert) ---

public sealed record UpdatePayrollSettingsCommand(
    DateOnly EffectiveFrom,
    decimal OvertimeMultiplier,
    decimal MonthlyWorkingHours,
    int DefaultWorkedDays,
    decimal SgkEmployeeRate,
    decimal UnemploymentEmployeeRate,
    decimal SgkEmployerRate,
    decimal UnemploymentEmployerRate,
    decimal StampTaxRate,
    decimal SgkBaseMin,
    decimal SgkBaseMax,
    decimal MonthlyMinWageIncomeTaxExemption,
    decimal MonthlyMinWageStampTaxExemption,
    decimal MinWageGross,
    IReadOnlyList<TaxBracketDto>? TaxBrackets = null) : IRequest<PayrollSettingsDto>;

public sealed class UpdatePayrollSettingsCommandValidator : AbstractValidator<UpdatePayrollSettingsCommand>
{
    public UpdatePayrollSettingsCommandValidator()
    {
        RuleFor(x => x.EffectiveFrom)
            .Must(d => d.Year is >= 2000 and <= 2100)
            .WithMessage("Yürürlük tarihi 2000-2100 aralığında olmalı.");
        RuleFor(x => x.OvertimeMultiplier).InclusiveBetween(1m, 5m);
        RuleFor(x => x.MonthlyWorkingHours).GreaterThan(0m);
        RuleFor(x => x.DefaultWorkedDays).InclusiveBetween(1, 31);
        RuleFor(x => x.SgkEmployeeRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.UnemploymentEmployeeRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.SgkEmployerRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.UnemploymentEmployerRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.StampTaxRate).InclusiveBetween(0m, 100m);
        RuleFor(x => x.SgkBaseMax)
            .GreaterThan(x => x.SgkBaseMin)
            .WithMessage("SGK üst sınır alt sınırdan büyük olmalı.");

        When(x => x.TaxBrackets is not null, () =>
            RuleFor(x => x.TaxBrackets)
                .Must(b => b!.Count > 0 && b[^1].Limit is null)
                .WithMessage("Vergi dilimleri boş olamaz ve son dilim sınırsız (limit=null) olmalı."));
    }
}

public sealed class UpdatePayrollSettingsCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdatePayrollSettingsCommand, PayrollSettingsDto>
{
    public async Task<PayrollSettingsDto> Handle(
        UpdatePayrollSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = await context.PayrollSettings
            .Include(s => s.TaxBrackets)
            .FirstOrDefaultAsync(s => s.EffectiveFrom == request.EffectiveFrom, cancellationToken);

        if (settings is null)
        {
            settings = new PayrollSettingsEntity { EffectiveFrom = request.EffectiveFrom };
            context.PayrollSettings.Add(settings);
        }

        settings.OvertimeMultiplier = request.OvertimeMultiplier;
        settings.MonthlyWorkingHours = request.MonthlyWorkingHours;
        settings.DefaultWorkedDays = request.DefaultWorkedDays;
        settings.SgkEmployeeRate = request.SgkEmployeeRate;
        settings.UnemploymentEmployeeRate = request.UnemploymentEmployeeRate;
        settings.SgkEmployerRate = request.SgkEmployerRate;
        settings.UnemploymentEmployerRate = request.UnemploymentEmployerRate;
        settings.StampTaxRate = request.StampTaxRate;
        settings.SgkBaseMin = request.SgkBaseMin;
        settings.SgkBaseMax = request.SgkBaseMax;
        settings.MonthlyMinWageIncomeTaxExemption = request.MonthlyMinWageIncomeTaxExemption;
        settings.MonthlyMinWageStampTaxExemption = request.MonthlyMinWageStampTaxExemption;
        settings.MinWageGross = request.MinWageGross;

        if (request.TaxBrackets is not null)
        {
            settings.TaxBrackets.Clear();
            var order = 1;
            foreach (var bracket in request.TaxBrackets)
            {
                settings.TaxBrackets.Add(new Domain.Entities.Payroll.IncomeTaxBracket
                {
                    Order = order++,
                    Limit = bracket.Limit,
                    Base = bracket.Base,
                    BaseTax = bracket.BaseTax,
                    Rate = bracket.Rate,
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return PayrollSettingsResolver.ToDto(settings);
    }
}
