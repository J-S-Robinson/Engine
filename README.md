# SimpleShapes Engine

Minimal Stride-based demo showing a 4×4 chessboard and a movable circle.

## Features
- 4×4 chessboard (alternating blue shades)
- Move the circle with numeric keypad (numpad) keys
- Smooth 0.5s ease-out movement with input queuing and boundary checks

## Quick start (Windows)
1. Build:
   ```
   dotnet build Engine/Engine.csproj -c Debug
   ```
2. Run the built executable:
   ```
   .\Engine\bin\Debug\net8.0\win-x64\Engine.exe
   ```

(You can also use `dotnet run --project Engine/Engine.csproj -c Debug -r win-x64`.)

## Controls
- Numpad 8 / 2 / 4 / 6 — move up / down / left / right
- Numpad 7 / 9 / 1 / 3 — diagonals
- Numpad 5 — no-op

## Development
- Main code: `Engine/SimpleShapesGame.cs`
- Board size, animation and behavior are implemented in that file.

---

No license specified.