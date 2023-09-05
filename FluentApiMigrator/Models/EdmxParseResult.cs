using System.Data.Entity.Core.Mapping;

namespace FluentApiMigrator.Models;

public class EdmxParseResult
{
    public List<EntitySetMapping> EntitySetMappings { get; set; }
    public Dictionary<string, List<RelationshipDescription?>> RelationshipDescriptions { get; set; } // Name of entity to relationship descriptions
    public Dictionary<string, List<TableColumnDescription?>> TableColumnsDescriptions { get; set; } // Name of entity to columns descriptions
    public Dictionary<string, CommonEntityInfo> CommonEntityInfos { get; set; } // Name of entity to common entity info
}
