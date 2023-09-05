using FluentApiMigrator.Builders;
using FluentApiMigrator.Interfaces;
using FluentApiMigrator.Models;

namespace FluentApiMigrator.Processors;

public class FluentApiProcessor : IProcessor
{
    private readonly FluentApiBuilder _builder = new FluentApiBuilder();

    public void Process(ProcessorContext context)
    {
        if (!Directory.Exists(context.OutputDirectory))
        {
            Directory.CreateDirectory(context.OutputDirectory);
        }

        GenerateFluentApiFiles(context.EdmxParseResult, context.OutputDirectory);
    }

    private void GenerateFluentApiFiles(EdmxParseResult parseResult, string outputDirectory)
    {
        foreach (var entitySetMapping in parseResult.EntitySetMappings)
        {
            var entityName = entitySetMapping.EntitySet.ElementType.Name;
            var commonInfo = parseResult.CommonEntityInfos[entityName];
            _builder.AddDefaultUsings()
                .AddNamespace()
                .StartEntityConfiguration(entityName)
                .ToTable(commonInfo.Table, commonInfo.Schema);

            if (parseResult.TableColumnsDescriptions.TryGetValue(entityName, out var descriptions))
            {
                foreach( var description in descriptions) 
                {
                    _builder.AddProperty(description);
                }
            }

            if (parseResult.RelationshipDescriptions.TryGetValue(entityName, out var relationships))
            {
                _builder.AddEmptyLine();
                foreach (var relationship in relationships)
                {
                    _builder.AddRelationship(relationship, commonInfo.PrimaryKeys);
                }
            }
            
            _builder.EndEntityConfiguration();
            var generatedFileText = _builder.ToString();
            _builder.Clear();

            WriteGeneratedFile(entityName, outputDirectory, generatedFileText);
            
        }
    }

    private void WriteGeneratedFile(string entityName, string outputDirectory, string generatedFileText)
    {
        var filename = $"{entityName}Configuration.cs";
        var path = Path.Combine(outputDirectory, filename);
        File.WriteAllText(path, generatedFileText);
    }
}
