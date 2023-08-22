using FluentApiMigrator.Exceptions;
using FluentApiMigrator.Interfaces;
using FluentApiMigrator.Models;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
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
        var root = XElement.Load(filePath, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
        var runtimeElemets = root.Elements("Runtime") ?? throw new EdmxFileException("Runtime section is missing in the edmx file.");
        var csdlSection = runtimeElemets.Elements("StarageModels")?.Elements("Shema")?.FirstOrDefault()
            ?? throw new EdmxFileException("CSDL section could not be loaded.");

        var ssdlSection = runtimeElemets.Elements("ConceptualModels")?.Elements("Shema")?.FirstOrDefault()
            ?? throw new EdmxFileException("SSDL section could not be loaded.");

        var mappingsSection = runtimeElemets.Elements("Mappings")?.Elements("Mapping")?.FirstOrDefault()
            ?? throw new EdmxFileException("CSDL section could not be loaded.");

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
        throw new NotImplementedException();
    }
}
