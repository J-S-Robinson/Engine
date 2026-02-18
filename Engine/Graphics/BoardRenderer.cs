using Engine.Game;
using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Engine.Graphics
{
    public class BoardRenderer
    {
        private SpriteBatch? _spriteBatch;
        private Texture? _whiteTex;
        private Texture? _circleTex;

        public void Load(GraphicsDevice graphicsDevice, GraphicsContext graphicsContext)
        {
            _spriteBatch = new SpriteBatch(graphicsDevice);

            _whiteTex = Texture.New2D(
                graphicsDevice,
                width: 1,
                height: 1,
                mipCount: 1,
                format: PixelFormat.R8G8B8A8_UNorm,
                textureFlags: TextureFlags.ShaderResource,
                usage: GraphicsResourceUsage.Dynamic);
            _whiteTex.SetData(graphicsContext.CommandList, new byte[] { 255, 255, 255, 255 });

            int diameter = GameConfig.CircleDiameter;
            _circleTex = Texture.New2D(
                graphicsDevice,
                width: diameter,
                height: diameter,
                mipCount: 1,
                format: PixelFormat.R8G8B8A8_UNorm,
                textureFlags: TextureFlags.ShaderResource,
                usage: GraphicsResourceUsage.Dynamic);

            var circleBytes = CircleTextureGenerator.MakeFilledCircleRgba(diameter);
            _circleTex.SetData(graphicsContext.CommandList, circleBytes);
        }

        public void Draw(GraphicsContext graphicsContext, GraphicsDevice graphicsDevice, Vector2 circlePos, Vector2 opponentPos, Color playerTint, Color opponentTint)
        {
            graphicsContext.CommandList.Clear(
                graphicsDevice.Presenter.BackBuffer,
                new Color4(0.0f, 0.0f, 0.0f, 1f));
            graphicsContext.CommandList.Clear(
                graphicsDevice.Presenter.DepthStencilBuffer,
                DepthStencilClearOptions.DepthBuffer);

            _spriteBatch!.Begin(graphicsContext, blendState: BlendStates.NonPremultiplied);

            var squarePos = new Vector2(0, 0);
            var squareSize = new Vector2(GameConfig.Step, GameConfig.Step);
            for (int row = 0; row < GameConfig.BoardRows; row++)
            {
                for (int col = 0; col < GameConfig.BoardCols; col++)
                {
                    var color = ((row + col) % 2 == 0) ? GameConfig.LightBlue : GameConfig.DarkBlue;
                    var x = squarePos.X + col * squareSize.X;
                    var y = squarePos.Y + row * squareSize.Y;
                    _spriteBatch.Draw(_whiteTex!, new RectangleF(x, y, squareSize.X, squareSize.Y), color);
                }
            }

            // Draw opponent (muted red) then player (green)
            _spriteBatch.Draw(_circleTex!, opponentPos, opponentTint);
            _spriteBatch.Draw(_circleTex!, circlePos, playerTint);
            _spriteBatch.End();
        }

        public void Unload()
        {
            _circleTex?.Dispose();
            _whiteTex?.Dispose();
            _spriteBatch?.Dispose();
        }

    }
}