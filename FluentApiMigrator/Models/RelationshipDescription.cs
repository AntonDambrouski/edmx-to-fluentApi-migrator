using System.Data.Entity.Core.Metadata.Edm;

namespace FluentApiMigrator.Models;

public class RelationshipDescription
{
    public RelationshipEntityDescription From { get; set; }
    public RelationshipEntityDescription To { get; set; }
    public OperationAction DeleteBehavior { get; set; }
    public List<string> ForeignKeys { get; set; }
    public string JoinTableName { get; set; }
    public bool IsManyToMany => From?.RelationshipType == RelationshipMultiplicity.Many && To?.RelationshipType == RelationshipMultiplicity.Many;
    public bool IsZeroOrOneToOne => From?.RelationshipType == RelationshipMultiplicity.ZeroOrOne && To?.RelationshipType == RelationshipMultiplicity.One;
}

public class RelationshipEntityDescription
{
    public string EntityName { get; set; }
    public string NavigationPropertyName { get; set; }
    public RelationshipMultiplicity RelationshipType { get; set; }
    public string JoinKeyName { get; set; }
}
