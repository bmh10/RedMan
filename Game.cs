using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Storage;
using System.Xml.Serialization;



namespace RedMan
{
    /*
     * Main game class controlling game state and level loading
     */
    public class Game : Microsoft.Xna.Framework.Game
    {
        //** Drawing resources **\\
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        //** Global Content **\\
        DateTime dateTime;
        public static SpriteFont hudFont, smallFont, bigFont;
        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;
        private Texture2D hudBackground;
        Typewriter typewriter;
        private Texture2D crosshair;
        private Vector2 crosshairPosition;

        //** Meta-level game state. **\\
        private int levelIndex = -2;
        private Level level;
        private MainMenu mainMenu;
        private bool wasContinuePressed;
        public bool inMainMenu;

        public static bool soundOn = true;
        public static bool paused = false;

        private bool escWasReleased;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        // We store our input states so that we only poll once per frame, 
        // then we use the same input state wherever needed
        private KeyboardState keyboardState;
        private MouseState mouseState;
        
        // The number of levels in the Levels directory of our content. We assume that
        // levels in our content are 0-based and that all numbers under this constant
        // have a level file present. This allows us to not need to check for the file
        // or handle exceptions, both of which can add unnecessary time to level loading.
        public static int numberOfLevels = 14;

        public Game()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferWidth = 1000;
            graphics.PreferredBackBufferHeight = 800;

            graphics.IsFullScreen = true;

            Window.Title = "RedMan";

            this.IsMouseVisible = false;

            Level.FurthestLevelReached = 0;
        }

        // LoadContent will be called once per game
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");
            smallFont = Content.Load<SpriteFont>("Fonts/smallFont");
            bigFont = Content.Load<SpriteFont>("Fonts/bigFont");

            // Credit typewriter for last level
            //typewriter = new Typewriter(this);

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");

            hudBackground = Content.Load<Texture2D>("Overlays/hudBackground");
            
