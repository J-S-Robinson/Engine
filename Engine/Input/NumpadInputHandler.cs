using System;
using Engine.Game;
using Stride.Input;

namespace Engine.Input
{
    public class NumpadInputHandler
    {
        public void CollectPressedMoves(InputManager input, Action<MoveCommand> onMove)
        {
            if (input.IsKeyPressed(Keys.NumPad7)) onMove(new MoveCommand(-1, -1));
            if (input.IsKeyPressed(Keys.NumPad8)) onMove(new MoveCommand(-1, 0));
            if (input.IsKeyPressed(Keys.NumPad9)) onMove(new MoveCommand(-1, 1));

            if (input.IsKeyPressed(Keys.NumPad4)) onMove(new MoveCommand(0, -1));
            if (input.IsKeyPressed(Keys.NumPad6)) onMove(new MoveCommand(0, 1));

            if (input.IsKeyPressed(Keys.NumPad1)) onMove(new MoveCommand(1, -1));
            if (input.IsKeyPressed(Keys.NumPad2)) onMove(new MoveCommand(1, 0));
            if (input.IsKeyPressed(Keys.NumPad3)) onMove(new MoveCommand(1, 1));
        }
    }
}