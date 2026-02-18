namespace Engine.Game
{
    public readonly struct MoveCommand
    {
        public MoveCommand(int dRow, int dCol)
        {
            DRow = dRow;
            DCol = dCol;
        }

        public int DRow { get; }
        public int DCol { get; }
    }
}