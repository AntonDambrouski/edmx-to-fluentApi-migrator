using Microsoft.Extensions.Configuration;
using FluentApiMigrator.Interfaces;
using FluentApiMigrator.Models;
using FluentApiMigrator.Processors;


var logger = NLog.LogManager.GetCurrentClassLogger();
try
{
   var configs = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
       .AddJsonFile("appsettings.json")
       .Build();

    var context = new ProcessorContext()
    {
        EdmxFilePath = configs.GetSection("edmxFilePath").Value,
        OutputDirectory = configs.GetSection("outputDirectory").Value,
    };

    var processors = new List<IProcessor> { new EdmxFileProcessor(), new FluentApiProcessor() };
    foreach (var processor in processors)
    {
        processor.Process(context);
    }
}
catch (Exception ex)
{
    logger.Error(ex, "Error occurred during migration proccess");
}
finally
{
    NLog.LogManager.Shutdown();
}