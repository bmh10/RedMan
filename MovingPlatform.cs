using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace RedMan
{
    /*
     * Implements graphics and physics for moving platforms
     */
    class MovingPlatform
    {
        private Texture2D objectTex;
        public bool moveVertical, horizontalOrientation;
        float RotationAngle;
        private bool hitStopper;
        public bool playerContact;
        public bool createdByEvent;
        private FaceDirection direction = FaceDirection.Left;
        private float waitTime;
        private const float MaxWaitTime = 0.2f;

        public Vector2 velocity;
        public static float currMoveSpeed;

        public static float PlatformSpeed = 90.0f;
        //public static float FasterSpeed = 90.0f;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        // Position in world space of the bottom center of this enemy.
        public Vector2 Position
        {
            get { return position; }
        }
        Vector2 position;

        private Rectangle localBounds;


        // Constructs a new moving platform.
        public MovingPlatform(Level level, Vector2 position, bool createdByEvent, bool verticalMotion, bool horizontalOrientation)
        {
            this.level = level;
            this.position = position;
            this.createdByEvent = createdByEvent;
            MovingPlatform.currMoveSpeed = PlatformSpeed;
            this.moveVertical = verticalMotion;
            this.horizontalOrientation = horizontalOrientation;

            if (horizontalOrientation)
                RotationAngle = 0;
            else
                RotationAngle = MathHelper.ToRadians(90);

            LoadContent();
        }

        // Loads a particular enemy sprite sheet and sounds.
        public void LoadContent()
        {
            objectTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "movingPlatformTile");

            // Calculate bounds within texture size.
            int width = (int)(objectTex.Width * 0.35);
            int left = (objectTex.Width - width) / 2;
            int height = (int)(objectTex.Height * 0.7);
            int top = objectTex.Height - height;
            localBounds = new Rectangle(left, top, width, height);
        }


        // Moves back and forth between stopper blocks waiting at either end briefly
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

                if (waitTime > 0)
                {
                    // Wait for some amount of time.
                    waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                    if (waitTime <= 0.0f)
                    {
                        // Then turn around.
                        direction = (FaceDirection)(-(int)direction);
                        hitStopper = false;
                    }
                }
                else
                {
                    HandleCollisions();

                    if (hitStopper)
                        waitTime = MaxWaitTime;
                    else
                    {
                        // Move in the current direction.
                        if (moveVertical)
                            velocity = new Vector2(0.0f, (int)direction * currMoveSpeed * elapsed);
                        else 
                            velocity = new Vector2((int)direction * currMoveSpeed * elapsed, 0.0f);

                        position += velocity;


                        if (playerContact)
                        {
                            // Move player while on the moving platform
                            level.Player.Position += this.velocity;
                        }
                    }
                }

                // If platform is out of bounds remove it from level
                if (this.position.X < 0 || this.position.X > level.Width * Tile.Width || this.position.Y < 0 || this.position.Y > level.Height * Tile.Height)
                    this.Remove();
        }


        private void HandleCollisions()
        {
            // Get the elevator's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            float posX = Position.X + localBounds.Width / 2 * (int)direction;
            int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
            int tileY = (int)Math.Floor(Position.Y / Tile.Height);

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);

                    if ( !moveVertical && Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Stopper
                        || moveVertical && Level.GetCollision(tileX, tileY + (int)direction) == TileCollision.Stopper)
                        hitStopper = true;

                    // Player can only make contact with horizontal platforms (vertical platforms only stop bullets)
                    if (Level.Player.BoundingRectangle.Intersects(this.BoundingRectangle) && horizontalOrientation)
                    {
                        playerContact = true;
                        level.Player.IsOnGround = true;
                    }
                    else playerContact = false;
                }
            }
        }

        public void Remove()
        {
            level.MovingPlatforms.Remove(this);
        }


        public Rectangle BoundingRectangle
        {
            get {
                    if (horizontalOrientation)
                        return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), objectTex.Width, objectTex.Height);
                    else
                        return new Rectangle((int)(position.X), (int)(position.Y - objectTex.Width / 2), objectTex.Height, objectTex.Width);
                }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(objectTex.Width / 2.0f, objectTex.Height); }
        }

       
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(objectTex, Position, null, Color.White, RotationAngle, Origin, 1.0f, SpriteEffects.None, 0f);

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

