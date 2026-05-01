using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Utilities.Logging;

namespace Stryker.Configuration.Options.Inputs;

/// <summary>
/// <b>Obsolete in v2.2.0 — deprecated per ADR-021.</b> Originally introduced in
/// v2.0.0 (ADR-016) as the CLI selector for the planned HotSwap mutation engine.
/// The HotSwap engine was removed because the underlying ADR-016 was based on a
/// wrong mental model of Stryker.NET's cost structure. This input class is kept
/// as a deprecated shim so that scripts and config files using <c>--engine
/// recompile|hotswap</c> continue to parse without breaking; a deprecation
/// warning is logged on use.
/// </summary>
// S1133 + CS0618 deferred to v3.0 per ADR-021 (the shim is the v2.x backwards-compat surface).
#pragma warning disable CS0618, S1133
[Obsolete("Deprecated in v2.2.0 (ADR-021): HotSwap engine was based on a wrong mental model. The class is a CLI shim that emits a deprecation warning; v3.0 may remove it.")]
public partial class MutationEngineInput : Input<string>
{
    private static readonly ILogger Logger = ApplicationLogging.LoggerFactory.CreateLogger<MutationEngineInput>();

    public override string Default => MutationEngine.Recompile.ToString();

    protected override string Description =>
        "[Deprecated v2.2.0 — see ADR-021] Mutation execution engine selector. " +
        "Both 'Recompile' and 'HotSwap' are accepted for backwards compatibility but have no functional effect: " +
        "Stryker.NET's all-mutations-in-one-assembly + ActiveMutationId-runtime-switching pattern doesn't benefit " +
        "from a hot-swap abstraction. Use --coverage-analysis for performance tuning instead.";

    protected override IEnumerable<string> AllowedOptions => EnumToStrings(typeof(MutationEngine));

    public MutationEngine Validate()
    {
        if (SuppliedInput is null)
        {
            return MutationEngine.Recompile;
        }
        if (Enum.TryParse(SuppliedInput, ignoreCase: true, out MutationEngine engine))
        {
            LogDeprecated(Logger, SuppliedInput);
            return engine;
        }
        throw new InputException(
            $"The given mutation engine ({SuppliedInput}) is invalid. " +
            $"Valid options are: [{string.Join(", ", Enum.GetValues<MutationEngine>())}] " +
            $"(both options are deprecated as of v2.2.0; see ADR-021).");
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "[Deprecated v2.2.0] --engine flag (supplied value: '{SuppliedValue}') is deprecated per ADR-021. " +
                  "The HotSwap engine was based on a wrong mental model of Stryker.NET's cost structure. " +
                  "The flag is accepted as a no-op shim; remove it from your config / scripts. " +
                  "Use --coverage-analysis for performance tuning instead.")]
    private static partial void LogDeprecated(ILogger logger, string suppliedValue);
}
#pragma warning restore CS0618, S1133
