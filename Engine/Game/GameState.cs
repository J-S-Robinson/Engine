using Stride.Core.Mathematics;

namespace Engine.Game
{
    public class GameState
    {
        public int CircleIndex { get; private set; } = 0;

        public Int2 GetCircleCell()
        {
            int row = CircleIndex / GameConfig.BoardCols;
            int col = CircleIndex % GameConfig.BoardCols;
            return new Int2(col, row);
        }

        public Vector2 GetCirclePosition()
        {
            var cell = GetCircleCell();
            return new Vector2(
                cell.X * GameConfig.Step + GameConfig.CircleOffset,
                cell.Y * GameConfig.Step + GameConfig.CircleOffset);
        }

        public bool TryApplyMove(MoveCommand move, out Int2 targetCell)
        {
            var current = GetCircleCell();
            int newRow = current.Y + move.DRow;
            int newCol = current.X + move.DCol;

            if (!IsOnBoard(newRow, newCol))
            {
                targetCell = current;
                return false;
            }

            CircleIndex = newRow * GameConfig.BoardCols + newCol;
            targetCell = new Int2(newCol, newRow);
            return true;
        }

        public bool IsOnBoard(int row, int col)
        {
            return row >= 0 && row < GameConfig.BoardRows && col >= 0 && col < GameConfig.BoardCols;
        }
    }
}