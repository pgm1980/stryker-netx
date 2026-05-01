using System.Xml;
using FluentAssertions;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 25 (v2.12.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.VsTest.UnitTest/CoverageCaptureTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class CoverageCaptureTests : TestBase
{
    [Fact]
    public void CanParseConfiguration()
    {
        var referenceConf = """<Parameters><Environment name="ActiveMutant" value="1"/></Parameters>""";
        var node = new XmlDocument();

        node.LoadXml(referenceConf);

        node.ChildNodes.Count.Should().Be(1);
        var coolChild = node.GetElementsByTagName("Parameters");
        coolChild[0]!.Name.Should().Be("Parameters");
        var envVars = node.GetElementsByTagName("Environment");

        envVars.Count.Should().Be(1);
    }
}
