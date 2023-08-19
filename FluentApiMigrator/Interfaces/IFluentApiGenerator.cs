using FluentApiMigrator.Models;

namespace FluentApiMigrator.Interfaces;

internal interface IFluentApiGenerator
{
    void Generate(FluentApiGeneratorContext context, string outputFolderPath);
}
