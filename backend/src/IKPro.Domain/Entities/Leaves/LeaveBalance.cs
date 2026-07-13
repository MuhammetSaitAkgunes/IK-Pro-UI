using IKPro.Domain.Common;
using IKPro.Domain.Entities.Organization;

namespace IKPro.Domain.Entities.Leaves;

/// <summary>
/// Çalışanın yıl bazlı yıllık izin hak edişi. Kullanılan/kalan gün, onaylı izinlerden
/// SQL view ile hesaplanır (Faz 4); burada hak ediş (entitlement) saklanır.
/// </summary>
public class LeaveBalance : AuditableEntity
{
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public int Year { get; set; }
    public int EntitledDays { get; set; }

    /// <summary>Devreden gün (önceki yıldan).</summary>
    public int CarriedOverDays { get; set; }
}
