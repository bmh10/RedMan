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
     * Implements bullet types, physics and graphics
     */
    class Bullet
    {
        public Texture2D bulletTex, explosionTex;
        AnimationPlayer animationPlayer;
        Animation sniperShotAnimation, blankAnimation;
        private SoundEffect shootSound, reloadSound, hitTarget;
        private SpriteEffects flip;
        public float airTime, explodeTimer;
        private float initialVelocityX;
        private float initialVelocityY = 0;
        private Vector2 initialPosition;
        private float range;
        private bool removeBullet;
        public bool exploded, bouncable, powerBullet;
        public static bool DropBomb;
        private Turret turret;
        private Enemy enemy;
        private float bulletRotation = 0;

        private const float MaxExplodeTime = 5;
        // Time to delay collisions when shooting in sniper mode
        private const float SniperDelayTime = 50.0f;

        // Weapons ranges
        private const float pistolRange = 500; 
        private const float uziRange = 300;
        private const float sniperRange = 800;
        private const float harpoonRange = 500;
        private const float spaceBlasterRange = 500;
        public static float multiDirectionalRange = 1000;
        private const float turretRange = 1000;
        public static float powerBulletRange = 800;
        public static float enemyShotRange = 500;

        // Weapon reload times
        public static float pistolReload = 50;
        public static float uziReload = 10;
        public static float sniperReload = 80;
        public static float harpoonReload = 50;
        public static float spaceBlasterReload = 30;
        public static float multiDirectionalReload = 10;
        public static float powerBulletReload = 80;

        // Weapon relative bullet velocity
        public static float pistolVelocity = 4;
        public static float uziVelocity = 5;
        public static float sniperVelocity = 4;
        public static float harpoonVelocity = 3;
        public static float submarineGunVelocity = 7;
        public static float spaceBlasterVelocity = 7;
        public static float multiDirectionalVelocity = 7;
        public static float turretVelocity = 4;
        public static float powerBulletVelocity = 5;
        public static float enemyShotVelocity = 3;

        // Target counter for current level
        public static int LevelTargetCount = 0;
        public static int Level1TargetCountMax = 4;

        // Stats for sniper mode (level 9)
        public static int shotsFired = 0;
        public static int shotsOnTarget = 0;
        public static float accuracyPercentage = 0;
        public static int enemiesKilled = 0;
        public static int enemiesMax = 8;
        public static int targetsHit = 0;
        public static int targetMax = 8;

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


        public bool PlayerBullet
        {
            get { return playerBullet; }
        }
        bool playerBullet;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float AirDragFactor = 0.58f;

        // For creating single enemy bullets (which are not continous like turrets)
        public Bullet(Level level, Vector2 position, GameTime gameTime, Enemy enemy)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float MoveVelocity = MoveAcceleration * elapsed * AirDragFactor;
            this.level = level;
            this.position = position;
            this.enemy = enemy;
            this.initialPosition = position;
            this.removeBullet = false;
            this.airTime = 0.0f;
            this.explodeTimer = 0.0f;

            this.playerBullet = false;

            // Set range and velocity
            range = enemyShotRange;
            MoveVelocity *= enemyShotVelocity;

            if (enemy.direction == FaceDirection.Right)
                initialVelocityX = MoveVelocity;
            else
                initialVelocityX = -MoveVelocity;

            LoadContent();
        }

        // For creating player bullets, turret bullets or continously firing enemies (implemented as turrets)
        public Bullet(Level level, Vector2 position, GameTime gameTime, Turret turret)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float MoveVelocity = MoveAcceleration * elapsed * AirDragFactor;
            this.level = level;
            this.position = position;
            this.initialPosition = position;
            this.turret = turret;
            this.removeBullet = false;
            this.airTime = 0.0f;
            this.explodeTimer = 0.0f;

            this.playerBullet = (turret == null);
            if (level.LevelIndex == 9)
                LoadContent();
           
            animationPlayer.PlayAnimation(blankAnimation);

            if (playerBullet)
            {
                // This bullet is from a player

                // Set range and bullet velocity depending on weapon
                if (level.Player.powerBulletCount > 0)
                {
                    powerBullet = true;
                    MoveVelocity *= powerBulletVelocity;
                    range = powerBulletRange;
                }
                else
                {
                    if (level.Player.inVehicle)
                    {
                        switch (level.Player.currVehicle)
                        {
                            case Vehicle.Submarine:
                                MoveVelocity *= submarineGunVelocity;
                                range = harpoonRange;
                                break;
                            case Vehicle.Spaceship:
                                MoveVelocity *= spaceBlasterVelocity;
                                range = spaceBlasterRange;
                                break;
                            case Vehicle.Snowmobile:
                                MoveVelocity *= spaceBlasterVelocity;
                                range = spaceBlasterRange;
                                break;
                            case Vehicle.Car:
                                MoveVelocity *= multiDirectionalVelocity;
                                range = multiDirectionalRange;
                                break;
                            default:
                                MoveVelocity *= uziVelocity;
                                range = pistolRange;
                                break;
                        }
                    }

                    else
                    {
                        switch (Level.Player.currGun)
                        {
                            case Gun.Pistol:
                                MoveVelocity *= pistolVelocity;
                                range = pistolRange;
                                break;
                            case Gun.Uzi:
                                MoveVelocity *= uziVelocity;
                                range = uziRange;
                                break;
                            case Gun.Sniper:
                                MoveVelocity *= sniperVelocity;
                                range = sniperRange;
                                break;
                            case Gun.Harpoon:
                                if (level.Player.inVehicle)
                                    MoveVelocity *= submarineGunVelocity;
                                else
                                    MoveVelocity *= harpoonVelocity;
                                range = harpoonRange;
                                break;
                            case Gun.SpaceBlaster:
                                MoveVelocity *= spaceBlasterVelocity;
                                range = spaceBlasterRange;
                                break;
                            case Gun.Multidirectional:
                                MoveVelocity *= multiDirectionalVelocity;
                                range = multiDirectionalRange;
                                break;
                            default:
                                range = pistolRange;
                                break;
                        }
                    }
                }

                
                //// If there is horizontal movement or no vertical movement fire horizontally
                //if (level.Player.Velocity.X == 0 || level.Player.vertMovement == 0)
                //{
                if (level.Player.shootWithMouse)
                {
                    if (level.Player.sniperMode)
                    {
                        animationPlayer.PlayAnimation(sniperShotAnimation);
                        initialVelocityX = 0;
                        initialVelocityY = 0;

                        // Update stats
                        shotsFired++;
                    }
                    else
                    {
                        Vector2 vector = level.Player.bulletVector;
                        initialVelocityX = vector.X * MoveVelocity;
                        initialVelocityY = vector.Y * MoveVelocity;
                    }
                }

                // Drop power bullets (like bombs in level 6)
                else if (DropBomb)
                {
                    initialVelocityX = 0;
                    initialVelocityY = MoveVelocity;
                }
                else
                {
                    if (level.Player.FacingRight)
                        initialVelocityX = MoveVelocity;
                    else
                        initialVelocityX = -MoveVelocity;

                    // In sudo 3D levels can also fire up or down when moving in those directions
                    if (level.inSudo3D())
                    {
                        if (level.Player.vertMovement != 0 && level.Player.Velocity.X == 0)
                        {
                            initialVelocityX = 0;
                            bulletRotation = MathHelper.ToRadians(90);

                            if (level.Player.vertMovement > 0)
                                initialVelocityY = MoveVelocity;
                            else if (level.Player.vertMovement < 0)
                                initialVelocityY = -MoveVelocity;
                        }
                    }
                }
            }

            else
            {
                // This bullet is from a turret/enemy
                // Set range for turret
                range = turretRange;
                MoveVelocity *= turretVelocity;

                // Set direction to fire
                if (turret.multiDirectionalFire)
                {
                    // Find top-left camera position
                    int left = (int)Math.Floor(level.cameraPosition.X);
                    int top = (int)Math.Floor(level.cameraPosition.Y);
                    turret.turretVector = level.Player.Position - turret.Position;

                    // Normalize movement vector
                    if (turret.turretVector != Vector2.Zero)
                        turret.turretVector.Normalize();
                    Vector2 vector = this.turret.turretVector;
                    initialVelocityX = vector.X * MoveVelocity;
                    initialVelocityY = vector.Y * MoveVelocity;
                }
                else if (turret.currFiringDireciton == TurretFiringDirection.Right)
                    initialVelocityX = MoveVelocity;
                else
                    initialVelocityX = -MoveVelocity;

                // If bullet is from enemy and fires bouncing bullets or vertical bullets
                if (turret.enemy != null)
                {
                    if (turret.enemy.bouncingBullets)
                    {
                        bouncable = true;
                        if (turret.enemy.Velocity.Y < 0)
                            initialVelocityY = -MoveVelocity;
                        else
                            initialVelocityY = MoveVelocity;
                    }
                    else if (turret.enemy.fireVertical)
                    {
                        initialVelocityX = 0;
                        initialVelocityY = -MoveVelocity;
                        bulletRotation = MathHelper.ToRadians(270);
                    }
                }
            }
            
            LoadContent();
        }

        public void LoadContent()
        {
            string bulletFolder = "Tiles/Bullets/";
            // Load different bullet texture depending on gun
            if (powerBullet)
                bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "powerBullet");

            else if (PlayerBullet)
            {
                if (level.Player.inVehicle)
                    bulletTex = Level.Content.Load<Texture2D>(bulletFolder + level.GetTileSet() + "vehicleBullet");
                else
                {
                    switch (level.Player.currGun)
                    {
                        case Gun.Pistol:
                            bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "pistolBullet"); break;
                        case Gun.Uzi:
                            bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "uziBullet"); break;
                        case Gun.Sniper:
                            bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "sniperBullet"); break;
                        case Gun.Harpoon:
                            bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "harpoonBullet");
                            break;
                        case Gun.SpaceBlaster:
                            bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "spaceBlasterBullet"); break;
                        case Gun.Multidirectional:
                            bulletTex = Level.Content.Load<Texture2D>(bulletFolder + "multiDirectionalBullet"); break;
                        // default:
                        // bulletTex = Level.Content.Load<Texture2D>("Sprites/pistolBullet"); break;
                    }
                }
            }
            // If fired from turret load turret bullet
            else
            {
                //if ((turret.enemy != null && turret.enemy.fireVertical) || (enemy != null && enemy.fireVertical)) bulletRotation = MathHelper.ToRadians(270);
                    bulletTex = Level.Content.Load<Texture2D>(bulletFolder + level.GetTileSet() + "turretBullet");

            }

            // Load different bullet sound depending on gun
            if (powerBullet)
                shootSound = Level.Content.Load<SoundEffect>("Sounds/Explosion");
            else if (PlayerBullet)
            {
                switch (level.Player.currGun)
                {
                    case Gun.Pistol:
                        shootSound = Level.Content.Load<SoundEffect>("Sounds/Pistol"); break;
                    case Gun.Uzi:
                        shootSound = Level.Content.Load<SoundEffect>("Sounds/Uzi"); break;
                    case Gun.Sniper:
                        shootSound = Level.Content.Load<SoundEffect>("Sounds/Sniper"); break;
                    case Gun.Harpoon:
                        if (level.Player.inVehicle)
                            shootSound = Level.Content.Load<SoundEffect>("Sounds" + level.GetTileSet() + "/VehicleGun");
                        else
                            shootSound = Level.Content.Load<SoundEffect>("Sounds/Harpoon");
                        break;
                    case Gun.SpaceBlaster:
                        shootSound = Level.Content.Load<SoundEffect>("Sounds/SpaceBlaster"); break;
                    case Gun.Multidirectional:
                        if (level.Player.sniperMode)
                            shootSound = Level.Content.Load<SoundEffect>("Sounds/Sniper");
                        else
                            shootSound = Level.Content.Load<SoundEffect>("Sounds/Uzi"); break;
                    // deafult:
                    // bulletTex = Level.Content.Load<Texture2D>("Sprites/pistolBullet"); break;
                }
            }
            sniperShotAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/sniperShotAnimation"), 0.1f, false);
            blankAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/blankAnimation"), 0.3f, false);
            explosionTex = Level.Content.Load<Texture2D>("Tiles/explosion");
            reloadSound = Level.Content.Load<SoundEffect>("Sounds/GunReload");
            hitTarget = Level.Content.Load<SoundEffect>("Sounds/HitTarget");
        }

        public void Update(GameTime gameTime)
        {
             float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

             if (airTime == 0.0f && PlayerBullet)
                 Game.PlaySound(shootSound);
             airTime++;

             // For certain guns make reload sound after each shot
             if (airTime == 50.0f && PlayerBullet && (level.Player.currGun == Gun.Pistol || level.Player.currGun == Gun.Sniper || level.Player.sniperMode))
                 Game.PlaySound(reloadSound);
             
             // Set constant velocity (including air drag)
             velocity.X = initialVelocityX;
             velocity.Y = initialVelocityY;

             // Apply velocity.
             Position += velocity * elapsed;
             Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));


             // If in sniper mode have a delay before handling collisions
             if (level.Player.sniperMode && level.LevelIndex == 9)
             {
                 if (airTime == SniperDelayTime)
                 {
                     HandleCollisions();
                     accuracyPercentage = (float)((float)shotsOnTarget / (float)shotsFired) * 100f;
                 }
             }
             else
                 HandleCollisions();

             if (exploded)
             {
                 if (explodeTimer == MaxExplodeTime)
                     removeBullet = true;
                 else
                     explodeTimer++;
             }

            
             // If in sniper mode all bullets are stationary. Have a delay before they are removed from the level
             bool isStationary = (level.Player.sniperMode) ? airTime > SniperDelayTime : velocity.X == 0 && velocity.Y == 0;
             bool isOutOfBounds = position.X < 0 || position.X > level.Width * Tile.Width || position.Y < 0 || position.Y > level.Height * Tile.Height;
             // Stop bullet if:
             //    1. Bullet is out of bounds of level
             // OR 2. Bullet has no velocity
             // OR 3. Bullet is out of range
             // OR 4. Bullet is shot by a turret and hits a stopper block
             // OR 5. Bullet collides with an impassable block
             if (isStationary || isOutOfBounds || (Math.Abs(initialPosition.X - Position.X) > range) || removeBullet)
                 level.Bullets.Remove(this);
        }


        private void HandleCollisions()
        {
            // Get the bullet's bounding rectangle and find neighboring tiles.
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

                    // Only bounces horizontally at the moment
                    if (bouncable && collision == TileCollision.Impassable)
                            initialVelocityY = -initialVelocityY;

                    else
                    {
                        // If enemy bullet hits stopper tile
                        if (!playerBullet && collision == TileCollision.Stopper)
                            exploded = true;
                    }

                    // If player bullet hits impassable tile (at top - so doesn't explode when firing from floor)
                    if (playerBullet && collision == TileCollision.Impassable && y == topTile)
                        exploded = true;

                    // If any bullet hits a vertical moving platform make it explode
                    for (int i = 0; i < level.MovingPlatforms.Count; i++)
                    {
                        MovingPlatform platform = level.MovingPlatforms[i];

                        if (!platform.horizontalOrientation && platform.BoundingRectangle.Intersects(this.BoundingRectangle))
                            this.exploded = true;
                    }

                    // This is handled in level class if not in sniper mode (implemented here in sniper mode to allow sniper delay and also
                    // when implemented here better shots do more damage which is what we want in sniper mode but not normal shooting mode)
                    if (level.Player.sniperMode)
                    {
                        // Enemies are killed by player bullets (but not other enemy bullets)
                        for (int j = 0; j < level.Enemies.Count; ++j)
                        {
                            Enemy enemy = level.Enemies[j];
                            if (enemy.BoundingRectangle.Intersects(this.BoundingRectangle) && this.PlayerBullet) // && !enemy.died)
                            {
                                level.Bullets.Remove(this);
                                enemy.OnKilled(this);

                                // Update sniper stats
                                shotsOnTarget++;
                            }
                        }
                    }


                    if (collision == TileCollision.Target && playerBullet)
                    {
                        // Make target disappear and play sound
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                        // For targets on buildings replace with different tile
                        if (level.LevelChars[x, y] == '*')
                            level.Tiles[x, y] = level.LoadLevelTile("visualTile", TileCollision.Passable);
                        Game.PlaySound(hitTarget);

                        switch (level.LevelIndex)
                        {
                            // Special instance for level 1 (Basic Training 2)
                            case 1:
                                LevelTargetCount++;
                                if (LevelTargetCount == Level1TargetCountMax)
                                {
                                    // Display message
                                    Level.Player.InfoString = "Oh it appears the lasers have broken, you'll have to try and jump through.";
                                    Level.Player.DrawInfo = true;
                                    // Make laser flash
                                    for (int i = 0; i < 21; i++)
                                    {
                                        if (i == 9) i = 11;
                                        level.RemoveLaser(9, 2 + i);
                                        level.LoadLaserTile(9, 2 + i, false, true, true, RotationDirection.None);
                                    }
                                }
                                break;

                            // Special instance for level 3 (Office level)
                            case 3:
                                // Remove laser
                                for (int i = 0; i < 2; i++)
                                    level.RemoveLaser(28, 15 + i);
                                 break;

                            // Special instance for level 4 (Space level)
                            case 4:
                                 LevelTargetCount++;

                                // First half of level
                                if (LevelTargetCount == 2)
                                {
                                    if (x == 219 && (y == 15 || y == 20))
                                    {
                                    // Display message
                                    Level.Player.InfoString = "Nice shot!";
                                    Level.Player.DrawInfo = true;
                                    // Remove laser
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(229, 14 + i);
                                    }
                                }

                                // Second half of level
                                if (LevelTargetCount == 3)
                                {
                                     // Remove laser
                                    for (int i = 0; i < 24; i++)
                                        level.RemoveLaser(90, 19 + i);
                                }
                                if (LevelTargetCount == 6)
                                {
                                    // Remove laser
                                    for (int i = 0; i < 25; i++)
                                        level.RemoveLaser(77, 3 + i);
                                }

                                break;

                            case 5:
                                LevelTargetCount++;

                                // First section of level
                                if (LevelTargetCount == 3)
                                {
                                        // Remove laser
                                        for (int i = 0; i < 3; i++)
                                            level.RemoveLaser(146, 15 + i);
                                }

                                // Snowmobile section
                                if (x == 77 && y == 123)
                                {
                                    // Remove laser
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(90, 122 + i);
                                }

                                if (x == 216 && y == 140)
                                {
                                    // Remove laser
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(196, 137 + i);
                                    LevelTargetCount = 4;
                                }

                                if (LevelTargetCount == 6)
                                {
                                    // Remove laser
                                    for (int i = 0; i < 3; i++)
                                        level.RemoveLaser(170, 140 + i);
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(170, 144 + i);
                                }
                                break;

                            // Haunted house level
                            case 6:
                                LevelTargetCount++;
                                if (LevelTargetCount == 1)
                                    level.RemoveLaser(108, 3);
                                if (LevelTargetCount == 2)
                                {
                                    // Remove laser 1
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(116, 3 + i);

                                    // Remove laser 2
                                    for (int i = 0; i < 3; i++)
                                        level.RemoveLaser(119 + i, 5);

                                    // Add laser
                                    for (int i = 0; i < 2; i++)
                                        level.LoadLaserTile(72, 1 + i, true, true, false, RotationDirection.None);

                                    // Add moving spikes
                                    for (int i = 0; i < 5; i++)
                                        Level.LoadMovingSpikeTile(127, 1 + i, true, false, false);
                                }
                                break;

                            case 8:
                                LevelTargetCount++;
                                level.Tiles[x, y] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                                if (LevelTargetCount >= 3)
                                {
                                    for (int i = 0; i < 6; i++)
                                    {
                                        level.RemoveLaser(778, 4 + i);
                                        level.RemoveLaser(778, 15 + i);
                                    }
                                }

                                if (LevelTargetCount >= 4)
                                {
                                    for (int i = 0; i < 8; i++)
                                        level.RemoveLaser(875, 13 + i);
                                }

                                break;

                            case 9:
                                targetsHit++;
                                shotsOnTarget++;

                                break;

                            case 11:
                                LevelTargetCount++;
                                if (LevelTargetCount >= 3)
                                {
                                    for (int i = 0; i < 6; i++)
                                        level.RemoveLaser(58 + i, 41);
                                }
                                break;

                            case 12:
                                LevelTargetCount++;
                                if (LevelTargetCount >= 2)
                                {
                                    for (int i = 0; i < 9; i++)
                                    {
                                        level.RemoveLaser(100 + i, 25);
                                        level.RemoveLaser(118 + i, 25);
                                    }
                                }
                                break;

                            // Special instance for bonus level
                            //case 8:
                                // Remove laser  
                                 //for (int i = 0; i < 3; i++)
                                    //level.RemoveLaser(x + 8, y - i);

                                //break;
                        }
                    }
                }
            }
        }


        // Resets target count to zero and sniper stats
        public static void ResetTargetCounts()
        {
            LevelTargetCount = 0;

            // Reset sniper stats
                shotsFired = 0;
                shotsOnTarget = 0;
                accuracyPercentage = 0;
                enemiesKilled = 0;
                enemiesMax = 8;
                targetsHit = 0;
                targetMax = 8;

        }

        public Rectangle BoundingRectangle
        {
            get { return new Rectangle((int)(position.X - Origin.X), (int)(position.Y - Origin.Y), bulletTex.Width, bulletTex.Height); }
        }

        // Gets a texture origin at bottom center
        public Vector2 Origin
        {
            get { return new Vector2(bulletTex.Width / 2.0f, bulletTex.Height); }
        }

        //public void OnContact(Player contactBy)
        //{
            //contactBy.OnKilled(null);
        //}


        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            /*
            //Debugging rectangle
            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, BoundingRectangle.Width, BoundingRectangle.Height);

            Color[] data = new Color[BoundingRectangle.Width * BoundingRectangle.Height];
            for (int i = 0; i < data.Length; ++i) data[i] = Color.Chocolate;
            rect.SetData(data);

            Vector2 coor = new Vector2(BoundingRectangle.X, BoundingRectangle.Y);
            spriteBatch.Draw(rect, coor, Color.White);
             */


                // Flip the bullet to face the way it is moving
                if (Velocity.X > 0)
                    flip = SpriteEffects.None;
                else if (Velocity.X < 0)
                    flip = SpriteEffects.FlipHorizontally;
                
                //spriteBatch.Draw(bulletTex, Position, null, Color.White, 0.0f, Origin, 1.0f, flip, 0.0f);

                // Draws sniper bullet animation (+ slight offset for bullet position)
                if (level.Player.sniperMode)
                {
                    // NB Decide which to put first
                    if (level.LevelIndex == 9)
                        animationPlayer.Draw(gameTime, spriteBatch, Position + new Vector2(0, sniperShotAnimation.FrameHeight / 2), flip);
                    spriteBatch.Draw(bulletTex, Position + new Vector2(0, bulletTex.Height / 2), null, Color.White, bulletRotation, Origin, 1.0f, flip, 0f);
                }
                else
                    spriteBatch.Draw(bulletTex, Position, null, Color.White, bulletRotation, Origin, 1.0f, flip, 0f);


                // Draw explosion at end of bullet's range
                if ((Math.Abs(initialPosition.X - Position.X) > range-25) || exploded)
                    spriteBatch.Draw(explosionTex, Position + new Vector2(-16, 0), null, Color.White, 0.0f, Origin, 1.0f, flip, 0.0f);

               
        }
    }
}
