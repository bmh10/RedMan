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
     * Implements graphics and physics for moving spikes
     */
    class MovingSpike
    {
        private Texture2D objectTex;
        private string objectLocation;
        private TileCollision tileType;
        private SpriteEffects flip;
        public bool createdByEvent;
        private bool vertical, goRight;
        private float RotationAngle;
        private bool continueMoving;

        public const float FastMoveSpeed = 5.0f;
        public const float BaseMoveSpeed = 2.5f;
        private const float SlowMoveSpeed = 1.0f;
        private float MoveSpeed;
        private float initialVelocity;

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


        public float MySpeed
        {
            get { return mySpeed; }
            set { mySpeed = value; }
        }
        float mySpeed = 0;

        public MovingSpike(Level level, Vector2 position, bool createdByEvent, bool vertical, bool goRight)
        {
            this.level = level;
            this.position = position;
            this.createdByEvent = createdByEvent;
            this.vertical = vertical;
            this.goRight = goRight;
            continueMoving = true;

            if (vertical)
            {
                if (goRight)
                    RotationAngle = MathHelper.ToRadians(180);
                else
                    RotationAngle = 0;
            }
            else
            {
                if (goRight)
                    RotationAngle = MathHelper.ToRadians(90);
                else
                    RotationAngle = MathHelper.ToRadians(-90);
            }
            LoadContent();
        }

        public void LoadContent()
        {
            objectTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "spikeTile");
        }

        public void Update(GameTime gameTime)
        {
            HandleCollisions();

            // Update move speed depending on player's speed
            // If player is sprinting to the right increase speed of spikes
            if (MySpeed == 0)
            {
                if (vertical)
                    MoveSpeed = SlowMoveSpeed;
                else
                {
                    if (level.Player.isSprinting && level.Player.Velocity.X > 0)
                        MoveSpeed = 2 * BaseMoveSpeed;
                    else
                        MoveSpeed = BaseMoveSpeed;
                }
            }
            else
                MoveSpeed = MySpeed;

            if (continueMoving)
            {
                    initialVelocity = (goRight) ? MoveSpeed : -MoveSpeed;

                if (vertical)
                    Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y + initialVelocity));
                else
                    Position = new Vector2((float)Math.Round(Position.X + initialVelocity), (float)Math.Round(Position.Y));

            }
            // NB If want spike to return to start put code here
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
                        continueMoving = false;
                    }
                }
            }
        }


        public Rectangle BoundingRectangle
        {
            get {
                if (!goRight)
                    return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), objectTex.Width, objectTex.Height);
                else
                    return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), objectTex.Width, objectTex.Height);
            }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(objectTex.Width / 2.0f, objectTex.Height); }
        }

        public void Remove()
        {
            continueMoving = false;
            level.MovingSpikes.Remove(this);
        }


        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Calculate position to draw (with slight offset)
            Vector2 pos;
            if (vertical)
                pos = new Vector2(BoundingRectangle.Location.X + Tile.Width / 2, BoundingRectangle.Location.Y);
            else
            {
                if (!goRight)
                    pos = new Vector2(BoundingRectangle.Location.X + Tile.Width, BoundingRectangle.Location.Y + Tile.Width / 2);
                else
                    pos = new Vector2(BoundingRectangle.Location.X, BoundingRectangle.Location.Y + Tile.Width / 2);
            }

            spriteBatch.Draw(objectTex, pos, null, Color.White, RotationAngle, Origin, 1.0f, flip, 0f);

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

