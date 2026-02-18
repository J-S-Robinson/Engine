using System;
using Stride.Engine;

namespace Engine
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            using var game = new MyGame();
            game.Run();
        }
    }
}
