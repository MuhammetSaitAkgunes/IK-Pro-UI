using IKPro.Domain.Common;

namespace IKPro.Domain.Entities.Settings;

/// <summary>E-posta bildirim tercihleri (tekil kayıt). Bildirim tetikleyicilerini kontrol eder.</summary>
public class NotificationSettings : AuditableEntity
{
    public bool NewPersonnelEmail { get; set; } = true;
    public bool LeaveRequestEmail { get; set; } = true;
    public bool WeeklyReportEmail { get; set; }
    public bool TwoFactorSmsEnabled { get; set; }
}
