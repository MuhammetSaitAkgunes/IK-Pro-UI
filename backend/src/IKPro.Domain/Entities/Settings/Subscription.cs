using IKPro.Domain.Common;

namespace IKPro.Domain.Entities.Settings;

/// <summary>Abonelik / faturalama bilgisi (tekil kayıt).</summary>
public class Subscription : AuditableEntity
{
    public string Plan { get; set; } = string.Empty;
    public string? BillingCycle { get; set; }
    public decimal Price { get; set; }
    public DateOnly? RenewalDate { get; set; }
    public string? PaymentMethodMasked { get; set; }
}
