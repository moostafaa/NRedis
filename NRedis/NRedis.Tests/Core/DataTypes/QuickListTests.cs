using FluentAssertions;
using NRedis.Core.DataTypes;
using Xunit;

namespace NRedis.Tests.Core.DataTypes;


public class QuickListTests
{
    [Fact]
    public void QuickList_PushAndPop_ShouldMaintainOrder()
    {
        // Arrange
        var ql = new QuickList();
        ql.PushTail(new Sds("one"));
        ql.PushTail(new Sds("two"));
        ql.PushHead(new Sds("zero"));

        // Assert - Initial state
        ql.Count.Should().Be(3);

        // Act & Assert - Pop head
        ql.PopHead(out var head).Should().BeTrue();
        head.ToString().Should().Be("zero");

        // Act & Assert - Pop tail
        ql.PopTail(out var tail).Should().BeTrue();
        tail.ToString().Should().Be("two");

        // Assert - Final state
        ql.Count.Should().Be(1);
        ql.Index(0).ToString().Should().Be("one");
    }

    [Fact]
    public void QuickList_Index_ShouldReturnCorrectElement()
    {
        // Arrange
        var ql = new QuickList();
        ql.PushTail(new Sds("a"));
        ql.PushTail(new Sds("b"));
        ql.PushTail(new Sds("c"));
        ql.PushTail(new Sds("d"));

        // Act & Assert
        ql.Index(2).ToString().Should().Be("c");
        ql.Index(-1).ToString().Should().Be("d");
        ql.Index(100).Should().BeNull();
    }

    [Fact]
    public void QuickList_Rotate_ShouldMoveTailToHead()
    {
        // Arrange
        var ql = new QuickList();
        ql.PushTail(new Sds("a"));
        ql.PushTail(new Sds("b"));
        ql.PushTail(new Sds("c"));

        // Act
        ql.Rotate(); // c should move to the front

        // Assert
        ql.Index(0).ToString().Should().Be("c");
        ql.Index(1).ToString().Should().Be("a");
        ql.Index(2).ToString().Should().Be("b");
    }
}
