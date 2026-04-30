using System;

namespace Stryker.Abstractions.Testing;

public sealed class TestDescription : ITestDescription
{
    public TestDescription(string id, string name, string testFilePath)
    {
        Id = id;
        Name = name;
        TestFilePath = testFilePath;
    }

    public string Id { get; }

    public string Name { get; }

    public string TestFilePath { get; }

    private bool Equals(TestDescription other)
    {
        return string.Equals(Id, other.Id, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return obj.GetType() == GetType() && Equals((TestDescription)obj);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Id);
    }
}
