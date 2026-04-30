using System;
using Stryker.Abstractions.Exceptions;

namespace Stryker.Configuration.Options.Inputs;

public class DashboardUrlInput : Input<string>
{
    // Default Stryker dashboard URL — this is the canonical public dashboard endpoint and acts as
    // a configurable default that callers may override via the SuppliedInput. Hard-coding here is
    // intentional and not a security/portability concern (S1075 false positive).
#pragma warning disable S1075 // URIs should not be hardcoded — default value for an overridable parameter
    public static readonly string DefaultUrl = "https://dashboard.stryker-mutator.io";
#pragma warning restore S1075
    public override string Default => DefaultUrl;

    protected override string Description => "Alternative url for Stryker Dashboard.";

    public string Validate()
    {
        if (SuppliedInput is not null)
        {
            if (!Uri.IsWellFormedUriString(SuppliedInput, UriKind.Absolute))
            {
                throw new InputException($"Stryker dashboard url '{SuppliedInput}' is invalid.");
            }

            return SuppliedInput;
        }
        return Default;
    }
}
