using FluentAssertions;
using FsCheck.Xunit;
using Stryker.Abstractions;
using Xunit;

namespace Stryker.Core.Tests.Properties;

/// <summary>
/// v2.6.0 (Sprint 19, Item C / ToT property P1 + P5): FsCheck property
/// invariants over the <see cref="MutationProfile"/> [Flags] enum and the
/// <see cref="MutationProfileMembershipAttribute"/> roundtrip.
/// </summary>
public class MutationProfileProperties
{
    [Theory]
    [InlineData(MutationProfile.None)]
    [InlineData(MutationProfile.Defaults)]
    [InlineData(MutationProfile.Stronger)]
    [InlineData(MutationProfile.All)]
    [InlineData(MutationProfile.Defaults | MutationProfile.Stronger)]
    [InlineData(MutationProfile.Defaults | MutationProfile.All)]
    [InlineData(MutationProfile.Stronger | MutationProfile.All)]
    [InlineData(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All)]
    public void Profile_BitwiseSelfOr_IsIdentity(MutationProfile profile)
        => (profile | profile).Should().Be(profile);

    [Theory]
    [InlineData(MutationProfile.None)]
    [InlineData(MutationProfile.Defaults)]
    [InlineData(MutationProfile.Stronger)]
    [InlineData(MutationProfile.All)]
    public void Profile_BitwiseSelfAnd_IsIdentity(MutationProfile profile)
        => (profile & profile).Should().Be(profile);

    [Property(MaxTest = 50)]
    public bool MembershipAttribute_RoundtripsProfileValue(MutationProfile profile)
    {
        var attr = new MutationProfileMembershipAttribute(profile);
        return attr.Profiles == profile;
    }

    [Property(MaxTest = 50)]
    public bool OredProfile_HasFlagOfBothComponents(MutationProfile a, MutationProfile b)
    {
        var combined = a | b;
        return combined.HasFlag(a) && combined.HasFlag(b);
    }
}
