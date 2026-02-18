using Stride.Core.Mathematics;

namespace Engine.Game
{
    public static class GameConfig
    {
        public const int Step = 150;
        public const int CircleDiameter = 140;
        public const int CircleOffset = (Step - CircleDiameter) / 2;
        public const int BoardCols = 4;
        public const int BoardRows = 4;

        public const float AnimDurationSeconds = 0.5f;
        public const int MaxQueuedMoves = 8;

        public static readonly Color LightBlue = new Color(0x4A, 0x90, 0xE2, 255);
        public static readonly Color DarkBlue = new Color(0x2A, 0x60, 0xA8, 255);
        public static readonly Color CircleTint = new Color(0x7B, 0xD8, 0x8F, 255);
        public static readonly Color OpponentTint = new Color(0xC6, 0x4A, 0x4A, 255); // muted red
    }
}