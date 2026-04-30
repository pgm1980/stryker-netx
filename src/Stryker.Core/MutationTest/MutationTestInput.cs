using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Testing;
using Stryker.Core.Initialisation;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;

namespace Stryker.Core.MutationTest;

/// <summary>
/// Represents the state the application under test is in.
/// </summary>
public class MutationTestInput
{
    /// <summary>
    /// Contains all information about the project to mutate
    /// </summary>
    public SourceProjectInfo SourceProjectInfo { get; set; } = null!; // set via object initializer by callers (Initialisation pipeline)

    /// <summary>
    /// Contains all information about the tests to run
    /// </summary>
    public ITestProjectsInfo TestProjectsInfo { get; set; } = null!; // set via object initializer by callers (Initialisation pipeline)

    /// <summary>
    /// The testrunner that will be used for the mutation test run
    /// </summary>
    public ITestRunner TestRunner { get; set; } = null!; // set via object initializer by callers (Initialisation pipeline)

    /// <summary>
    /// Get/Set the initial test
    /// </summary>
    public InitialTestRun InitialTestRun { get; set; } = null!; // set via object initializer by callers (Initialisation pipeline)
}
