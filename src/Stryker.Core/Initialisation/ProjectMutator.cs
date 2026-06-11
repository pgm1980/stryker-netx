using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Reporting;
using Stryker.Core.MutationTest;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;

namespace Stryker.Core.Initialisation;

public partial class ProjectMutator(ILogger<ProjectMutator> logger, IServiceProvider serviceProvider) : IProjectMutator
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public IMutationTestProcess MutateProject(IStrykerOptions options, MutationTestInput input, IReporter reporters, IMutationTestProcess? mutationTestProcess = null)
    {
        var process = mutationTestProcess ?? _serviceProvider.GetRequiredService<IMutationTestProcess>();
        process.Initialize(input, options, reporters);

        // Enrich test projects info with unit tests
        EnrichTestProjectsWithTestInfo(input.InitialTestRun, input.TestProjectsInfo);

        // mutate
        process.Mutate();

        return process;
    }

    private void EnrichTestProjectsWithTestInfo(InitialTestRun initialTestRun, ITestProjectsInfo testProjectsInfo)
    {
        var unitTests =
            initialTestRun.Result.TestDescriptions
            .Select(desc => desc.Case)
            // F# has a different syntax tree and would throw further down the line
            .Where(unitTest => string.Equals(Path.GetExtension(unitTest.CodeFilePath), ".cs", StringComparison.Ordinal));

        foreach (var unitTest in unitTests)
        {
            var testFile = testProjectsInfo.TestFiles.SingleOrDefault(testFile => string.Equals(testFile.FilePath, unitTest.CodeFilePath, StringComparison.Ordinal));
            if (testFile is null)
            {
                LogCouldNotLocateUnitTest(_logger);
                continue;
            }

            // Sprint 179 (issue #292, 360°-Analyse H-12): VsTest reports LineNumber 0 when
            // source info is missing (inherited tests, missing PDBs), and reported lines may
            // not intersect a method declaration (top-level statements, stale PDBs). Both
            // used to throw unguarded and killed the whole run — skip the test instead.
            var lines = testFile.SyntaxTree.GetText().Lines;
            if (unitTest.LineNumber < 1 || unitTest.LineNumber > lines.Count)
            {
                LogUnusableTestLocation(_logger, unitTest.FullyQualifiedName, unitTest.LineNumber);
                continue;
            }

            var lineSpan = lines[unitTest.LineNumber - 1].Span;
            var nodesInSpan = testFile.SyntaxTree.GetRoot().DescendantNodes(lineSpan);
            var node = nodesInSpan.FirstOrDefault(n => n is MethodDeclarationSyntax);
            if (node is null)
            {
                LogUnusableTestLocation(_logger, unitTest.FullyQualifiedName, unitTest.LineNumber);
                continue;
            }

            testFile.AddTest(unitTest.Id, unitTest.FullyQualifiedName, node);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Could not locate unit test in any testfile. This should not happen and results in incorrect test reporting.")]
    private static partial void LogCouldNotLocateUnitTest(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Could not map unit test {TestName} to a method declaration (reported line {LineNumber}); test reporting for it will be incomplete.")]
    private static partial void LogUnusableTestLocation(ILogger logger, string testName, int lineNumber);
}
