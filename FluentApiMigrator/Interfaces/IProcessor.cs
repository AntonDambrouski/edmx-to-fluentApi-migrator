using FluentApiMigrator.Models;

namespace FluentApiMigrator.Interfaces;

public interface IProcessor
{
    void Process(ProcessorContext context);
}
