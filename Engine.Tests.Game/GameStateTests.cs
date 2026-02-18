using Engine.Game;
using Stride.Core.Mathematics;
using Xunit;

namespace Engine.Tests.Game;

public class GameStateTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(3, 3, true)]
    [InlineData(-1, 0, false)]
    [InlineData(0, -1, false)]
    [InlineData(4, 0, false)]
    [InlineData(0, 4, false)]
    [Trait("Category", "Game")]
    public void IsOnBoard_ReturnsExpected(int row, int col, bool expected)
    {
        var state = new GameState();

        Assert.Equal(expected, state.IsOnBoard(row, col));
    }

    [Fact]
    [Trait("Category", "Game")]
    public void TryApplyMove_RejectsOutOfBoardMove()
    {
        var state = new GameState();

        var moved = state.TryApplyMove(new MoveCommand(-1, 0), out var target);

        Assert.False(moved);
        Assert.Equal(new Int2(0, 0), target);
        Assert.Equal(new Int2(0, 0), state.GetCircleCell());
    }

    [Fact]
    [Trait("Category", "Game")]
    public void TryApplyMove_RejectsMoveIntoOpponentCell()
    {
        var state = new GameState();

        // Move to (2,3)
        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(0, 1), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(0, 1), out _));
        Assert.Equal(new Int2(2, 3), state.GetCircleCell());

        var moved = state.TryApplyMove(new MoveCommand(0, 1), out var target);

        Assert.False(moved);
        Assert.Equal(new Int2(2, 3), target);
        Assert.Equal(new Int2(2, 3), state.GetCircleCell());
    }

    [Fact]
    [Trait("Category", "Game")]
    public void PositionMapping_UsesStepAndOffset()
    {
        var state = new GameState();

        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _)); // (0,1)
        Assert.True(state.TryApplyMove(new MoveCommand(0, 1), out _)); // (1,1)

        var pos = state.GetCirclePosition();
        var expected = new Vector2(
            GameConfig.Step + GameConfig.CircleOffset,
            GameConfig.Step + GameConfig.CircleOffset);

        Assert.Equal(expected, pos);
    }
}
