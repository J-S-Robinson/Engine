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

        // attempted-move (bump) state
        private bool _isAttempting;
        private Vector2 _attemptStartPos = Vector2.Zero;
        private Vector2 _attemptTargetPos = Vector2.Zero;
        private float _attemptElapsed;
        private const float AttemptPhaseDuration = 0.15f; // out/back phase (total = 0.3s)

        // Compute bump distance so the moving circle's perimeter reaches the opponent's perimeter:
        // bumpDistance = distanceBetweenCellCenters - circleDiameter
        private static float ComputeBumpDistance(int dRow, int dCol)
        {
            var cellDist = GameConfig.Step * MathF.Sqrt(dRow * dRow + dCol * dCol);
            var bump = cellDist - GameConfig.CircleDiameter;
            return MathF.Max(0f, bump);
        }

        private readonly Queue<MoveCommand> _moveQueue = new Queue<MoveCommand>();

        public Vector2 CirclePosition { get; private set; }

        // color-hit state (pulse + residual)
        private bool _isColorHitActive;
        private float _colorHitElapsed;
        private const float ColorPulseDuration = 0.05f;     // slightly longer pulse (user-requested)
        private const float ColorResidualDuration = 0.25f;  // slower residual fade
        private const float ColorTotalDuration = ColorPulseDuration + ColorResidualDuration;
        public Color CurrentTint { get; private set; } = GameConfig.CircleTint;
        public Color CurrentOpponentTint { get; private set; } = GameConfig.OpponentTint;

        private bool IsBusy => _isAnimating || _isAttempting;

        public void SnapToState(GameState state)
        {
            CirclePosition = state.GetCirclePosition();
            _isAnimating = false;
            _isAttempting = false;
            _animElapsed = 0f;
            _attemptElapsed = 0f;
            CurrentTint = GameConfig.CircleTint;
            CurrentOpponentTint = GameConfig.OpponentTint;
            _moveQueue.Clear();
        }

        public void QueueOrStartMove(GameState state, MoveCommand move)
        {
            var cell = state.GetCircleCell();
            int row = cell.Y;
            int col = cell.X;

            if (IsBusy)
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

                // Allow queuing moves that would end up targeting the opponent's cell.
                // The animator will play an "attempted bump" when such a queued move is processed.
                if (_moveQueue.Count >= GameConfig.MaxQueuedMoves)
                    return;

                _moveQueue.Enqueue(move);
                return;
            }

            // If the requested target would be the opponent, play a short bump animation instead of moving.
            int newRow = row + move.DRow;
            int newCol = col + move.DCol;
            if (state.IsOnBoard(newRow, newCol))
            {
                var opp = state.GetOpponentCell();
                if (newRow == opp.Y && newCol == opp.X)
                {
                    _attemptStartPos = CirclePosition;
                    // direction toward opponent (normalized)
                    var dir = new Vector2(move.DCol, move.DRow);
                    var len = MathF.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
                    if (len <= 0f) return;
                    var norm = dir / len;
                    var bump = ComputeBumpDistance(move.DRow, move.DCol);
                    _attemptTargetPos = _attemptStartPos + norm * bump;
                    _attemptElapsed = 0f;
                    _isAttempting = true;
                    return;
                }
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
            Update((float)gameTime.Elapsed.TotalSeconds, state);
        }

        public void Update(float deltaSeconds, GameState state)
        {
            // Attempted bump animation takes precedence when active
            if (_isAttempting)
            {
                _attemptElapsed += deltaSeconds;
                float half = AttemptPhaseDuration;
                float total = half * 2f;
                if (_attemptElapsed < half)
                {
                    float at = Math.Min(_attemptElapsed / half, 1f);
                    var aeased = EaseOutCubic(at);
                    CirclePosition = Vector2.Lerp(_attemptStartPos, _attemptTargetPos, aeased);
                    // color starts at the moment of contact (end of outward phase)
                    return;
                }
                else if (_attemptElapsed < total)
                {
                    // if we just reached the contact point, start the color-hit effect now
                    if (!_isColorHitActive)
                    {
                        _isColorHitActive = true;
                        _colorHitElapsed = 0f;
                    }

                    // update color-hit while returning (and drive its tween)
                    UpdateColorHit(deltaSeconds);

                    float at2 = Math.Min((_attemptElapsed - half) / half, 1f);
                    var aeased2 = EaseOutCubic(at2);
                    CirclePosition = Vector2.Lerp(_attemptTargetPos, _attemptStartPos, aeased2);
                    return;
                }

                // finished bump — restore exact start and continue to queued moves if any
                CirclePosition = _attemptStartPos;
                _isAttempting = false;
                // ensure color-hit finishes correctly
                UpdateColorHit(deltaSeconds);

                if (_moveQueue.Count > 0)
                {
                    var next = _moveQueue.Dequeue();
                    // If the queued move would target the opponent, play a bump instead of moving.
                    var cell = state.GetCircleCell();
                    int newRow = cell.Y + next.DRow;
                    int newCol = cell.X + next.DCol;
                    var opp = state.GetOpponentCell();
                    if (newRow == opp.Y && newCol == opp.X)
                    {
                        _attemptStartPos = CirclePosition;
                        var ndir = new Vector2(next.DCol, next.DRow);
                        var nlen = MathF.Sqrt(ndir.X * ndir.X + ndir.Y * ndir.Y);
                        if (nlen == 0f) return;
                        var nnorm = ndir / nlen;
                        var nbump = ComputeBumpDistance(next.DRow, next.DCol);
                        _attemptTargetPos = _attemptStartPos + nnorm * nbump;
                        _attemptElapsed = 0f;
                        _isAttempting = true;
                        return;
                    }

                    if (state.TryApplyMove(next, out var targetCell))
                    {
                        _animStartPos = CirclePosition;
                        _animTargetPos = new Vector2(
                            targetCell.X * GameConfig.Step + GameConfig.CircleOffset,
                            targetCell.Y * GameConfig.Step + GameConfig.CircleOffset);
                        _animElapsed = 0f;
                        _isAnimating = true;
                    }
                }

                return;
            }

            // during normal move animation, also update color-hit if active
            UpdateColorHit(deltaSeconds);

            if (!_isAnimating)
                return;

            _animElapsed += deltaSeconds;
            float t = Math.Min(_animElapsed / GameConfig.AnimDurationSeconds, 1f);
            float eased = EaseOutCubic(t);
            CirclePosition = Vector2.Lerp(_animStartPos, _animTargetPos, eased);

            if (t < 1f)
                return;

            CirclePosition = _animTargetPos;

            if (_moveQueue.Count > 0)
            {
                var next = _moveQueue.Dequeue();

                // check queued move against opponent cell first
                var cell = state.GetCircleCell();
                int newRow = cell.Y + next.DRow;
                int newCol = cell.X + next.DCol;
                var opp = state.GetOpponentCell();
                if (newRow == opp.Y && newCol == opp.X)
                {
                    _attemptStartPos = CirclePosition;
                    var ndir2 = new Vector2(next.DCol, next.DRow);
                    var nlen2 = MathF.Sqrt(ndir2.X * ndir2.X + ndir2.Y * ndir2.Y);
                    if (nlen2 == 0f) return;
                    var nnorm2 = ndir2 / nlen2;
                    var nbump2 = ComputeBumpDistance(next.DRow, next.DCol);
                    _attemptTargetPos = _attemptStartPos + nnorm2 * nbump2;
                    _attemptElapsed = 0f;
                    _isAttempting = true;
                    return;
                }

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

        private const float ResidualLightenAmount = 0.5f; // how much lighter the immediate residual tint is (0..1)

        private void UpdateColorHit(float deltaSeconds)
        {
            if (!_isColorHitActive) return;

            // Player base (normalized) and opponent base (normalized)
            var basePlayerC = GameConfig.CircleTint;
            var basePlayerVec = basePlayerC.ToVector4(); // Vector4(r,g,b,a) in 0..1
            float basePR = basePlayerVec.X;
            float basePG = basePlayerVec.Y;
            float basePB = basePlayerVec.Z;
            float basePA = basePlayerVec.W;

            var baseOppC = GameConfig.OpponentTint;
            var baseOppVec = baseOppC.ToVector4();
            float baseOR = baseOppVec.X;
            float baseOG = baseOppVec.Y;
            float baseOB = baseOppVec.Z;
            float baseOA = baseOppVec.W;

            _colorHitElapsed += deltaSeconds;

            if (_colorHitElapsed <= ColorPulseDuration)
            {
                // quick pulse toward white (peak)
                float p = MathF.Min(_colorHitElapsed / ColorPulseDuration, 1f);
                float pe = EaseOutCubic(p);

                // player
                float pr = basePR * (1f - pe) + 1f * pe;
                float pg = basePG * (1f - pe) + 1f * pe;
                float pb = basePB * (1f - pe) + 1f * pe;
                CurrentTint = new Color(pr, pg, pb, basePA);

                // opponent (same envelope)
                float orr = baseOR * (1f - pe) + 1f * pe;
                float ogg = baseOG * (1f - pe) + 1f * pe;
                float obb = baseOB * (1f - pe) + 1f * pe;
                CurrentOpponentTint = new Color(orr, ogg, obb, baseOA);

                return;
            }

            // Residual: immediately drop to a lighter tint (in normalized space), then slowly fade back to base.
            float rem = _colorHitElapsed - ColorPulseDuration;

            float lightPR = basePR * (1f - ResidualLightenAmount) + 1f * ResidualLightenAmount;
            float lightPG = basePG * (1f - ResidualLightenAmount) + 1f * ResidualLightenAmount;
            float lightPB = basePB * (1f - ResidualLightenAmount) + 1f * ResidualLightenAmount;

            float lightOR = baseOR * (1f - ResidualLightenAmount) + 1f * ResidualLightenAmount;
            float lightOG = baseOG * (1f - ResidualLightenAmount) + 1f * ResidualLightenAmount;
            float lightOB = baseOB * (1f - ResidualLightenAmount) + 1f * ResidualLightenAmount;

            if (rem <= ColorResidualDuration)
            {
                float p = MathF.Min(rem / ColorResidualDuration, 1f);
                float pe = EaseOutCubic(p);

                float pr = lightPR * (1f - pe) + basePR * pe;
                float pg = lightPG * (1f - pe) + basePG * pe;
                float pb = lightPB * (1f - pe) + basePB * pe;
                CurrentTint = new Color(pr, pg, pb, basePA);

                float orr = lightOR * (1f - pe) + baseOR * pe;
                float ogg = lightOG * (1f - pe) + baseOG * pe;
                float obb = lightOB * (1f - pe) + baseOB * pe;
                CurrentOpponentTint = new Color(orr, ogg, obb, baseOA);

                return;
            }

            // finished
            _isColorHitActive = false;
            _colorHitElapsed = 0f;
            CurrentTint = basePlayerC;
            CurrentOpponentTint = baseOppC;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }
    }
}