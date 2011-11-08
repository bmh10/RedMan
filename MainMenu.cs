using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using System.Net.Mail;


namespace RedMan
{
    /*
     * Implements main menu structure and and content
     */
    class MainMenu
    {
        enum Menu { Main, LoadGame, Options, Credits, Controls, Email, Check };
        enum MailStatus { Writing, NoSender, NoSubject, NoMessage, Sent, Failed };

        Menu currMenu;
        int selectedOption;
        float selectedYpos;
        Game game;
        SpriteBatch spriteBatch;
        Texture2D mouse, levelThumbnail, titleBanner;
        Point mousePos;
        AnimationPlayer animationPlayer;
        Animation walkAnimation;
        private SoundEffect changeSelectionSound;

        Viewport vp;

        private int numOfOptions;

        private bool mWasReleased, uWasReleased, dWasReleased, eWasReleased, escWasReleased;

        // Scrolling background textures
        Texture2D bg1, bg2, bg3, bg4, bg5;
        Vector2[] p = new Vector2[5];


        private List<KeyValuePair<Rectangle, int>> selectionRects;

        // Initial y position (i) and spacing (s) of menu options
        int i = 200;
        int s = 50;

        // For typing in text boxes
        MailStatus mailStatus;
        Rectangle nameR, subjectR, messageR;
        Rectangle currSelected;
        KeyboardStringReader textReader = new KeyboardStringReader();
        StringBuilder emailText = new StringBuilder();
        StringBuilder subjectText = new StringBuilder();
        StringBuilder messageText = new StringBuilder();


        public MainMenu(Game game, SpriteBatch spriteBatch)
        {
            this.game = game;
            this.spriteBatch = spriteBatch;
            this.currMenu = Menu.Main;
            this.vp = spriteBatch.GraphicsDevice.Viewport;
            selectedOption = 0;

            Song song = game.Content.Load<Song>("Sounds/Backing/mainMenu"); 
            MediaPlayer.Play(song);
            MediaPlayer.IsRepeating = true;


            // Create selection rectangles
            selectionRects = new List<KeyValuePair<Rectangle, int>>();

            LoadContent();                
        }

        private void LoadContent()
        {

            mouse = game.Content.Load<Texture2D>("Sprites/mouse");
            titleBanner = game.Content.Load<Texture2D>("Sprites/titleBanner");
            changeSelectionSound = game.Content.Load<SoundEffect>("Sounds/menuChangeSelection");

            walkAnimation = new Animation(game.Content.Load<Texture2D>("Sprites/Player/Walk"), 0.1f, true);

            // Load scrolling background images
            String ext = "Backgrounds/MainMenu/Background0";
            bg1 = game.Content.Load<Texture2D>(ext + "1");
            bg2 = game.Content.Load<Texture2D>(ext + "2");
            bg3 = game.Content.Load<Texture2D>(ext + "3");
            bg4 = game.Content.Load<Texture2D>(ext + "4");
            bg5 = game.Content.Load<Texture2D>(ext + "5");

            // Initalize inital image positions
            p[0] = new Vector2(0, 0);
            p[1] = new Vector2(p[0].X + bg1.Width, 0);
            p[2] = new Vector2(p[1].X + bg2.Width, 0);
            p[3] = new Vector2(p[2].X + bg3.Width, 0);
            p[4] = new Vector2(p[3].X + bg4.Width, 0);
        }

