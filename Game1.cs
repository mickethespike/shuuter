using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Particles;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using MonoGame.Extended.Particles.Profiles;
using MonoGame.Extended.Particles.Modifiers;
using MonoGame.Extended.TextureAtlases;
using System;
using System.Collections.Generic;
using System.IO;

namespace Shooter
{
    public class Player
    {
        public Vector2 Pos;
        public Vector2 Scale;

        public Player(Vector2 pos, Vector2 scale)
        {
            Pos = pos;
            Scale = scale;
        }
    }

    public class Game1 : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        TextureRegion2D pixel;
        Texture2D shipTexture;
        Texture2D enemyTexture;
        SpriteFont font;

        Vector2 shipTextureMiddle;
        Vector2 enemyScale = new Vector2(8);
        Vector2 fontSize;
        Vector2 fontOrigin;

        Player player = new Player(new Vector2(100, 20), new Vector2(8));

        List<Vector2> balls = new List<Vector2>(100);
        List<Vector2> enemies = new List<Vector2>(10);

        int _lives = 3;
        int _score = 0;
        int _highScore;
        bool gameOver = false;
        public int Lives { get => _lives; set { if (value < 1) { gameOver = true; } _lives = value; } }

        void Save()
        {
            using (var fileStream = new FileStream("totallynot.savedscore", FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                writer.Write(_highScore);
            }
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }
        
        protected override void Initialize()
        {
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            base.Initialize();

            player.Pos.X = 150;
            player.Pos.Y = GraphicsDevice.Viewport.Height / 2f - shipTextureMiddle.Y;
            fontSize = font.MeasureString("Game Over");
            fontOrigin = GraphicsDevice.Viewport.Bounds.Location.ToVector2() - fontSize / 2;

            var saveFile = new FileInfo("totallynot.savedscore");
            if (saveFile.Exists && saveFile.Length == 4)
            {
                using (BinaryReader reader = new BinaryReader(saveFile.OpenRead()))
                {
                    _highScore = reader.ReadInt32();
                }
            }

            Window.ClientSizeChanged += Window_ClientSizeChanged;
            Window_ClientSizeChanged(null, EventArgs.Empty);
        }

        private void Window_ClientSizeChanged(object s, EventArgs e)
        {
        }

        ParticleEmitter fartEmitter;

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            var pixelTex = new Texture2D(GraphicsDevice, 1, 1);
            pixelTex.SetData(new Color[] { Color.White });
            pixel = new TextureRegion2D(pixelTex);

            fartEmitter = new ParticleEmitter(pixel, 1000, TimeSpan.FromSeconds(1.66), Profile.Point())
            {
                AutoTrigger = false,
                Parameters =
                {
                    Quantity = new Range<int>(10, 12),
                    Scale = new Range<float>(6, 8),
                    Speed = new Range<float>(30, 80)
                },
                Modifiers =
                {
                    new AgeModifier
                    {
                        Interpolators =
                        {
                            new OpacityInterpolator
                            {
                                StartValue = 1f
                            }
                        }
                    },
                    new LinearGravityModifier
                    {
                        Direction = Vector2.UnitY,
                        Strength = 80
                    }
                }
            };
            

            shipTexture = Content.Load<Texture2D>("ship");
            shipTextureMiddle = shipTexture.Bounds.Size.ToVector2() / 2f;

            enemyTexture = Content.Load<Texture2D>("enemy");

            font = Content.Load<SpriteFont>("font");
        }

        protected override void UnloadContent()
        {
            Save();
        }

        const float speed = 250;
        const float eSpeed = 300; // enemy speed

        const float fires = 1f / 2;
        float timeout = 0;
        float eTimeout = 10f;

        Random random = new Random();

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            base.Update(gameTime);

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboardState = Keyboard.GetState();

            if (gameOver)
                return;

            if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                player.Pos.Y += delta * speed;

            if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                player.Pos.Y -= delta * speed;
            
