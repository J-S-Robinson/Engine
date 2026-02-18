using System;
using System.Collections.Generic;
using Engine.Game;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Engine.Animation
{
    public class MoveAnimator
    {
        private bool _isAnimating;
        private Vector2 _animStartPos = Vector2.Zero;
        private Vector2 _animTargetPos = Vector2.Zero;
        private float _animElapsed;
        private readonly Queue<MoveCommand> _moveQueue = new Queue<MoveCommand>();

        public Vector2 CirclePosition { get; private set; }

        public void SnapToState(GameState state)
        {
            CirclePosition = state.GetCirclePosition();
            _isAnimating = false;
            _animElapsed = 0f;
            _moveQueue.Clear();
        }

        public void QueueOrStartMove(GameState state, MoveCommand move)
        {
            var cell = state.GetCircleCell();
            int row = cell.Y;
            int col = cell.X;

            if (_isAnimating)
            {
                int predRow = row;
                int predCol = col;
                foreach (var queuedMove in _moveQueue)
                {
                    predRow += queuedMove.DRow;
                    predCol += queuedMove.DCol;
                }

                int newPredRow = predRow + move.DRow;
                int newPredCol = predCol + move.DCol;
                if (!state.IsOnBoard(newPredRow, newPredCol))
                    return;

                // Prevent queuing a move that would land on the opponent's cell
                var opp = state.GetOpponentCell();
                if (newPredRow == opp.Y && newPredCol == opp.X)
                    return;

                if (_moveQueue.Count >= GameConfig.MaxQueuedMoves)
                    return;

                _moveQueue.Enqueue(move);
                return;
            }

            if (!state.TryApplyMove(move, out var targetCell))
                return;

            _animStartPos = CirclePosition;
            _animTargetPos = new Vector2(
                targetCell.X * GameConfig.Step + GameConfig.CircleOffset,
                targetCell.Y * GameConfig.Step + GameConfig.CircleOffset);
            _animElapsed = 0f;
            _isAnimating = true;
        }

        public void Update(GameTime gameTime, GameState state)
        {
            if (!_isAnimating)
                return;

            _animElapsed += (float)gameTime.Elapsed.TotalSeconds;
            float t = Math.Min(_animElapsed / GameConfig.AnimDurationSeconds, 1f);
            float eased = EaseOutCubic(t);
            CirclePosition = Vector2.Lerp(_animStartPos, _animTargetPos, eased);

            if (t < 1f)
                return;

            CirclePosition = _animTargetPos;

            if (_moveQueue.Count > 0)
            {
                var next = _moveQueue.Dequeue();
                if (state.TryApplyMove(next, out var targetCell))
                {
                    _animStartPos = CirclePosition;
                    _animTargetPos = new Vector2(
                        targetCell.X * GameConfig.Step + GameConfig.CircleOffset,
                        targetCell.Y * GameConfig.Step + GameConfig.CircleOffset);
                    _animElapsed = 0f;
                    _isAnimating = true;
                    return;
                }
            }

            _isAnimating = false;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }
    }
}