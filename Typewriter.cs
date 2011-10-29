using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
//using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace RedMan
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    class Typewriter
    {
        Game game;
        //GraphicsDeviceManager graphics;
        //SpriteBatch spriteBatch;
        Rectangle textBox;
        Texture2D debugColor;
        SpriteFont font;
        String text;
        String parsedText;
        String typedText;
        double typedTextLength;
        int delayInMilliseconds;
        bool isDoneDrawing;

        public Typewriter(Game game)
        {
            this.game = game;

            textBox = new Rectangle(100, 100, 500, 150);
            LoadContent();
        }

      
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        private void LoadContent()
        {
            debugColor = game.Content.Load<Texture2D>("Sprites/solidred");
            font = Game.hudFont;
            text = "Congratulations! You have completed the game! Let me know what you thought at www.benhomer.freeiz.com\nThanks for playing!";
            parsedText = parseText(text);
            delayInMilliseconds = 50;
            isDoneDrawing = false;
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Update(GameTime gameTime)
        {
            if (!isDoneDrawing)
            {
                if (delayInMilliseconds == 0)
                {
                    typedText = parsedText;
                    isDoneDrawing = true;
                }
                else if (typedTextLength < parsedText.Length)
                {
                    typedTextLength = typedTextLength + gameTime.ElapsedGameTime.TotalMilliseconds / delayInMilliseconds;

                    if (typedTextLength >= parsedText.Length)
                    {
                        typedTextLength = parsedText.Length;
                        isDoneDrawing = true;
                    }

                    typedText = parsedText.Substring(0, (int)typedTextLength);
                }
            }

            //base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public void Draw(SpriteBatch spriteBatch)
        {
            
            spriteBatch.Draw(debugColor, textBox, Color.White);
            spriteBatch.DrawString(font, typedText, new Vector2(textBox.X, textBox.Y), Color.White);

            //base.Draw(gameTime);
        }

        private String parseText(String text)
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

            return returnString + line;
        }

    }
}