            if (player.Pos.Y < shipTextureMiddle.Y * player.Scale.Y + 80)
                player.Pos.Y = shipTextureMiddle.Y * player.Scale.Y + 80;

            var viewHeight = GraphicsDevice.Viewport.Height;
            if (player.Pos.Y > viewHeight - shipTextureMiddle.Y * player.Scale.Y)
                player.Pos.Y = viewHeight - shipTextureMiddle.Y * player.Scale.Y;

            for (int i = enemies.Count; i-- > 0;)
            {
                Vector2 pos = enemies[i];

                pos.X -= delta * eSpeed;

                if (pos.X < -5 * enemyScale.X)
                {
                    enemies.RemoveAt(i);
                    Lives--;
                }
                else
                {
                    enemies[i] = pos;
                }
            }

            int enemyCount = enemies.Count;
            Vector2 shipSize = shipTexture.Bounds.Size.ToVector2();
            for (int i = balls.Count; i-- > 0;)
            {
                Vector2 pos = balls[i];

                pos.X += delta * speed;

                if (pos.X > GraphicsDevice.Viewport.Width)
                    balls.RemoveAt(i);
                else
                {
                    for (int j = enemyCount; j-- > 0;)
                    {
                        Vector2 enemyPos = enemies[j];
                        Vector2 enemySize = new Vector2(5f * enemyScale.X, 5f * enemyScale.Y);
                        if (new Rectangle((int)enemyPos.X, (int)enemyPos.Y, (int)enemySize.X, (int)enemySize.Y).Contains(pos))
                        {
                            fartEmitter.Trigger(enemyPos + enemySize / 2f);

                            enemies.RemoveAt(j);
                            balls.RemoveAt(i);
                            enemyCount--;
                            _score += 100;

                            if (_score > _highScore)
                            {
                                _highScore = _score;
                            }

                            goto LoopFace;
                        }
                    }

                    balls[i] = pos;
                    LoopFace: { }
                }
            }

            if (keyboardState.IsKeyDown(Keys.Space) && timeout <= 0)
            {
                balls.Add(player.Pos - new Vector2(
                    -shipTextureMiddle.X * player.Scale.X, shipTextureMiddle.Y));

                timeout = fires;
            }

            if (eTimeout <= 0)
            {
                enemies.Add(new Vector2(GraphicsDevice.Viewport.Width,
                    random.Next(80, (int)(viewHeight - enemyScale.Y * enemyTexture.Height))));
                eTimeout = 10f;
            }

            if (timeout >= 0)
                timeout -= delta;
            if (eTimeout >= 0)
                eTimeout -= 0.1f;

            fartEmitter.Update(delta);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            spriteBatch.Draw(shipTexture, player.Pos, null, Color.MediumAquamarine,
                0, shipTextureMiddle, player.Scale, SpriteEffects.None, 0);

            spriteBatch.DrawString(font, $"Lives: {Lives}", new Vector2(10, 5),
                Color.Red, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
            spriteBatch.DrawString(font, $"Score: {_score}", new Vector2(10, 35),
                Color.White, 0, Vector2.Zero, 0.5f, SpriteEffects.None, 0);

            for (int i = 0; i < balls.Count; i++)
            {
                spriteBatch.Draw(shipTexture, balls[i], null, Color.White);
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                spriteBatch.Draw(enemyTexture, enemies[i], null, scale: enemyScale);
            }

            spriteBatch.DrawString(font, _highScore.ToString(), new Vector2(GraphicsDevice.Viewport.Bounds.Size.ToVector2().X / 2 - font.MeasureString(_highScore.ToString()).X / 2, 10), Color.White);

            if (gameOver)
            {
                spriteBatch.DrawString(font, "Game Over",
                    GraphicsDevice.Viewport.Bounds.Size.ToVector2() / 2 - font.MeasureString("Game Over") / 2, Color.Red);
            }
            spriteBatch.Draw(fartEmitter);

            spriteBatch.End();
            base.Draw(gameTime);
        }
    }
}
