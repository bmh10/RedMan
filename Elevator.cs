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
    class Elevator
    {
        private Texture2D elevatorTex;
        private Texture2D elevatorBaseTile;
        private SoundEffect elevateSound;
        private float airTime;
        private float initialVelocity;
        private Vector2 initialPosition;
        private bool goUp;
        bool continueLifting;

        private const float MoveSpeed = 3;


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


        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        public Elevator(Level level, Vector2 position, bool goUp)
        {
            this.level = level;
            this.position = new Vector2(position.X + Tile.Width / 2, position.Y + Tile.Height);
            this.initialPosition = this.position;
            this.goUp = goUp;
            this.airTime = 0.0f;
            continueLifting = true;
            LoadContent();
        }

        public void LoadContent()
        {
            elevatorTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "elevatorTile");
            elevatorBaseTile = level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "elevatorBaseTile");
            elevateSound = Level.Content.Load<SoundEffect>("Sounds/Elevator");
        }

        public void Update(GameTime gameTime, Level level)
        {
                HandleCollisions();

                if (continueLifting)
                {

                    if (airTime == 0.0f)
                        Game.PlaySound(elevateSound);
                    airTime++;

                    if (goUp)
                        initialVelocity = -MoveSpeed;
                    else
                        initialVelocity = MoveSpeed;

                    Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y + initialVelocity));

                    level.Player.Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y + initialVelocity)); // velocity* elapsed;
                }
                else
                {
                    Position = new Vector2((float)Math.Round(initialPosition.X), (float)Math.Round(initialPosition.Y));
                    airTime = 0;
                    continueLifting = true;
                }
        }


        private void HandleCollisions()
        {
            // Get the elevator's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);

                    // If elevator hits stopper tile
                    if (collision == TileCollision.Stopper)
                    {
                        continueLifting = false;
                        level.Tiles[x, y + 3] = new Tile(elevatorBaseTile, TileCollision.Platform);
                    }
                }
            }
        }


        public Rectangle BoundingRectangle
        {
            get { return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), elevatorTex.Width, elevatorTex.Height); }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(elevatorTex.Width / 2.0f, elevatorTex.Height); }
        }


        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(elevatorTex, Position, null, Color.White, 0.0f, Origin, 1.0f, SpriteEffects.None, 0.0f);

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

