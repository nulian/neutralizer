using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Neutralizer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont hudFont;
        private SpriteFont smallHudFont;
        private Texture2D status = null;
        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;
        private Texture2D splashScreen;
        private Texture2D hintScreen;
        private Song backgroundMusic;
        // Meta-level game state.
        private int levelIndex = -1;
        private Level level;
        private bool wasContinuePressed;
        private bool wasResetPressed;
        
        private bool lastLevel = false;
        private bool youWin = false;
        private bool gameStarted = false;
        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);
        private bool wasStartPressed;
        private const int TargetFrameRate = 60;
        private const int BackBufferWidth = 1280;
        private const int BackBufferHeight = 720;
        private const Buttons ContinueButton = Buttons.A;

        public long PassedTime
        {
            get { return passedTime; }
        }
        long passedTime;

        public PlatformerGame()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = BackBufferWidth;
            graphics.PreferredBackBufferHeight = BackBufferHeight;
            //graphics.IsFullScreen = true;
            Content.RootDirectory = "Content";

            // Framerate differs between platforms.
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / TargetFrameRate);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");
            smallHudFont = Content.Load<SpriteFont>("Fonts/SmallFontHud");

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");

            splashScreen = Content.Load<Texture2D>("Splashscreen/TitleScreen");
            backgroundMusic = Content.Load<Song>("Sounds/BGM");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(Content.Load<Song>("Sounds/BGM"));

            LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            if (!level.Player.IsAlive)
            {
                MediaPlayer.Pause();
            }
            if (level.ReachedExit)
            {
                MediaPlayer.Pause();
            }
            if (gameStarted)
            {
                HandleInput();
                if (!youWin)
                    passedTime += gameTime.ElapsedGameTime.Ticks;
                level.Update(gameTime);
            }
            else {
                KeyboardState keyboardState = Keyboard.GetState();
                GamePadState gamepadState = GamePad.GetState(PlayerIndex.One);
                //if (keyboardState.IsKeyDown(Keys.Escape))
                //    Exit();
                if (gamepadState.Buttons.Back == ButtonState.Pressed || keyboardState.IsKeyDown(Keys.R))
                {
                    levelIndex = -1;
                    passedTime = 0;
                    LoadNextLevel();
                }
                if (keyboardState.IsKeyDown(Keys.Enter))
                    gameStarted = true;
                bool startPressed = gamepadState.IsButtonDown(Buttons.Start);
                if (!wasStartPressed && startPressed)
                    gameStarted = true;
                wasStartPressed = startPressed;
            }
            base.Update(gameTime);
        }

        private void HandleInput()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamepadState = GamePad.GetState(PlayerIndex.One);

            // Exit the game when back is pressed.
            if (keyboardState.IsKeyDown(Keys.Escape) )
                gameStarted = false;
            bool startPressed = gamepadState.IsButtonDown(Buttons.Start);

            bool resetPressed =
                keyboardState.IsKeyDown(Keys.R) |
                gamepadState.IsButtonDown(Buttons.Back);
            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Space) ||
                gamepadState.IsButtonDown(ContinueButton);

            if (!level.ReachedExit && level.Player.IsAlive)
            {
                if (keyboardState.IsKeyDown(Keys.H) || gamepadState.IsButtonDown(Buttons.Y))
                    status = hintScreen;
                else
                    status = null;
            }
            if (!wasStartPressed && startPressed)
                gameStarted = false;
            if (!wasResetPressed && resetPressed)
            {
                ReloadCurrentLevel();
            }

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    MediaPlayer.Resume();
                    ReloadCurrentLevel();

                }
                else if (level.ReachedExit && youWin)
                {
                    passedTime = 0;
                    MediaPlayer.Resume();
                    LoadNextLevel();
                }
                else   if (level.ReachedExit)
                {
                    MediaPlayer.Resume();   
                    LoadNextLevel();
                }
            }
            wasResetPressed = resetPressed;
            wasContinuePressed = continuePressed;
            wasStartPressed = startPressed;
        }

        private void LoadNextLevel()
        {

            // Find the path of the next level.
            string levelPath;
            string levelPath2;
            int oldLevelIndex = levelIndex;
            if (lastLevel)
            {
                lastLevel = false;
                youWin = false;
            }
            // Loop here so we can try again when we can't find a level.
            while (true)
            {
                // Try to find the next level. They are sequentially numbered txt files.
                levelPath = String.Format("Levels/{0}.txt", ++levelIndex);
                levelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/" + levelPath);
                levelPath2 = "";
                levelPath2 = String.Format("Levels/{0}.txt", levelIndex+1);
                levelPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/" + levelPath2);
                
                if (File.Exists(levelPath))
                    break;

                // If there isn't even a level 0, something has gone wrong.
                if (levelIndex == 0)
                    throw new Exception("No levels found.");

                // Whenever we can't find a level, start over again at 0.
                levelIndex = -1;
            }
            if (levelIndex != -1)
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/Hints/Level" + levelIndex + ".xnb")))
                    hintScreen = Content.Load<Texture2D>("Hints/Level" + levelIndex);
                else
                    hintScreen = null;

            if (File.Exists(levelPath2))
                winOverlay = Content.Load<Texture2D>("Overlays/you_next");
            else
            {
                winOverlay = Content.Load<Texture2D>("Overlays/you_win");
                lastLevel = true;
            }
            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            level = new Level(this,Services, levelPath);
        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            if (gameStarted)
            {
                level.Draw(gameTime, spriteBatch);

                DrawHud();
            }
            else
                spriteBatch.Draw(splashScreen, new Vector2(0, 0), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);
            Vector2 centerText = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.5f,
                                         titleSafeArea.Y + 29);
            Vector2 right = new Vector2(titleSafeArea.X + titleSafeArea.Width /1.4f,
                                         titleSafeArea.Y + 13);
            TimeSpan timeDone = new TimeSpan(PassedTime);
            string timeString = "TIME: " + timeDone.Minutes + ':' + String.Format("{0:00}", timeDone.Seconds);
            // Drawing the score
            DrawShadowedString(smallHudFont, "Press \"Y\" for help", centerText, Color.White);
            DrawShadowedString(hudFont, timeString, right, Color.White);



            // Determine the status overlay message to show.
            

            if (level.ReachedExit && lastLevel)
            {
                youWin = true;
                status = winOverlay;
          
            }
            else if (level.ReachedExit)
            {
                status = winOverlay;
            }
            else if (!level.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
    }
}