        // Updates the menu's current selection and handles what happens when a selection is made
        public void Update(GameTime gameTime, KeyboardState keyboardState, MouseState mouseState)
        {
            UpdateScrollingBackground(gameTime);

           // Update number of options depending on current menu
           UpdateNumOfOptions();
           animationPlayer.PlayAnimation(walkAnimation);

                // Handle keyboard presses
                if (keyboardState.IsKeyDown(Keys.Down) && dWasReleased)
                {
                    selectedOption = (selectedOption + 1) % numOfOptions;
                    Game.PlaySound(changeSelectionSound);
                    dWasReleased = false;
                }
                if (keyboardState.IsKeyDown(Keys.Up) && uWasReleased)
                {
                    selectedOption = (selectedOption - 1 + numOfOptions) % numOfOptions;
                    Game.PlaySound(changeSelectionSound);
                    uWasReleased = false;
                }
                if (keyboardState.IsKeyDown(Keys.Enter) && eWasReleased)
                {
                    HandleSelection();
                    Game.PlaySound(changeSelectionSound);
                    eWasReleased = false;
                }
                if (keyboardState.IsKeyDown(Keys.Escape) && escWasReleased)
                {
                    Game.PlaySound(changeSelectionSound);
                    escWasReleased = false;
                    // Save game
                    Game.DoSaveGame();
                    selectedOption = 0;
                    currMenu = Menu.Check;
                }
                
                // Check if neccesary keys have been released
                dWasReleased = keyboardState.IsKeyUp(Keys.Down);
                uWasReleased = keyboardState.IsKeyUp(Keys.Up);
                eWasReleased = keyboardState.IsKeyUp(Keys.Enter);
                escWasReleased = keyboardState.IsKeyUp(Keys.Escape);
                
            // Handle mouse selection(no delay)
            mousePos = new Point(mouseState.X, mouseState.Y);

            foreach (KeyValuePair<Rectangle, int> item in selectionRects)
            {
                // If item's rectangle contains mouse set the current selected option to the associated menu option
                if (item.Key.Contains(mousePos))
                    selectedOption = item.Value;
            }

            // Also handle mouse clicks
            if (mouseState.LeftButton == ButtonState.Pressed && mWasReleased)
            {
                HandleSelection();
                Game.PlaySound(changeSelectionSound);
                mWasReleased = false;
            }

            // Check if mouse button has been released
            mWasReleased = (mouseState.LeftButton == ButtonState.Released);


            KeyboardState keyboard = Keyboard.GetState();
            
            if (currSelected == subjectR)
                this.textReader.Process(keyboard, gameTime, this.subjectText);
            else if (currSelected == messageR)
                this.textReader.Process(keyboard, gameTime, this.messageText);
            else
                this.textReader.Process(keyboard, gameTime, this.emailText);
            
        }


        private void UpdateScrollingBackground(GameTime gameTime)
        {
            // Updates scrolling background image positions
            if (p[0].X < -bg1.Width)
            {
                p[0].X = p[4].X + bg5.Width;
            }

            if (p[1].X < -bg2.Width)
            {
                p[1].X = p[0].X + bg1.Width;
            }

            if (p[2].X < -bg3.Width)
            {
                p[2].X = p[1].X + bg2.Width;
            }

            if (p[3].X < -bg4.Width)
            {
                p[3].X = p[2].X + bg3.Width;
            }

            if (p[4].X < -bg5.Width)
            {
                p[4].X = p[3].X + bg4.Width;
            }

            Vector2 aDirection = new Vector2(-1, 0);
            Vector2 aSpeed = new Vector2(160, 0);

            p[0] += aDirection * aSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            p[1] += aDirection * aSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            p[2] += aDirection * aSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            p[3] += aDirection * aSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            p[4] += aDirection * aSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
            
        }

        // Update number of menu options depending on current menu
        private void UpdateNumOfOptions()
        {
            switch (currMenu)
            {
                case Menu.Main:
                    numOfOptions = 4; break;
                case Menu.LoadGame:
                    numOfOptions = 2; break;
                case Menu.Options:
                    numOfOptions = 6; break;
                case Menu.Credits:
                    numOfOptions = 1; break;
                case Menu.Controls:
                    numOfOptions = 1; break;
                case Menu.Email:
                    numOfOptions = 2; break;
                case Menu.Check:
                    numOfOptions = 2; break;
            }
        }


