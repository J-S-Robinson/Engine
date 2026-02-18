using Engine.Animation;
using Engine.Game;
using Stride.Core.Mathematics;
using Xunit;

namespace Engine.Tests.Animation;

public class MoveAnimatorTests
{
    [Fact]
    [Trait("Category", "Animation")]
    public void NormalMove_ReachesTargetAfterDuration()
    {
        var state = new GameState();
        var animator = new MoveAnimator();
        animator.SnapToState(state);

        animator.QueueOrStartMove(state, new MoveCommand(0, 1));

        for (int i = 0; i < 10; i++)
        {
            animator.Update(0.05f, state);
        }

        Assert.Equal(new Int2(1, 0), state.GetCircleCell());
        var expected = new Vector2(
            GameConfig.Step + GameConfig.CircleOffset,
            GameConfig.CircleOffset);
        Assert.Equal(expected, animator.CirclePosition);
    }

    [Fact]
    [Trait("Category", "Animation")]
    public void QueuedMove_IsAppliedAfterCurrentAnimationCompletes()
    {
        var state = new GameState();
        var animator = new MoveAnimator();
        animator.SnapToState(state);

        animator.QueueOrStartMove(state, new MoveCommand(0, 1));
        animator.QueueOrStartMove(state, new MoveCommand(1, 0));

        for (int i = 0; i < 20; i++)
        {
            animator.Update(0.05f, state);
        }

        Assert.Equal(new Int2(1, 1), state.GetCircleCell());
        var expected = new Vector2(
            GameConfig.Step + GameConfig.CircleOffset,
            GameConfig.Step + GameConfig.CircleOffset);
        Assert.Equal(expected, animator.CirclePosition);
    }

    [Fact]
    [Trait("Category", "Animation")]
    public void Bump_TriggersTintOnBothCircles_ThenRestores()
    {
        var state = new GameState();
        var animator = new MoveAnimator();

        // Move player to (2,3) so next right move bumps opponent at (3,3)
        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(1, 0), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(0, 1), out _));
        Assert.True(state.TryApplyMove(new MoveCommand(0, 1), out _));
        animator.SnapToState(state);

        animator.QueueOrStartMove(state, new MoveCommand(0, 1));

        // Cross into return phase so color-hit has started.
        animator.Update(0.16f, state);

        var playerBase = GameConfig.CircleTint.ToVector4();
        var opponentBase = GameConfig.OpponentTint.ToVector4();
        var playerNow = animator.CurrentTint.ToVector4();
        var opponentNow = animator.CurrentOpponentTint.ToVector4();

        Assert.True(VectorDistance(playerNow, playerBase) > 0.0001f);
        Assert.True(VectorDistance(opponentNow, opponentBase) > 0.0001f);

        // Finish bump + color residual
        for (int i = 0; i < 40; i++)
        {
            animator.Update(0.02f, state);
        }

        Assert.True(VectorDistance(animator.CurrentTint.ToVector4(), playerBase) < 0.0001f);
        Assert.True(VectorDistance(animator.CurrentOpponentTint.ToVector4(), opponentBase) < 0.0001f);
        Assert.Equal(new Int2(2, 3), state.GetCircleCell());
    }

    private static float VectorDistance(Vector4 a, Vector4 b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        float dw = a.W - b.W;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz + dw * dw);
    }
}
