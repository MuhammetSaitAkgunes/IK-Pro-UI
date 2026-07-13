using IKPro.Domain.Common;

namespace IKPro.Domain.Entities.Settings;

/// <summary>Abonelik / faturalama bilgisi (tekil kayıt).</summary>
public class Subscription : AuditableEntity
{
    /// <summary>Plan rozeti (settings.js: "PRO").</summary>
    public string Plan { get; set; } = string.Empty;

    /// <summary>Plan görünen adı (settings.js: "HR Master Kurumsal").</summary>
    public string? PlanName { get; set; }
    public string? BillingCycle { get; set; }
    public decimal Price { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public string? PaymentMethodMasked { get; set; }
}
