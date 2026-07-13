namespace IKPro.Domain.ReadModels;

/// <summary>
/// İzin bakiyesi okuma modeli — vw_LeaveBalanceSummary SQL view'ına eşlenir.
/// Kullanılan gün, onaylı ve yıllık bakiyeden düşen taleplerden canlı hesaplanır.
/// </summary>
public class LeaveBalanceSummary
{
    public int EmployeeId { get; set; }
    public int Year { get; set; }
    public int EntitledDays { get; set; }
    public int CarriedOverDays { get; set; }
    public int UsedDays { get; set; }
    public int RemainingDays { get; set; }
}