        // Handles what happens when a menu item is chosen
        private void HandleSelection()
        {
            // Remove all rectangles from list so they can be updated to suit current menu
            selectionRects.RemoveRange(0, selectionRects.Count);

            switch (currMenu)
            {
                // Main menu
                case Menu.Main:

                    switch (selectedOption)
                    {
                        // New Game
                        case 0:
                            game.inMainMenu = false;
                            Level.FurthestLevelReached = 0;
                            game.LoadMainWorld();
                            break;

                        // Load level
                        case 1:
                            currMenu = Menu.LoadGame;
                            // Get furthest level from save file
                            Game.DoLoadGame();
                            break;
                        
                        // Options
                        case 2:
                            currMenu = Menu.Options;
                            break;

                        // Quit
                        case 3:
                            currMenu = Menu.Check;
                            //game.Exit();
                            break;
                    }
                    break;

                // Load level menu
                case Menu.LoadGame:
                    switch (selectedOption)
                    {
                        // Load level - load a saved game from a save file
                        case 0:
                            //StorageDevice device = new StorageDevice();
                            //Game.DoLoadGame(); //- start game from previous furthest level
                            game.inMainMenu = false;
                            game.LoadMainWorld();
                            break;

                        // Back
                        case 1:
                            currMenu = Menu.Main;
                            break;

                    }
                    break;

                // Options menu 
                case Menu.Options:
                    switch (selectedOption)
                    {
                        // Sound
                        case 0:
                            Game.soundOn = !Game.soundOn;
                            //if (Game.soundOn)
                                MediaPlayer.IsMuted = !Game.soundOn;
                            break;

                        // Controls
                        case 1:
                            currMenu = Menu.Controls;
                            break;

                        // Credits
                        case 2:
                            currMenu = Menu.Credits;
                            break;

                        // Send email
                        case 3:
                            // Reset all text fields to blank
                            emailText.Clear();
                            subjectText.Clear();
                            messageText.Clear();

                            currMenu = Menu.Email;
                            mailStatus = MailStatus.Writing;
                            break;

                        // Visit website
                        case 4:
                            System.Diagnostics.Process.Start("http://www.benhomer.freeiz.com/blog/");
                            break;

                        // Back
                        case 5:
                            currMenu = Menu.Main;
                            break;

                    }
                    break;

                // Credits menu 
                case Menu.Credits:
                    switch (selectedOption)
                    {
                        // Back
                        case 0:
                            currMenu = Menu.Options;
                            break;
                    }
                    break;

                // Controls menu 
                case Menu.Controls:
                    switch (selectedOption)
                    {
                        // Back
                        case 0:
                            currMenu = Menu.Options;
                            break;
                    }
                    break;

                // Email menu 
                case Menu.Email:
                    switch (selectedOption)
                    {
                        
                        // Send
                        case 0:
                                try
                                {
                                    MailMessage mail = new MailMessage();
                                    SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                                    mail.From = new MailAddress("bensblogx@gmail.com");
                                    mail.To.Add("bensblogx@gmail.com");
                                    mail.Subject = "Redman: " + subjectText;
                                    mail.Body = "From: " + emailText + "\n" + messageText;

                                    if (emailText.Length == 0)
                                        mailStatus = MailStatus.NoSender;
                                    else if (subjectText.Length == 0)
                                        mailStatus = MailStatus.NoSubject;
                                    else if (messageText.Length == 0)
                                        mailStatus = MailStatus.NoMessage;
                                    else
                                    {
                                        SmtpServer.Port = 587;
                                        // Find out how to encrypt username and password
                                        SmtpServer.Credentials = new System.Net.NetworkCredential("********", "********");
                                        SmtpServer.EnableSsl = true;

                                        SmtpServer.Send(mail);
                                        mailStatus = MailStatus.Sent;
                                    }
                                }
                                catch (Exception)
                                {
                                    mailStatus = MailStatus.Failed;
                                }
                        break;

                        // Back
                        case 1:
                            currMenu = Menu.Options;
                            break;

                        // Email address text box
                        case 2:
                            currSelected = nameR;
                            break;

                        // Subject text box
                        case 3:
                            currSelected = subjectR;
                            break;

                        // Message text box
                        case 4:
                            currSelected = messageR;
                            break;

                    }
                    break;

                // Check menu 
                case Menu.Check:
                    switch (selectedOption)
                    {
                        // Yes (exit)
                        case 0:
                            game.Exit();
                            break;
                        // No (don't exit)
                        case 1:
                            currMenu = Menu.Main;
                            break;
                    }
                    break;
            }

            // After handling selection set selected option to be the first one in new menu
            selectedOption = 0;
        }



        // Draws the game logo and main menu (+ add demo mode/cutscene later)
        public void Draw(GameTime gameTime)//, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();

            // Draw scrolling background
            spriteBatch.Draw(bg1, p[0], Color.White);
            spriteBatch.Draw(bg2, p[1], Color.White);
            spriteBatch.Draw(bg3, p[2], Color.White);
            spriteBatch.Draw(bg4, p[3], Color.White);
            spriteBatch.Draw(bg5, p[4], Color.White);

            // Draw title banner
            spriteBatch.Draw(titleBanner, new Vector2((vp.Width - titleBanner.Width)/2, 70), Color.White);

