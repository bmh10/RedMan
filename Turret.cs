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
     * Possible turret firing directions when not in multidirectional firing mode
     */
    enum TurretFiringDirection { Left, Right, };

    /*
     * Implements graphics and physics of turrets
     */
    class Turret
    {
        private Texture2D turretTex, explosionTex;
        private SoundEffect explosionSound;
        private Vector2 initialPosition;
        private float timeSinceShot;
        public Enemy enemy;

        private bool died;
        public bool createdByEvent;
        private int deathTimer;
        private const int DeadTime = 30;
        private const int ReloadTime = 100;

        public TurretFiringDirection currFiringDireciton;
        // True if turret can fire bullets at any angle (otherwise they only fire horizontally)
        public bool multiDirectionalFire;
        public Vector2 turretVector;

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

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float AirDragFactor = 0.58f;


        public Turret(Level level, Vector2 position, bool createdByEvent, Enemy enemy, bool multiDirectionalFire)
        {
            this.enemy = enemy;
            this.level = level;
            //this.position = position;
            this.position = new Vector2(position.X + Tile.Width / 2, position.Y + Tile.Height);
            this.initialPosition = position;
            this.createdByEvent = createdByEvent;
            this.multiDirectionalFire = multiDirectionalFire;
            this.timeSinceShot = 50;
            level.Turrets.Add(this);
            LoadContent();
        }

        public void LoadContent()
        {
            if (level.inCarRaceMode())
            {
                int xpos = (int)(initialPosition.X / Tile.Width);
                if (xpos < 240) // concrete
                    turretTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "turretTile");
                else if (xpos < 688) // sand
                    turretTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "sandTurretTile");
                else // ice
                    turretTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "iceTurretTile");
            }
            else
                turretTex = Level.Content.Load<Texture2D>("Tiles/" + level.GetTileSet() + "turretTile");

            explosionTex = Level.Content.Load<Texture2D>("Tiles/explosion");
            explosionSound = Level.Content.Load<SoundEffect>("Sounds/Explosion");
        }

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Has has been dead for a certain time, remove turret from level
            if (died)
            {
                deathTimer++;
                if (deathTimer > DeadTime)
                {
                    Level.Turrets.Remove(this);
                    Level.Score += 50;
                }
            }

            // If fired from an enemy update position
            if (enemy != null)
                this.position = enemy.Position;

            if (level.Player.Position.X < position.X)
                currFiringDireciton = TurretFiringDirection.Left;
            else
                currFiringDireciton = TurretFiringDirection.Right;

            timeSinceShot++;
            if (!died && timeSinceShot > ReloadTime)
            {
                level.Bullets.Add(new Bullet(level, position, gameTime, this));
                timeSinceShot = 0;
            }
        }


        // Called when the turret has been killed.
        public void OnKilled()
        {
            Game.PlaySound(explosionSound);
            died = true;

            if (level.inCarRaceMode())
            {
                int x = (int)(initialPosition.X / Tile.Width);
                int y = (int)(initialPosition.Y / Tile.Height);
                if (x < 240) // concrete
                    level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                else if (x < 688) // sand
                    level.Tiles[x, y] = level.LoadLevelTile("pavementTile", TileCollision.Passable);
                else // ice
                    level.Tiles[x, y] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
            }
        }

        // Removes a turret without any sound effect
        public void Remove()
        {
            died = true;
        }

        public Rectangle BoundingRectangle
        {
            get { return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), turretTex.Width, turretTex.Height); }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(turretTex.Width / 2.0f, turretTex.Height); }
        }

        public void OnContact(Player contactBy)
        {
            contactBy.OnKilled(null);
        }


        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Only draw if this is an actual turret not an enemy firing
            if (enemy == null)
            {
                if (died)
                    spriteBatch.Draw(explosionTex, Position, null, Color.White, 0.0f, Origin, 1.0f, SpriteEffects.None, 0.0f);
                else
                    spriteBatch.Draw(turretTex, Position, null, Color.White, 0.0f, Origin, 1.0f, SpriteEffects.None, 0.0f);
            }

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
