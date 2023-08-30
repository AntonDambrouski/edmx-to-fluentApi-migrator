using System.Data.Entity.Core.Metadata.Edm;

namespace FluentApiMigrator.Models;

public class ForeignKeyInfo
{
    public string NavPropertyNameFrom { get; set; }
    public OperationAction DeleteBehaviorFrom { get; set; }
    public OperationAction DeleteBehaviorTo { get; set; }
    public RelationshipMultiplicity RelationshipFrom { get; set; }
    public RelationshipMultiplicity RelationshipTo { get; set; }
    public string NavPropertyNameTo { get; set; }
    public string EntitySetNameFrom { get; set; }
    public string EntitySetNameTo { get; set; }
    public bool IsManyToMany => JoinTableName != null;
    public string JoinTableName { get; set; }
    public string JoinTableKeyFrom { get; set; }
    public string JoinTableKeyTo { get; set; }
    public HashSet<string> ForeignKeys { get; set; } = new HashSet<string>();
}
