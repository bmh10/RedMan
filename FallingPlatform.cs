using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace RedMan
{
    /*
     * Implements physics and graphics of falling platforms
     */
    class FallingPlatform
    {
        private Texture2D texture;
        private Vector2 origin;
        private SoundEffect crackSound;
        bool hit;
        private Vector2 originalPosition;
        private int tileXpos, tileYpos;
        private const int FallDelayTime = 50;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        // Gets the current position of this platform in world space.
        public Vector2 Position
        {
            get
            {
                return position;
            }
        }
        Vector2 position;


        // Constructs a new falling platform.
        public FallingPlatform(Level level, Vector2 pos)
        {
            this.level = level;
            this.position = pos;
            this.originalPosition = pos;
            tileXpos = (int)(this.originalPosition.X / Tile.Width);
            tileYpos = (int)(this.originalPosition.Y / Tile.Height);
            LoadContent();
        }

        // Loads the texture and contact sound.
        public void LoadContent()
        {
            texture = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "platformTile");
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
            crackSound = Level.Content.Load<SoundEffect>("Sounds/Crack");
        }

        // Falls when hit by player and removed from level until restart
        public void Update(GameTime gameTime)
        {
            if (!Game.paused)
            {
                // Make platform fall verticaly
                if (hit)
                    position.Y += 5;
                // Give player small amount of time to move before tile is removed from play
                if (position.Y > originalPosition.Y + FallDelayTime)
                    level.Tiles[tileXpos, tileYpos] = new Tile(null, TileCollision.Passable);
                else
                    level.Tiles[tileXpos, tileYpos] = new Tile(null, TileCollision.Platform);

                // If platform has fallen off bottom of level, remove from game
                if (position.Y > level.Height * Tile.Height)
                    level.FallingPlatforms.Remove(this);
            } 
        }

        // Called when this platform has been hit by the player.
        public void OnContact(Player contactBy)
        {
            hit = true;
           Game.PlaySound(crackSound);
        }


        public Rectangle BoundingRectangle
        {
            get { return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), texture.Width, texture.Height); }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(texture.Width / 2.0f, texture.Height); }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, Position, null, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);

            /*
            //Debugging rectangle
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, BoundingRectangle.Width, BoundingRectangle.Height);

            Color[] data = new Color[BoundingRectangle.Width * BoundingRectangle.Height];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Chocolate;
            rect.SetData(data);

            Vector2 coor = new Vector2(BoundingRectangle.X, BoundingRectangle.Y);
            spriteBatch.Draw(rect, coor, Color.White);
             */
        }
    }
}
