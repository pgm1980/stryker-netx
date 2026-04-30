using System.Collections.Generic;

namespace Stryker.Abstractions.ProjectComponents;

public interface IProjectComponent : IReadOnlyProjectComponent
{
    new string FullPath { get; set; }
    new IEnumerable<IMutant> Mutants { get; set; }
    new IFolderComposite Parent { get; set; }
    new string RelativePath { get; set; }
}
