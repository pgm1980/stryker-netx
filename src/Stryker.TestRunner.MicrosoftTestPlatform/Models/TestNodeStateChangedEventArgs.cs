using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Models;

/// <summary>
/// JSON-RPC payload for the "testing/testUpdates/tests" notification — represents a batch of node-state changes for a given run.
/// Note: Suffix "EventArgs" is preserved 1:1 with upstream Stryker.NET 4.14.1 even though this record does not derive from
/// <see cref="EventArgs"/>; renaming would break public-surface compatibility (ADR-001/ADR-003). CA1711 is suppressed
/// project-wide via .editorconfig.
/// </summary>
[ExcludeFromCodeCoverage]
public record TestNodeStateChangedEventArgs(
    [property: JsonPropertyName("runId")] Guid RunId,
    [property: JsonPropertyName("changes")] TestNodeUpdate[] Changes
    );
