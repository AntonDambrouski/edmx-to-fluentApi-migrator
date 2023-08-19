using FluentApiMigrator.Interfaces;
using FluentApiMigrator.Processors;


var logger = NLog.LogManager.GetCurrentClassLogger();
try
{
    var processors = new List<IProcessor> { new EdmxProcessor(), new FluentApiProcessor() };
    foreach (var processor in processors)
    {
        processor.Process();
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