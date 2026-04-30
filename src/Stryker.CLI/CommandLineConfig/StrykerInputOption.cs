using McMaster.Extensions.CommandLineUtils;

namespace Stryker.CLI.CommandLineConfig;

public sealed class StrykerInputOption : CommandOption
{
    public InputCategory Category { get; private set; }

    public StrykerInputOption(string template, CommandOptionType optionType, InputCategory category) : base(template, optionType) => Category = category;
}