            // Draw animation
            if (animationPlayer.Animation != null)
                animationPlayer.Draw(gameTime, spriteBatch, new Vector2(300, selectedYpos + 40), SpriteEffects.None);

            // Draw menu options depending on state
            switch(currMenu)
            {
                case Menu.Main:
                    DrawMainMenu();
                    break;

                case Menu.LoadGame:
                    //doLoadGame();
                    DrawLoadLevelMenu();
                    break;

                case Menu.Options:
                    DrawOptionsMenu();
                    break;

                case Menu.Credits:
                    DrawCreditsMenu();
                    break;

                case Menu.Controls:
                    DrawControlsMenu();
                    break;

                case Menu.Email:
                    DrawEmailMenu();
                    break;

                case Menu.Check:
                    DrawCheckMenu();
                    break;
            }

            // Draw cursor sprite
            spriteBatch.Draw(mouse, new Vector2(mousePos.X, mousePos.Y), Color.White);

            // Testing
            //spriteBatch.DrawString(Game.bigFont, selectedOption.ToString(), new Vector2(100, 200), Color.Red);

            spriteBatch.End();
        }

        private void DrawMainMenu()
        {
            i = 200;
            DrawMenuOption("New Game", 0, i);
            DrawMenuOption("Load Level", 1, i += s);
            DrawMenuOption("Options", 2, i += s);
            DrawMenuOption("Quit", 3, i += s);
        }

        private void DrawLoadLevelMenu()
        {
            i = 200;
            levelThumbnail = game.Content.Load<Texture2D>("Levels/Thumbnails/" + Level.FurthestLevelReached);
            spriteBatch.Draw(levelThumbnail, new Vector2(i, 300), null, Color.White, 0, new Vector2(0), 0.25f, SpriteEffects.None, 0);

            game.DrawShadowedString(Game.hudFont, Info.getInfo(Level.FurthestLevelReached, -1), new Vector2(i, 450), Color.Red);
            game.DrawShadowedString(Game.hudFont, "Score: " + Player.Score, new Vector2(i, 480), Color.Red);

            float percentageComplete = (float) Math.Round((double)((double)(Level.FurthestLevelReached) / (double)(Game.numberOfLevels-1) * 100f), 2);
            percentageComplete = MathHelper.Clamp(percentageComplete, 0, 100);
            game.DrawShadowedString(Game.hudFont, "Completed: " + percentageComplete  + "%", new Vector2(i, 510), Color.Red);
            
            DrawMenuOption("Load Game", 0, i += 2*s);
            DrawMenuOption("Back", 1, i += s);
        }

        private void DrawOptionsMenu()
        {
            String snd = (Game.soundOn) ? "ON" : "OFF";

            i = 200;
            DrawMenuOption("Sound: " + snd, 0, i);
            DrawMenuOption("Controls", 1, i += s);
            DrawMenuOption("Credits", 2, i += s);
            DrawMenuOption("Send email", 3, i += s);
            DrawMenuOption("Visit Website", 4, i += s);
            DrawMenuOption("Back", 5, i += s);
        }

        private void DrawCreditsMenu()
        {
            i = 200;
            DrawString("Programming and Design: Ben Homer", i);
            DrawString("QA Testing: Dan Homer", i += s);
            DrawMenuOption("Back", 0, i += s);
        }

        private void DrawControlsMenu()
        {
            i = 200;
            DrawSmallString("Move left/right: Arrow keys", i);
            DrawSmallString("Jump: Up arrow key", i += s);
            DrawSmallString("Sprint: Hold Ctrl while moving", i += s);
            DrawSmallString("Shoot: S", i += s);
            DrawSmallString("Pause: P", i += s);
            DrawSmallString("Quit/Return home: Esc", i += s);
            DrawMenuOption("Back", 0, i += s);
        }

