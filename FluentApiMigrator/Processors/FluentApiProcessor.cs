using FluentApiMigrator.Builders;
using FluentApiMigrator.Interfaces;
using FluentApiMigrator.Models;

namespace FluentApiMigrator.Processors;

public class FluentApiProcessor : IProcessor
{
    private readonly FluentApiBuilder _builder = new FluentApiBuilder();

    public void Process(ProcessorContext context)
    {
        // Check if the output directory exists; if not, create it.
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
            // Get the name of the entity.
            var entityName = entitySetMapping.EntitySet.ElementType.Name;

            // Retrieve common information about the entity.
            var commonInfo = parseResult.CommonEntityInfos[entityName];

            // Initialize Fluent API configuration for the entity.
            _builder.AddDefaultUsings()
                .AddNamespace()
                .StartEntityConfiguration(entityName)
                .ToTable(commonInfo.Table, commonInfo.Schema)
                .HasKey(commonInfo.PrimaryKeys.ToArray());

            // Add properties to the entity configuration.
            if (parseResult.TableColumnsDescriptions.TryGetValue(entityName, out var descriptions))
            {
                foreach (var description in descriptions)
                {
                    _builder.AddProperty(description);
                }
            }

            // Add relationships to the entity configuration.
            if (parseResult.RelationshipDescriptions.TryGetValue(entityName, out var relationships))
            {
                _builder.AddEmptyLine();
                foreach (var relationship in relationships)
                {
                    _builder.AddRelationship(relationship, commonInfo.PrimaryKeys);
                }
            }

            // End the entity configuration and retrieve the generated file text.
            _builder.EndEntityConfiguration();
            var generatedFileText = _builder.ToString();
            _builder.Clear();

            // Write the generated Fluent API configuration to a file.
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

