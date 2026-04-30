using McMaster.Extensions.CommandLineUtils;
using Stryker.CLI.CommandLineConfig;
using Stryker.Configuration.Options;

namespace Stryker.CLI;

public interface IConfigBuilder
{
    void Build(IStrykerInputs inputs, string[] args, CommandLineApplication app, CommandLineConfigReader cmdConfigHandler);
}
