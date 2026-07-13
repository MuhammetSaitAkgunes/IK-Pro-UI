using IKPro.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IKPro.Infrastructure.Persistence;

/// <summary>dbo.fn_WorkingDays SQL fonksiyonunu çağırır (tatil tablosu DB'de tek kaynak).</summary>
public sealed class SqlWorkingDayCalculator(AppDbContext context) : IWorkingDayCalculator
{
    public async Task<int> GetWorkingDaysAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken)
        => await context.Database
            .SqlQuery<int>($"SELECT dbo.fn_WorkingDays({start}, {end}) AS [Value]")
            .SingleAsync(cancellationToken);
}
