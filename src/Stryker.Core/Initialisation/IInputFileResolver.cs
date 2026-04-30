using System.Collections.Generic;
using System.IO.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Core.ProjectComponents.SourceProjects;

namespace Stryker.Core.Initialisation;

public interface IInputFileResolver
{
    IReadOnlyCollection<SourceProjectInfo> ResolveSourceProjectInfos(IStrykerOptions options);
    IFileSystem FileSystem { get; }
}
