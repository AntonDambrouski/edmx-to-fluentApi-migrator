namespace FluentApiMigrator.Models;

public class ProcessorContext
{
    public EdmxParseResult EdmxParseResult { get; set; }
    public string? EdmxFilePath { get; set; }
    public string? OutputDirectory { get; set; }
}

