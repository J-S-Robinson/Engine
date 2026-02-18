using Stride.Core.Mathematics;

namespace Engine.Game
{
    public class GameState
    {
        public int CircleIndex { get; private set; } = 0;
        public int OpponentIndex { get; private set; } = (GameConfig.BoardCols - 1) + (GameConfig.BoardRows - 1) * GameConfig.BoardCols; // default (3,3)

        public Int2 GetCircleCell()
        {
            int row = CircleIndex / GameConfig.BoardCols;
            int col = CircleIndex % GameConfig.BoardCols;
            return new Int2(col, row);
        }

        public Int2 GetOpponentCell() => new Int2(OpponentIndex % GameConfig.BoardCols, OpponentIndex / GameConfig.BoardCols);

        public Vector2 GetCirclePosition()
        {
            var cell = GetCircleCell();
            return new Vector2(
                cell.X * GameConfig.Step + GameConfig.CircleOffset,
                cell.Y * GameConfig.Step + GameConfig.CircleOffset);
        }

        public Vector2 GetOpponentPosition()
        {
            var cell = GetOpponentCell();
            return new Vector2(cell.X * GameConfig.Step + GameConfig.CircleOffset, cell.Y * GameConfig.Step + GameConfig.CircleOffset);
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

            // Prevent moving into the opponent's occupied cell
            var opp = GetOpponentCell();
            if (newRow == opp.Y && newCol == opp.X)
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