        private void DrawEmailMenu()
        {
            i = 200;
            int x1 = 100;
            int x2 = 300;

            DrawEmailStatus();

            spriteBatch.DrawString(Game.bigFont, "Email:", new Vector2(x1, i), Color.Red);
            nameR = new Rectangle(x2, i + 10, 400, 40);
            DrawMenuOptionTextBox(nameR, 2);
            i += s;

            spriteBatch.DrawString(Game.bigFont, "Subject:", new Vector2(x1, i), Color.Red);
            subjectR = new Rectangle(x2, i + 10, 400, 40);
            DrawMenuOptionTextBox(subjectR, 3);
            i += s;

            spriteBatch.DrawString(Game.bigFont, "Message:", new Vector2(x1, i), Color.Red);
            messageR = new Rectangle(x2, i + 10, 400, 300);
            DrawMenuOptionTextBox(messageR, 4);
            i += s;

            String email = emailText.ToString();
            String subject = subjectText.ToString();
            String message = messageText.ToString();

            parseTextAndDraw(email, Game.hudFont, nameR);
            parseTextAndDraw(subject, Game.hudFont, subjectR);
            parseTextAndDraw(message, Game.hudFont, messageR);

            DrawMenuOption("Send", 0, i += 5*s);
            DrawMenuOption("Back", 1, i += s);
        }

        // Draw mail status to screen
        private void DrawEmailStatus()
        {
            String status;
            switch (mailStatus)
            {
                case MailStatus.Writing:
                    status = "Please type 'Bug Report'\nin subject if\nreporting a bug.";
                    break;
                case MailStatus.NoSender:
                    status = "Please type an email\naddress to send from.";
                    break;
                case MailStatus.NoSubject:
                    status = "Please type a subject\nfor the email.";
                    break;
                case MailStatus.NoMessage:
                    status = "Please type a message\nfor the email.";
                    break;
                case MailStatus.Sent:
                    status = "Sent";
                    break;
                case MailStatus.Failed:
                    status = "Failed\nYou need an internet\nconnection in order\nto send an email.";
                    break;

                default:
                    status = "";
                    break;
            }
            spriteBatch.DrawString(Game.hudFont, status, new Vector2(730, 300), Color.Red);
        }

        // Manages text wrapping in text box (for email sending page) and draws into given textbox
        private void parseTextAndDraw(String text, SpriteFont font, Rectangle textBox)
        {
            String line = String.Empty;
            String returnString = String.Empty;
            String[] wordArray = text.Split(' ');
 
            foreach (String word in wordArray)
            {
                if (font.MeasureString(line + word).Length() > textBox.Width)
                {
                    returnString = returnString + line + '\n';
                    line = String.Empty;
                }
 
                line = line + word + ' ';
            }
 
            spriteBatch.DrawString(font, returnString + line, new Vector2(textBox.X, textBox.Y), Color.White);
        }

        private void DrawCheckMenu()
        {
            i = 200;
            DrawString("Are you sure you want to quit?", i);
            DrawMenuOption("Yes", 0, i += 2*s);
            DrawMenuOption("No", 1, i += s);
        }


        private void DrawMenuOption(String str, int menuOptionIndex, int yPos)
        {
            
            int width = (int)Game.bigFont.MeasureString(str).X;

            // If option is selected change its color
            Color strColor =  (selectedOption.Equals(menuOptionIndex)) ? Color.Blue : Color.Red;
            if (selectedOption.Equals(menuOptionIndex))
                selectedYpos = yPos;

            game.DrawShadowedString(Game.bigFont, str, new Vector2((vp.Width - width) / 2, yPos), strColor);

            // Add menu options bounding rectangle to list
            selectionRects.Add(new KeyValuePair<Rectangle, int>(new Rectangle((vp.Width - width) / 2, yPos, width, s), menuOptionIndex));
        }


        private void DrawMenuOptionTextBox(Rectangle rect, int menuOptionIndex)
        {
            Texture2D pixel;
            // If textbox is selected change its color
            if (currSelected == rect)
                pixel = game.Content.Load<Texture2D>("Sprites/solidblue");
            else
                pixel = game.Content.Load<Texture2D>("Sprites/solidred");
            spriteBatch.Draw(pixel, rect, Color.White);
            
            // Add menu options bounding rectangle to list
            selectionRects.Add(new KeyValuePair<Rectangle, int>(rect, menuOptionIndex));
        }

        private void DrawString(String str, int yPos)
        {

            float width = Game.bigFont.MeasureString(str).X;

            game.DrawShadowedString(Game.bigFont, str, new Vector2((vp.Width - width) / 2, yPos), Color.Red);
        }

        private void DrawSmallString(String str, int yPos)
        {

            float width = Game.hudFont.MeasureString(str).X;

            game.DrawShadowedString(Game.hudFont, str, new Vector2((vp.Width - width) / 2, yPos), Color.Red);
        }

    }
}
