using System.IO.Abstractions;
using Stryker.Configuration.Options;

namespace Stryker.CLI.Logging;

public interface ILoggingInitializer
{
    void SetupLogOptions(IStrykerInputs inputs, IFileSystem? fileSystem = null);
}
