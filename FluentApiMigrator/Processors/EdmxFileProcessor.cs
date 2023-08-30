using FluentApiMigrator.Exceptions;
using FluentApiMigrator.Interfaces;
using FluentApiMigrator.Models;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Xml.Linq;

namespace FluentApiMigrator.Processors;

public class EdmxFileProcessor : IProcessor
{
    public void Process(ProcessorContext context)
    {
        if (!File.Exists(context.EdmxFilePath)) throw new ArgumentException("Specified file is not exist");

        if (Path.GetExtension(context.EdmxFilePath).ToLower() != ".edmx")
            throw new ArgumentException("File is not an edmx file.");

        context.EdmxParseResult = LoadAndParseEdmxFile(context.EdmxFilePath);
    }

    private EdmxParseResult LoadAndParseEdmxFile(string filePath)
    {
        var xdoc = XDocument.Load(filePath);

        // Get the StorageModels schema section
        var ssdlSection = xdoc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "StorageModels")
            ?.Descendants()
            ?.FirstOrDefault(e => e.Name.LocalName == "Schema") ?? throw new EdmxFileException("Could not load storage models schema section");

        // Get the ConceptualModels schema section
        var csdlSection = xdoc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "ConceptualModels")
            ?.Descendants()
            ?.FirstOrDefault(e => e.Name.LocalName == "Schema") ?? throw new EdmxFileException("Could not load conceptual models schema section");

        // Get the Mappings section
        var mappingsSection = xdoc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Mappings")
            ?.Descendants()
            ?.FirstOrDefault(e => e.Name.LocalName == "Mapping") ?? throw new EdmxFileException("Could not load mapping section");


        StorageMappingItemCollection mappingsCollection;
        using var csdlReader = csdlSection.CreateReader();
        using var ssdlReader = ssdlSection.CreateReader();
        using var mappingsReader = mappingsSection.CreateReader();
        {
            var ssdlCollection = StoreItemCollection.Create(new[] { ssdlReader }, null, null, out var ssdlErrors);
            if (ssdlErrors?.Any() ?? false)
            {
                throw new EdmxFileException("Errors occured when parsing ssdl section: " + string.Join(", ", ssdlErrors));
            }

            var csdlCollection = EdmItemCollection.Create(new[] { csdlReader }, null, out var csdlErrors);
            if (csdlErrors?.Any() ?? false)
            {
                throw new EdmxFileException("Errors occured when parsing csdl section: " + string.Join(", ", csdlErrors));
            }

            mappingsCollection = StorageMappingItemCollection.Create(csdlCollection, ssdlCollection, new[] { mappingsReader }, null, out var mappingsErrors);
            if (mappingsErrors?.Any() ?? false)
            {
                throw new EdmxFileException("Errors occured when parsing mappings section: " + string.Join(", ", mappingsErrors));
            }
        }

        return ParseMappingsCollection(mappingsCollection);
    }

    private EdmxParseResult ParseMappingsCollection(StorageMappingItemCollection mappingsCollection)
    {
        var entitySetMappings = mappingsCollection.GetItems<EntityContainerMapping>().SelectMany(m => m.EntitySetMappings).ToList();
        var relationshipDescriptions = GetRelationshipDescriptions(entitySetMappings);
        var tableColumnsDescriptions = GetTableColumnDescriptions(entitySetMappings);
        return new EdmxParseResult
        {
            EntitySetMappings = entitySetMappings,
            RelationshipDescriptions = relationshipDescriptions,
            TableColumnsDescriptions = tableColumnsDescriptions
        };
    }

    private Dictionary<string, List<TableColumnDescription>> GetTableColumnDescriptions(List<EntitySetMapping> entitySetMappings)
    {
        var entityColumnData = new Dictionary<string, List<TableColumnDescription?>>(); // Create a dictionary to store column descriptions for each entity type
        var entityTypeMappings = entitySetMappings.Select(m => m.EntityTypeMappings.First()).ToList(); // Get the EntityTypeMappings for all entity sets in the list

        // Loop through each EntityTypeMapping
        foreach (var entityTypeMapping in entityTypeMappings)
        {
            var fragment = entityTypeMapping.Fragments.First(); // Get the first Fragment for the EntityTypeMapping
            var storeEntityType = fragment.StoreEntitySet.ElementType; // Get the StoreEntityType (the corresponding entity type in the storage model)
            var propertyMapping = fragment.PropertyMappings.OfType<ScalarPropertyMapping>(); // Get the property mappings for scalar properties
            var entityType = entityTypeMapping.EntitySetMapping.EntitySet.ElementType; // Get the EntityType (the corresponding entity type in the conceptual model)
            var keyProps = storeEntityType.KeyProperties; // Get the key properties of the StoreEntityType

            // Map the properties from the StoreEntityType to TableColumnDescription objects
            entityColumnData[entityType.Name] = storeEntityType.Properties.Select((p, i) =>
            {
                var mappedProperty = propertyMapping.FirstOrDefault(pm => pm.Column.Name.Equals(p.Name))?.Property; // Find the corresponding mapped property in property mappings

                if (mappedProperty is null) return null; // Property not found in mapping

                // Create a TableColumnDescription object with property information
                return new TableColumnDescription
                {
                    IsPrimaryKey = keyProps.Any(k => k.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)),
                    IsFixedLength = p.IsFixedLength,
                    IsComputed = p.IsStoreGeneratedComputed,
                    IsIdentity = p.IsStoreGeneratedIdentity,
                    IsNullable = p.Nullable,
                    Precision = p.Precision,
                    IsUnicode = p.IsUnicode,
                    MaxLength = p.MaxLength,
                    Name = mappedProperty.Name,
                    Scale = p.Scale,
                    SqlType = p.TypeName,
                    DbName = p.Name,
                    ClrType = mappedProperty.TypeName
                };
            }).Where(c => c != null).ToList(); // Filter out null entries (unmapped properties)
        }

        return entityColumnData;
    }

    private Dictionary<string, List<RelationshipDescription>> GetRelationshipDescriptions(List<EntitySetMapping> entitySetMappings)
    {
        var foreignKeysInfo = GetForeignKeysInfo(entitySetMappings); // Get foreign keys information from entitySetMappings
        var relationshipData = new Dictionary<string, List<RelationshipDescription>>();

        // Loop through each entity set in entitySetMappings
        foreach (var entitySet in entitySetMappings.Select(e => e.EntitySet))
        {
            var entitySetName = entitySet.ElementType.Name;
            relationshipData[entitySetName] = new List<RelationshipDescription>(); // Create a list to store relationship descriptions for the current entity set

            // Get foreign key information related to the current entity set
            var entitySetFkInfo = foreignKeysInfo.Values
                .Where(fki => (fki.EntitySetNameFrom?.Equals(entitySetName) ?? false) || (fki.EntitySetNameTo?.Equals(entitySetName) ?? false));

            // Iterate through each foreign key information
            foreach (var fkInfo in entitySetFkInfo)
            {
                // Create a RelationshipDescription object to store relationship settings
                var relationshipSettings = new RelationshipDescription
                {
                    ForeignKeys = fkInfo.ForeignKeys.ToList(),
                    DeleteBehavior = fkInfo.DeleteBehaviorFrom == OperationAction.Cascade || fkInfo.DeleteBehaviorTo == OperationAction.Cascade
                        ? OperationAction.Cascade
                        : OperationAction.None,
                };

                // Check if the current entity set is the "From" or "To" entity set in the relationship
                if (entitySetName.Equals(fkInfo.EntitySetNameFrom))
                {
                    // Set relationship settings for the "From" side
                    relationshipSettings.From = new RelationshipEntityDescription
                    {
                        EntityName = fkInfo.EntitySetNameFrom,
                        NavigationPropertyName = fkInfo.NavPropertyNameFrom,
                        RelationshipType = fkInfo.RelationshipTo,
                        JoinKeyName = fkInfo.JoinTableKeyFrom
                    };

                    // Set relationship settings for the "To" side
                    relationshipSettings.To = new RelationshipEntityDescription
                    {
                        EntityName = fkInfo.EntitySetNameTo,
                        NavigationPropertyName = fkInfo.NavPropertyNameTo,
                        RelationshipType = fkInfo.RelationshipFrom,
                        JoinKeyName = fkInfo.JoinTableKeyTo
                    };
                }
                else if (entitySetName.Equals(fkInfo.EntitySetNameTo))
                {
                    // Set relationship settings for the "From" side (reverse)
                    relationshipSettings.From = new RelationshipEntityDescription
                    {
                        EntityName = fkInfo.EntitySetNameTo,
                        NavigationPropertyName = fkInfo.NavPropertyNameTo,
                        RelationshipType = fkInfo.RelationshipFrom,
                        JoinKeyName = fkInfo.JoinTableKeyTo
                    };

                    // Set relationship settings for the "To" side (reverse)
                    relationshipSettings.To = new RelationshipEntityDescription
                    {
                        EntityName = fkInfo.EntitySetNameFrom,
                        NavigationPropertyName = fkInfo.NavPropertyNameFrom,
                        RelationshipType = fkInfo.RelationshipTo,
                        JoinKeyName = fkInfo.JoinTableKeyFrom
                    };
                }

                // If it's a many-to-many relationship, set the join table name
                if (fkInfo.IsManyToMany)
                {
                    relationshipSettings.JoinTableName = fkInfo.JoinTableName;
                }
                
                relationshipData[entitySetName].Add(relationshipSettings); // Add the relationship settings to the list for the current entity set
            }
        }

        return relationshipData;
    }

    // This method extracts foreign key information from entitySetMappings
    private static Dictionary<string, ForeignKeyInfo> GetForeignKeysInfo(List<EntitySetMapping> entitySetMappings)
    {
        var joinTableMappings = entitySetMappings.FirstOrDefault()?.ContainerMapping?.AssociationSetMappings.ToList(); // Get join table mappings from entitySetMappings
        var result = new Dictionary<string, ForeignKeyInfo>(); // Create a dictionary to store foreign key information

        // Iterate through each entity set in entitySetMappings
        foreach (var entitySet in entitySetMappings.Select(e => e.EntitySet))
        {
            // Iterate through navigation properties of the current entity set
            foreach (var navProperty in entitySet.ElementType.NavigationProperties)
            {
                
                var relationshipType = (AssociationType)navProperty.RelationshipType; // Get the relationship type (association) of the navigation property

                // Create a ForeignKeyInfo object if it doesn't already exist in the result dictionary
                if (!result.TryGetValue(relationshipType.Name, out var fkInfo))
                {
                    fkInfo = new ForeignKeyInfo();
                }

                // Check if the constraint is null (many-to-many relationship)
                if (relationshipType.Constraint is null)
                {
                    
                    var joinTableMapping = joinTableMappings.First(i => i.AssociationSet.Name.Equals(relationshipType.Name)); // Get the join table mapping related to the current relationship
                    AddManyToManyFkInfo(fkInfo, entitySet, joinTableMapping, navProperty.Name);// Add foreign key information for many-to-many relationship
                }
                else
                {
                    AddFkInfo(fkInfo, entitySet, navProperty); // Add foreign key information for regular (one-to-many or many-to-one) relationship
                }

                
                result[relationshipType.Name] = fkInfo; // Store the foreign key information in the result dictionary
            }
        }

        return result;
    }

    // This method adds foreign key information for regular (one-to-many or many-to-one) relationship
    private static void AddFkInfo(ForeignKeyInfo fkInfo, EntitySet entitySet, NavigationProperty navProperty)
    {
        var constraint = (navProperty.RelationshipType as AssociationType).Constraint; // Get the constraint of the relationship

        // Iterate through foreign key properties in the constraint
        foreach (var fkKey in constraint.ToProperties.Select(p => p.Name))
        {
            fkInfo.ForeignKeys.Add(fkKey);
        }

        // Set relationship multiplicity and other information
        fkInfo.RelationshipFrom = constraint.FromRole.RelationshipMultiplicity;
        fkInfo.RelationshipTo = constraint.ToRole.RelationshipMultiplicity;

        var fromEndMemberName = navProperty.FromEndMember.Name;

        // Check if the current role is the "From" role or the "To" role
        if (constraint.FromRole.Name.Equals(fromEndMemberName))
        {
            fkInfo.NavPropertyNameFrom = navProperty.Name;
            fkInfo.DeleteBehaviorFrom = constraint.ToRole.DeleteBehavior;
            fkInfo.EntitySetNameFrom = entitySet.ElementType.Name;
        }
        else
        {
            fkInfo.NavPropertyNameTo = navProperty.Name;
            fkInfo.DeleteBehaviorTo = constraint.FromRole.DeleteBehavior;
            fkInfo.EntitySetNameTo = entitySet.ElementType.Name;
        }
    }

    // This method adds foreign key information for a many-to-many relationship
    private static void AddManyToManyFkInfo(ForeignKeyInfo fkInfo, EntitySet entitySet, AssociationSetMapping joinTableMapping, string navPropertyName)
    {
        // Get the mapping of the source and target ends of the association set mapping
        var sourceEndMapping = joinTableMapping.SourceEndMapping;
        var targetEndMapping = joinTableMapping.TargetEndMapping;
        fkInfo.JoinTableName = joinTableMapping.AssociationSet.Name; // Set the join table name in the ForeignKeyInfo object

        // Check if the source end of the association set mapping corresponds to the current entity set
        if (sourceEndMapping.AssociationEnd.Name.Equals(entitySet.ElementType.Name))
        {
            // Set the foreign key information for the "From" side of the relationship
            fkInfo.JoinTableKeyFrom = sourceEndMapping.PropertyMappings.First().Column.Name;
            fkInfo.RelationshipFrom = sourceEndMapping.AssociationEnd.RelationshipMultiplicity;
            fkInfo.JoinTableKeyTo = targetEndMapping.PropertyMappings.First().Column.Name;
            fkInfo.RelationshipTo = targetEndMapping.AssociationEnd.RelationshipMultiplicity;
            fkInfo.EntitySetNameFrom = entitySet.ElementType.Name;
            fkInfo.NavPropertyNameFrom = navPropertyName;
        }
        else
        {
            // Set the foreign key information for the "To" side of the relationship (reverse)
            fkInfo.JoinTableKeyTo = sourceEndMapping.PropertyMappings.First().Column.Name;
            fkInfo.RelationshipTo = sourceEndMapping.AssociationEnd.RelationshipMultiplicity;
            fkInfo.JoinTableKeyFrom = targetEndMapping.PropertyMappings.First().Column.Name;
            fkInfo.RelationshipFrom = targetEndMapping.AssociationEnd.RelationshipMultiplicity;
            fkInfo.NavPropertyNameTo = navPropertyName;
            fkInfo.EntitySetNameTo = entitySet.ElementType.Name;
        }
    }
}
