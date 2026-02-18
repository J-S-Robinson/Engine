using Stride.Core.Mathematics;
using Stride.Engine;        // Game
using Stride.Games;         // GameTime
using Stride.Graphics;      // SpriteBatch, Texture, PixelFormat, RectangleF
using System;
using System.Collections.Generic;
using Stride.Input;         // Keyboard, Keys
namespace Engine
{
    public class MyGame : Game
    {
        private SpriteBatch? _spriteBatch; // initialized in LoadContent()
        private Texture? _whiteTex;   // 1x1 white pixel for rectangles (initialized in LoadContent())
        private Texture? _circleTex;  // RGBA circle with alpha (initialized in LoadContent())

        private const int Step = 150; // how much the circle moves per numpad press (matches square size)
        private const int CircleDiameter = 140; // size of the circle texture
        private const int CircleOffset = (Step - CircleDiameter) / 2; // center the circle inside a square
        private const int BoardCols = 4;
        private const int BoardRows = 4;

        private int _circleIndex = 0; // index 0..15 (row-major, top-left = 0)
        private Vector2 _circlePos = new Vector2(CircleOffset, CircleOffset); // computed from index

        // Animation state for smooth moves
        private bool _isAnimating = false;
        private Vector2 _animStartPos = Vector2.Zero;
        private Vector2 _animTargetPos = Vector2.Zero;
        private float _animElapsed = 0f;
        private const float AnimDuration = 0.5f; // seconds (half-second)

        // Input queueing (allows queuing moves while animating)
        private readonly Queue<(int dRow, int dCol)> _moveQueue = new Queue<(int dRow, int dCol)>();
        private const int MaxQueuedMoves = 8;
        // Stride 4.x commonly uses the async LoadContent signature
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Example window configuration (optional)
            Window.AllowUserResizing = false;                 // optional
            Window.Title = "Simple Chess RPG";              // optional

            // Method A: set current window client size immediately
            Window.SetSize(new Int2(Step * BoardCols, Step * BoardRows));

            // Method B: preferred windowed size (used when switching out of fullscreen)
            Window.PreferredWindowedSize = new Int2(Step * BoardCols, Step * BoardRows);

            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // --- 1x1 white texture (we scale it to draw a solid rectangle) ---
            _whiteTex = Texture.New2D(
                GraphicsDevice,
                width: 1, height: 1,
                mipCount: 1,
                format: PixelFormat.R8G8B8A8_UNorm,
                textureFlags: TextureFlags.ShaderResource,
                // Dynamic is convenient when you plan to SetData on it
                usage: GraphicsResourceUsage.Dynamic
            );
            _whiteTex.SetData(GraphicsContext.CommandList, new byte[] { 255, 255, 255, 255 });

            // --- Circle texture with alpha ---
            int diameter = CircleDiameter;
            _circleTex = Texture.New2D(
                GraphicsDevice,
                width: diameter, height: diameter,
                mipCount: 1,
                format: PixelFormat.R8G8B8A8_UNorm,
                textureFlags: TextureFlags.ShaderResource,
                usage: GraphicsResourceUsage.Dynamic
            );

            var circleBytes = MakeFilledCircleRgba(diameter);
            _circleTex.SetData(GraphicsContext.CommandList, circleBytes);

            // Ensure circle position matches internal index after content load
            UpdateCirclePosFromIndex();
        }


        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            HandleNumpadSnapMoves();
            UpdateAnimation(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Clear
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Color4(0.0f, 0.0f, 0.0f, 1f));
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            // IMPORTANT in Stride 4.x: pass GraphicsContext to Begin
            _spriteBatch!.Begin(GraphicsContext, blendState: BlendStates.NonPremultiplied);

