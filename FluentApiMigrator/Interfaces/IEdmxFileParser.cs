using FluentApiMigrator.Models;

namespace FluentApiMigrator.Interfaces;

internal interface IEdmxFileParser
{
    EdmxParseResult Parse(string filePath);
}
