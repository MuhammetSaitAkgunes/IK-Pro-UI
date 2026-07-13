namespace IKPro.Domain.Common;

/// <summary>
/// Base for all persisted entities with an integer identity key.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
