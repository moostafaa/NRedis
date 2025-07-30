using FluentAssertions;
using NRedis.Core.DataTypes;
using Xunit;

namespace NRedis.Tests.Core.DataTypes;

public class SkipListTests
{
    [Fact]
    public void SkipList_InsertAndOrder_ShouldBeCorrect()
    {
        // Arrange
        var sl = new SkipList();

        // Act
        sl.Insert(new Sds("player1"), 100);
        sl.Insert(new Sds("player3"), 300);
        sl.Insert(new Sds("player2"), 200);

        // Assert
        sl.Count.Should().Be(3);

        var node = sl.GetElementByRank(1);
        node.Member.ToString().Should().Be("player1");
        node.Score.Should().Be(100);

        node = node.Levels[0].Forward;
        node.Member.ToString().Should().Be("player2");
        node.Score.Should().Be(200);

        node = node.Levels[0].Forward;
        node.Member.ToString().Should().Be("player3");
        node.Score.Should().Be(300);
    }

    [Fact]
    public void SkipList_Delete_ShouldRemoveNode()
    {
        // Arrange
        var sl = new SkipList();
        sl.Insert(new Sds("player1"), 100);
        sl.Insert(new Sds("player2"), 200);
        sl.Count.Should().Be(2);

        // Act
        bool deleted = sl.Delete(new Sds("player1"), 100);

        // Assert
        deleted.Should().BeTrue();
        sl.Count.Should().Be(1);
        sl.GetElementByRank(1).Member.ToString().Should().Be("player2");

        // Act & Assert - Deleting non-existent node
        bool notDeleted = sl.Delete(new Sds("nonexistent"), 999);
        notDeleted.Should().BeFalse();
    }
}