            //Known issue that you get exceptions if you use Media PLayer while connected to your PC
            //See http://social.msdn.microsoft.com/Forums/en/windowsphone7series/thread/c8a243d2-d360-46b1-96bd-62b1ef268c66
            //Which means its impossible to test this from VS.
            //So we have to catch the exception and throw it away
            try
            {
                MediaPlayer.IsRepeating = true;
                MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));
            }
            catch { }

            // What to load when game first opens
            //LoadMainWorld();
            //LoadLevel(12);
            LoadMainMenu();
        }

        // Loads correct crosshair depending on level
        protected void LoadCrossHair()
        {
            if (levelIndex == 9)
                crosshair = Content.Load<Texture2D>("Sprites/crosshair2");
            else
                crosshair = Content.Load<Texture2D>("Sprites/crosshair");
        }

        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        protected override void Update(GameTime gameTime)
        {
            // If user opens another window (i.e. game window is not active) pause game until window becomes active again
            if (!this.IsActive)
                return;

            dateTime = DateTime.Now;

            // Get all of our input states
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();

            if (inMainMenu)
                mainMenu.Update(gameTime, keyboardState, mouseState);
            // Otherwise update level
            else
            {
                if (level.Player.atDoor)
                {
                    LoadLevel(level.Player.levelToLoad);
                    level.Player.atDoor = false;
                }

                // Handle polling for our input and handling high-level input
                HandleInput();

                // Update our level, passing down the GameTime along with all input states
                level.Update(gameTime, keyboardState, mouseState, Window.CurrentOrientation);

                // Update credit typewriter if last level
                if (levelIndex == 13)
                    typewriter.Update(gameTime);

                base.Update(gameTime);
            }
        }

        private void HandleInput()
        {
            Viewport vp = spriteBatch.GraphicsDevice.Viewport;

            // If not in main world
            if (levelIndex != -1)
            {
                // Get crosshair position from mouse position
                if (mouseState.X >= 0 && mouseState.X < vp.Width && mouseState.Y >= 0 && mouseState.Y < vp.Height)
                    crosshairPosition = new Vector2(mouseState.X - crosshair.Width / 2, mouseState.Y - crosshair.Height / 2);
            }

            if (keyboardState.IsKeyDown(Keys.Escape) && escWasReleased)
            {
                escWasReleased = false;
                if (level.LevelIndex == -1)
                {
                    DoSaveGame();
                    LoadMainMenu();
                }
                else
                    LoadMainWorld();
            }

            escWasReleased = keyboardState.IsKeyUp(Keys.Escape);

            bool continuePressed =
                keyboardState.IsKeyDown(Keys.Enter);

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    level.StartNewLife(); // Without gems back in place
                }
                else if (level.TimeRemaining == TimeSpan.Zero)
                {
                    if (level.ReachedExit)
                    {
                        // Add points from this level onto total points count
                        Player.Score += level.Score;

                        // If completed last level save game and go back to main menu (and don't increment furthestLevelReached)
                        if (levelIndex == 13)
                        {
                            DoSaveGame();
                            LoadMainMenu();
                        }
                        else
                        {
                            // If completed a level which hasn't already been completed
                            if (levelIndex >= Level.FurthestLevelReached)
                                Level.FurthestLevelReached++;
                            // If on sniper level part 1 or last boss, load next level immediately.
                            // Otherwise go back to main world
                            if (levelIndex == 9 || levelIndex == 12)
                                LoadNextLevel();
                            else
                                LoadMainWorld();
                        }
                    }
                    else
                        ReloadCurrentLevel(); // With gems back in place
                }
            }
            wasContinuePressed = continuePressed;
        }


        // Loads the game's main menu
        private void LoadMainMenu()
        {
            mainMenu = new MainMenu(this, spriteBatch);
            inMainMenu = true;
        }

        private void LoadNextLevel()
        {
            // Move to the next level
            levelIndex = (levelIndex + 1) % numberOfLevels;

            //Level.LoadFromSubLevel = false; --> might need to put this back in

            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            string levelPath = string.Format("Content/Levels/{0}.txt", levelIndex);

            using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                level = new Level(Services, fileStream, levelIndex);

            LoadCrossHair();

            // Credit typewriter for last level
            if (levelIndex == 13)
                typewriter = new Typewriter(this);
        }


        // Loads a specified level
        public void LoadLevel(int index)
        {
            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            if (index < -1)
                ReloadCurrentLevel();
            else
            {
                levelIndex = index;
                string levelPath = "Content/Levels/" + levelIndex.ToString() + ".txt";

                using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                    level = new Level(Services, fileStream, levelIndex);
            }
            Level.LoadMainWorldFromLevel = true;
            LoadCrossHair();

            // Credit typewriter for last level
            if (index == 13)
                typewriter = new Typewriter(this);
        }

        // Loads main world level and puts player outside level just completed (or exited)
        public void LoadMainWorld()
        {
             // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            levelIndex = -1;
            string levelPath = "Content/Levels/" + levelIndex.ToString() + ".txt";

            using (Stream fileStream = TitleContainer.OpenStream(levelPath))
                level = new Level(Services, fileStream, levelIndex);

        }

        private void ReloadCurrentLevel()
        {
            --levelIndex;
            LoadNextLevel();
        }

        /// Draws the game from background to foreground.
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);

            if (inMainMenu)
                mainMenu.Draw(gameTime);
            else
            {
                // Draws all level tiles, gems, enemies and player - also moves camera according to player position
                level.Draw(gameTime, spriteBatch);

                // Draws score and time remaining (calls spriteBatch.begin()/end() within)
                DrawHud();
            }

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            spriteBatch.Begin();
           

            // If player shooting with mouse turn crosshair on
            if (level.Player.shootWithMouse)
                spriteBatch.Draw(crosshair, crosshairPosition, Color.White);

            Viewport vp = GraphicsDevice.Viewport;
            Rectangle titleSafeArea = vp.TitleSafeArea;
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            Vector2 hudLocation = new Vector2(10, hudBackground.Height/2 - hudFont.LineSpacing/2);

            spriteBatch.Draw(hudBackground, new Vector2(0), Color.White);
            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            string timeString;
            if (levelIndex != -1)
                timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            else
                timeString = "TIME: " + dateTime.ToShortTimeString();
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }

            if (!level.DoorTimerOn)//.CountDownTimerOn)
                DrawShadowedString(hudFont, timeString, hudLocation, timeColor);
            else
            {
                string str = level.DoorTimer.Seconds.ToString();
                float timeWidth = hudFont.MeasureString(str).X;
                spriteBatch.DrawString(bigFont, str, new Vector2((vp.Width - timeWidth) / 2, vp.Height / 8), timeColor);
            }

            // Draws info and level start string
            level.Player.DrawInfoString(spriteBatch);

            // Draw score
            int score = (levelIndex == -1) ? Player.Score : level.Score;
            DrawShadowedString(hudFont, "SCORE: " + score, hudLocation + new Vector2(200, 0), Color.Yellow);

            // Draw current weapon 
            int powerBullets = level.Player.powerBulletCount;
            // Draw number of power bullets if have any
            if (powerBullets > 0)
                DrawShadowedString(hudFont, "Power Bullets: " + powerBullets, hudLocation + new Vector2(350, 0), Color.Yellow);
            else
            {
                string weapon;
                if (level.Player.hasGun)
                    weapon = level.Player.currGun.ToString();
                else
                    weapon = "None";
                DrawShadowedString(hudFont, "Current Weapon: " + weapon, hudLocation + new Vector2(350, 0), Color.Yellow);
            }

            DrawLevelSpecificInfo(hudLocation + new Vector2(700, 0));

            if (levelIndex == 9)
                DrawSniperStats();

            if (paused)
            {
                String str = "Paused";
                String str2 = "(Press ESC to quit)";
                DrawShadowedString(bigFont, str, new Vector2((vp.Width - bigFont.MeasureString(str).X) / 2, (vp.Height - bigFont.MeasureString(str).Y) / 2), Color.Yellow);
                DrawShadowedString(hudFont, str2, new Vector2((vp.Width - hudFont.MeasureString(str2).X) / 2, (vp.Height - hudFont.MeasureString(str2).Y) / 2 + 50), Color.Yellow);
            }

            // Determine the status overlay message to show.
            Texture2D status = null;
            if (level.TimeRemaining == TimeSpan.Zero)
            {
                if (level.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
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

            if (levelIndex == 13)
                typewriter.Draw(spriteBatch);
           

            spriteBatch.End();
        }

           // Draws info specific to levels within the HUD display
        private void DrawLevelSpecificInfo(Vector2 location)
        {
            switch (levelIndex)
            {
                // Weapons training
                case 1:
                    DrawShadowedString(hudFont, "Targets: " + Bullet.Level1TargetCount + "/" + Bullet.Level1TargetCountMax, location, Color.Yellow);
                    break;
                // Water level
                case 2:
                    DrawShadowedString(hudFont, "Amulets Collected: " + level.Player.level2AmuletCount + "/" + level.Player.level2AmuletMax, location, Color.Yellow);
                    break; 
                // Space level
                case 4:
                    if (level.Player.showStats)
                        DrawShadowedString(hudFont, "Stars Collected: " + level.Player.level4StarCount + "/" + level.Player.level4StarMax, location, Color.Yellow);
                    break;
                // Haunted house level
                case 6:
                    DrawShadowedString(hudFont, "Pumpkins Collected: " + level.Player.level6PumpkinCount + "/" + level.Player.level6PumpkinMax, location, Color.Yellow);
                    break;
                // Heaven level
                case 11:
                    DrawShadowedString(hudFont, "Stars Collected: " + level.Player.level11StarCount + "/" + level.Player.level11StarMax, location, Color.Yellow);
                    break; 
            }
        }
    
        // Draws two strings slightly offset to create a shadowed effect
        public void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Red);
            spriteBatch.DrawString(font, value, position, color);
        }


        private void DrawSniperStats()
        {
            DrawSniperString("Co-ords: " + level.Player.mousePos.X + ", " + level.Player.mousePos.Y, 0);
            DrawSniperString("Shots fired: " + Bullet.shotsFired, 10);
            DrawSniperString("Shots on target: " + Bullet.shotsOnTarget, 20);
            DrawSniperString("Accuracy: " + Bullet.accuracyPercentage + "%", 30);
            DrawSniperString("Enemies: " + Bullet.enemiesKilled + " / " + Bullet.enemiesMax, 50);
            DrawSniperString("Targets: " + Bullet.targetsHit + " / " + Bullet.targetMax, 60);

            // add wind direction later
        }

        private void DrawSniperString(string value, int yOffset)
        {
            float strWidth = smallFont.MeasureString(value).X;
            spriteBatch.DrawString(smallFont, value, level.Player.mousePos + new Vector2(-(strWidth / 2), 170 + yOffset), Color.Green);
        }


        public static void PlaySound(SoundEffect sound)
        {
            if (soundOn)
                sound.Play();
        }

        #region Save/Load Game

        [Serializable]
        public struct SaveGameData
        {
            public int furthestLevelReached;
            public int Score;
        }


        // This method serializes a data object into
        // the StorageContainer for this game.
        public static void DoSaveGame()
        {
            IAsyncResult r = StorageDevice.BeginShowSelector(
                         PlayerIndex.One, null, null);
            StorageDevice device = StorageDevice.EndShowSelector(r);

            // Create the data to save.
            SaveGameData data = new SaveGameData();
            data.furthestLevelReached = Level.FurthestLevelReached;
            data.Score = Player.Score;

            // Open a storage container.
            IAsyncResult result =
                device.BeginOpenContainer("SavedGames", null, null);

            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();

            StorageContainer container = device.EndOpenContainer(result);

            // Close the wait handle.
            result.AsyncWaitHandle.Close();

            string filename = "savegame.sav";

            // Check to see whether the save exists.
            if (container.FileExists(filename))
                // Delete it so that we can create one fresh.
                container.DeleteFile(filename);

            // Create the file.
            Stream stream = container.CreateFile(filename);

            // Convert the object to XML data and put it in the stream.
            XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
            serializer.Serialize(stream, data);

            // Close the file.
            stream.Close();

            // Dispose the container, to commit changes.
            container.Dispose();
        }


        // This method loads a serialized data object
        // from the StorageContainer for this game.
        public static void DoLoadGame()//StorageDevice device)
        {
            IAsyncResult r = StorageDevice.BeginShowSelector(
                           PlayerIndex.One, null, null);
            StorageDevice device = StorageDevice.EndShowSelector(r);// .new StorageDevice();
            
            // Open a storage container.
            IAsyncResult result =
                device.BeginOpenContainer("SavedGames", null, null);

            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();

            StorageContainer container = device.EndOpenContainer(result);

            // Close the wait handle.
            result.AsyncWaitHandle.Close();

            string filename = "savegame.sav";

            // Check to see whether the save exists.
            if (!container.FileExists(filename))
            {
                // If not, dispose of the container and return.
                container.Dispose();
                return;
            }

            // Open the file.
            Stream stream = container.OpenFile(filename, FileMode.Open);

            // Read the data from the file.
            XmlSerializer serializer = new XmlSerializer(typeof(SaveGameData));
            SaveGameData data = (SaveGameData)serializer.Deserialize(stream);

            Level.FurthestLevelReached = data.furthestLevelReached;
            Player.Score = data.Score;
            // Close the file.
            stream.Close();

            // Dispose the container.
            container.Dispose();

            // Report the data to the console.
            //Debug.WriteLine("Name:     " + data.PlayerName);
            //Debug.WriteLine("Level:    " + data.Level.ToString());
            //Debug.WriteLine("Score:    " + data.Score.ToString());
            //Debug.WriteLine("Position: " + data.AvatarPosition.ToString());
            //Level.FurthestLevelReached = data.furthestLevelReached;
        }

        #endregion
    }
}

#region Entry Point

namespace RedMan
{
#if WINDOWS || XBOX
    static class Program
    {
        // The main entry point for the application.
        static void Main(string[] args)
        {
            using (Game game = new Game())
            {
                game.Run();
            }
        }
    }
#endif
}

#endregion