            // Chessboard: draw 4x4 squares alternating between blue and darker blue
            var squarePos  = new Vector2(0, 0);
            var squareSize = new Vector2(Step, Step);
            var lightBlue = new Color(0x4A, 0x90, 0xE2, 255); // blue-ish
            var darkBlue  = new Color(0x2A, 0x60, 0xA8, 255); // darker blue
            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    var color = ((row + col) % 2 == 0) ? lightBlue : darkBlue;
                    var x = squarePos.X + col * squareSize.X;
                    var y = squarePos.Y + row * squareSize.Y;
                    _spriteBatch!.Draw(_whiteTex!, new RectangleF(x, y, squareSize.X, squareSize.Y), color);
                }
            }

            // Circle: draw the 140x140 texture at a position (or pass RectangleF to scale)
            _spriteBatch!.Draw(_circleTex!, _circlePos, new Color(0x7B, 0xD8, 0x8F, 255));  // green tint

            _spriteBatch!.End();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _circleTex?.Dispose();
            _whiteTex?.Dispose();
            _spriteBatch?.Dispose();
            base.UnloadContent();
        }

        private static byte[] MakeFilledCircleRgba(int diameter)
        {
            var bytes = new byte[diameter * diameter * 4];

            float r  = diameter / 2f;
            float cx = r, cy = r;
            float r2 = r * r;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x - cx + 0.5f;
                    float dy = y - cy + 0.5f;
                    bool inside = (dx * dx + dy * dy) <= r2;

                    int i = (y * diameter + x) * 4;
                    if (inside)
                    {
                        bytes[i + 0] = 255; // R
                        bytes[i + 1] = 255; // G
                        bytes[i + 2] = 255; // B
                        bytes[i + 3] = 255; // A
                    }
                    else
                    {
                        bytes[i + 0] = 0;   // R
                        bytes[i + 1] = 0;   // G
                        bytes[i + 2] = 0;   // B
                        bytes[i + 3] = 0;   // A
                    }
                }
            }
            return bytes;
        }
    
        private void UpdateCirclePosFromIndex()
        {
            int row = _circleIndex / BoardCols;
            int col = _circleIndex % BoardCols;
            _circlePos = new Vector2(col * Step + CircleOffset, row * Step + CircleOffset);
            _isAnimating = false; // ensure any animation state is cleared when snapping
        }

        private void UpdateAnimation(GameTime gameTime)
        {
            if (!_isAnimating) return;

            _animElapsed += (float)gameTime.Elapsed.TotalSeconds;
            var t = Math.Min(_animElapsed / AnimDuration, 1f);
            var eased = EaseOutCubic(t);
            _circlePos = Vector2.Lerp(_animStartPos, _animTargetPos, eased);

            if (t >= 1f)
            {
                // Snap to exact target at end of this segment
                _circlePos = _animTargetPos;

                // If there are queued moves, start the next animation immediately
                if (_moveQueue.Count > 0)
                {
                    var next = _moveQueue.Dequeue();
                    int row = _circleIndex / BoardCols;
                    int col = _circleIndex % BoardCols;
                    int newRow = row + next.dRow;
                    int newCol = col + next.dCol;

                    _circleIndex = newRow * BoardCols + newCol;
                    _animStartPos = _circlePos;
                    _animTargetPos = new Vector2(newCol * Step + CircleOffset, newRow * Step + CircleOffset);
                    _animElapsed = 0f;
                    _isAnimating = true;
                }
                else
                {
                    _isAnimating = false;
                }
            }
        }

        private static float EaseOutCubic(float t) => 1f - (1f - t) * (1f - t) * (1f - t);

        private void TryMoveIndexBy(int dRow, int dCol)
        {
            int row = _circleIndex / BoardCols;
            int col = _circleIndex % BoardCols;

            // If currently animating, validate against the predicted position (current index + queued moves)
            if (_isAnimating)
            {
                int predRow = row;
                int predCol = col;
                foreach (var q in _moveQueue)
                {
                    predRow += q.dRow;
                    predCol += q.dCol;
                }

                int newPredRow = predRow + dRow;
                int newPredCol = predCol + dCol;
                if (newPredRow < 0 || newPredRow >= BoardRows || newPredCol < 0 || newPredCol >= BoardCols)
                    return; // invalid for predicted position

                if (_moveQueue.Count >= MaxQueuedMoves)
                    return; // queue full

                _moveQueue.Enqueue((dRow, dCol));
                return;
            }

            // Not animating: perform move immediately (start animation)
            int newRow = row + dRow;
            int newCol = col + dCol;
            if (newRow < 0 || newRow >= BoardRows || newCol < 0 || newCol >= BoardCols)
                return; // invalid move

            _circleIndex = newRow * BoardCols + newCol;

            // Start animation from current position to the new cell's center
            _animStartPos = _circlePos;
            _animTargetPos = new Vector2(newCol * Step + CircleOffset, newRow * Step + CircleOffset);
            _animElapsed = 0f;
            _isAnimating = true;
        }

        private void HandleNumpadSnapMoves()
        {
            // Map each numpad press to a board step (row, col). Moves that would leave the board are ignored.
            if (Input.IsKeyPressed(Keys.NumPad7)) TryMoveIndexBy(-1, -1);
            if (Input.IsKeyPressed(Keys.NumPad8)) TryMoveIndexBy(-1,  0);
            if (Input.IsKeyPressed(Keys.NumPad9)) TryMoveIndexBy(-1,  1);

            if (Input.IsKeyPressed(Keys.NumPad4)) TryMoveIndexBy( 0, -1);
            // NumPad5 intentionally does nothing
            if (Input.IsKeyPressed(Keys.NumPad6)) TryMoveIndexBy( 0,  1);

            if (Input.IsKeyPressed(Keys.NumPad1)) TryMoveIndexBy( 1, -1);
            if (Input.IsKeyPressed(Keys.NumPad2)) TryMoveIndexBy( 1,  0);
            if (Input.IsKeyPressed(Keys.NumPad3)) TryMoveIndexBy( 1,  1);
        }
    }
}