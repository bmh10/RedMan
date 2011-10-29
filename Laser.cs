using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace RedMan
{
    /*
     * Direction of laser rotation
     */
    enum RotationDirection { None, Clockwise, Anticlockwise };

    /*
     * Implements laser graphics and physics
     */
    class Laser
    {
        private Texture2D laserTex, transparentTex, testTex;
        
        private SpriteEffects flip;
        private float timer;
        private float cycleTimeMax = 100;

        private float rotationAngle;
        private RotationDirection rotationDir;
        private const float RotationSpeed = 5;

        private bool laserOn;
        public bool flashing;
        public bool createdByEvent;
        private bool vertical;
        private bool remove = false;
        public Vector2 tileCoords;

        private const int LaserThickness = 9;


        public Level Level
        {
            get { return level; }
        }
        Level level;


        //** Physics state **\\
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;


        public Laser(Level level, int x, int y, bool createdByEvent, bool vertical, bool flashing, RotationDirection rotationDir)
        {
            this.level = level;
            this.position = RectangleExtensions.GetBottomCenter(level.GetBounds(x, y));
            this.tileCoords = new Vector2(x, y);
            this.createdByEvent = createdByEvent;
            this.vertical = vertical;
            this.flashing = flashing;
            this.rotationDir = rotationDir;
            // If not flashing laser is always on
            if (!flashing)
                laserOn = true;
            // Rotate tile to use as either horizontal or vertical (so only need to store one image file)
            if (vertical)
                rotationAngle = 0;
            else
            {
                rotationAngle = MathHelper.ToRadians(90);
            }

            LoadContent();
        }

        public void LoadContent()
        {
            if (rotationDir == RotationDirection.None)
                laserTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "laserTile");
            else
                laserTex = Level.Content.Load<Texture2D>("Tiles/rotateLaserTile");
            transparentTex = Level.Content.Load<Texture2D>("Tiles/transparentTile");
            testTex = Level.Content.Load<Texture2D>("Tiles/dirtTile");
        }

        // Updates both flashing and non-flashing lasers
        public void Update(GameTime gameTime)
        {
            if (level.inCarRaceMode() && rotationDir != RotationDirection.None)
                level.Tiles[(int)tileCoords.X, (int)tileCoords.Y] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);

            if (rotationDir == RotationDirection.Clockwise)
                rotationAngle += MathHelper.ToRadians(RotationSpeed);
            else if (rotationDir == RotationDirection.Anticlockwise)
                rotationAngle -= MathHelper.ToRadians(RotationSpeed);

            if (flashing)
            {
                float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

                timer++;
                if (timer < cycleTimeMax / 2)
                    laserOn = true;
                else if (timer < cycleTimeMax)
                    laserOn = false;

                if (timer >= cycleTimeMax)
                    timer = 0;

                UpdateFlashes();
            }
            else
            {
                if (this.BoundingRectangle.Intersects(level.Player.BoundingRectangle))
                    level.Player.OnKilled(null);
            }

            if (remove)
                level.Lasers.Remove(this);
        }

        // Alternates the flashing laser between on/off
        private void UpdateFlashes()
        {
            // Get tile co-oridinates
            RotatedRectangle bounds = BoundingRectangle;
            int x = bounds.X / Tile.Width;
            int y = bounds.Y / Tile.Height;

            if (laserOn)
            {
                if (this.BoundingRectangle.Intersects(level.Player.BoundingRectangle))
                    level.Player.OnKilled(null);
            }
            else
                level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
        }

        public void Remove()
        {
            remove = true;
        }

        public RotatedRectangle BoundingRectangle
        {
            get {
                if (rotationDir == RotationDirection.None)
                {
                    if (vertical)
                        return new RotatedRectangle(new Rectangle((int)(position.X - Origin.X + (laserTex.Width - LaserThickness) / 2), (int)(position.Y - Origin.Y + Tile.Width/2), LaserThickness, laserTex.Height), rotationAngle);
                    else
                    return new RotatedRectangle(new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y + (Tile.Width) / 2), Tile.Width, LaserThickness), 0);
                }
                else
                {
                    return new RotatedRectangle((new Rectangle((int)(position.X - Origin.X + (laserTex.Width - LaserThickness) / 2), (int)(position.Y - Origin.Y), LaserThickness, laserTex.Height)), rotationAngle);
                }
            }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(laserTex.Width / 2.0f, laserTex.Height); }
        }
        

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Vector2 center = new Vector2(laserTex.Width / 2, laserTex.Height / 2);
            Vector2 pos = new Vector2(Position.X, Position.Y - laserTex.Height/2);
            if (laserOn)
            {
                if (rotationDir == RotationDirection.None)
                    spriteBatch.Draw(laserTex, pos, null, Color.White, rotationAngle, center, 1.0f, flip, 0f);
                else
                    spriteBatch.Draw(laserTex, pos, null, Color.White, rotationAngle, Origin, 1.0f, flip, 0f);
            }
            else
                spriteBatch.Draw(transparentTex, Position, null, Color.White, 0.0f, Origin, 1.0f, flip, 0.0f);

      
            //Debugging rectangle
            /*
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, BoundingRectangle.Width, BoundingRectangle.Height);

            Color[] data = new Color[BoundingRectangle.Width * BoundingRectangle.Height];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Chocolate;
            rect.SetData(data);

            Vector2 coor = new Vector2(BoundingRectangle.X, BoundingRectangle.Y);
            spriteBatch.Draw(rect, coor, Color.White);
            */

            //Rectangle aPositionAdjusted = new Rectangle(BoundingRectangle.X + (BoundingRectangle.Width / 2), BoundingRectangle.Y + (BoundingRectangle.Height/2), BoundingRectangle.Width, BoundingRectangle.Height);
            //spriteBatch.Draw(testTex, aPositionAdjusted, new Rectangle(0, 0, LaserThickness, laserTex.Height), Color.Blue, BoundingRectangle.Rotation, new Vector2(LaserThickness / 2, laserTex.Height), SpriteEffects.None, 0);
        }
    }
}

