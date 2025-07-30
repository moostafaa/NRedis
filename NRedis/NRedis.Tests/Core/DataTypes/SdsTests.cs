using FluentAssertions;
using NRedis.Core.DataTypes;
using Xunit;

namespace NRedis.Tests.Core.DataTypes;

public class SdsTests
{
    [Fact]
    public void Sds_CreationAndToString_ShouldWork()
    {
        // Arrange
        var s = new Sds("hello");

        // Act & Assert
        s.ToString().Should().Be("hello");
    }

    [Fact]
    public void Sds_Append_ShouldCombineStrings()
    {
        // Arrange
        var s1 = new Sds("hello, ");
        var s2 = new Sds("world");

        // Act
        s1.Append(s2);

        // Assert
        s1.ToString().Should().Be("hello, world");
    }

    [Fact]
    public void Sds_EqualsAndHashCode_ShouldBehaveCorrectlyInDictionary()
    {
        // Arrange
        var dict = new Dictionary<Sds, string>();
        var key1 = new Sds("mykey");
        var key2 = new Sds("mykey");

        // Act
        dict[key1] = "myvalue";

        // Assert
        dict.Should().ContainKey(key2);
        dict[key2].Should().Be("myvalue");
        key1.GetHashCode().Should().Be(key2.GetHashCode());
    }
}
