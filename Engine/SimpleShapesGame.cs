using Engine.Animation;
using Engine.Game;
using Engine.Graphics;
using Engine.Input;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using StrideGame = Stride.Engine.Game;
namespace Engine
{
    public class MyGame : StrideGame
    {
        private readonly GameState _state = new GameState();
        private readonly NumpadInputHandler _inputHandler = new NumpadInputHandler();
        private readonly MoveAnimator _animator = new MoveAnimator();
        private readonly BoardRenderer _renderer = new BoardRenderer();

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            Window.AllowUserResizing = false;
            Window.Title = "Simple Chess RPG";
            Window.SetSize(new Int2(GameConfig.Step * GameConfig.BoardCols, GameConfig.Step * GameConfig.BoardRows));
            Window.PreferredWindowedSize = new Int2(GameConfig.Step * GameConfig.BoardCols, GameConfig.Step * GameConfig.BoardRows);

            _renderer.Load(GraphicsDevice, GraphicsContext);
            _animator.SnapToState(_state);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            _inputHandler.CollectPressedMoves(Input, move => _animator.QueueOrStartMove(_state, move));
            _animator.Update(gameTime, _state);
        }

        protected override void Draw(GameTime gameTime)
        {
            _renderer.Draw(GraphicsContext, GraphicsDevice, _animator.CirclePosition);

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _renderer.Unload();
            base.UnloadContent();
        }
    }
}