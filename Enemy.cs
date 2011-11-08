using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace RedMan
{
    /*
     * Facing direction along the X axis.
     */
    enum FaceDirection
    {
        Left = -1,
        Right = 1,
    }

    /*
     * Implements enemy and boss movement, health and graphics
     */
    class Enemy
    {
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
        Vector2 position, startPosition;

        public Vector2 Velocity
        {
            get { return velocity; }
        }
        Vector2 velocity;

        // True is this enemy was created when player hit an event switch
        public bool createdByEvent;

        public static int level12bossCount = 0;

        private Rectangle localBounds;
        // Gets a rectangle which bounds this enemy in world space.
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                // Explosive mines and bosses have larger bounding rectangle (covers full tile)
                if (monsterType == "MonsterD" || isBoss)
                    return new Rectangle((int)(position.X - sprite.Origin.X), (int)(position.Y - sprite.Origin.Y), walkAnimation.FrameWidth, walkAnimation.FrameHeight);

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        // Gets an upper bounding rectangle which bounds this enemy in world space.
        public Rectangle UpperBoundingRectangle
        {
            get
            {
                    return new Rectangle((int)(position.X - sprite.Origin.X), (int)(position.Y - sprite.Origin.Y - Tile.Height ), walkAnimation.FrameWidth, walkAnimation.FrameHeight);
            }
        }

        // Animations
        private Animation walkAnimation;
        private Animation idleAnimation;
        private Animation dieAnimation;
        private AnimationPlayer sprite;
        private SoundEffect deathSound;

        public string monsterType;
        bool moveVertical = false;
        int vertOffset;
        public bool died;
        private int deathTimer;
        private const int DeadTime = 30;
        private bool noGravity;

        public bool isBoss, hardEnemy;
        private float bossHealth;
        private float bossMaxHealth = 100.0f;
        private float timeSinceShot;
        private int shotsToKill;
        public bool bouncingBullets, fireVertical, multiDirectoinalFiring;
        // Turrets associated with a single enemy (ie may have multiple)
        private Turret[] enemyTurrets;

        private const int BossReloadTime = 50;
        private const int EnemyReloadTime = 100;
        private int bossReloadTime = BossReloadTime;

        private const int EnemyVisionHeight = 10;
        private const int EnemyVisionWidth = 200;


        // The direction this enemy is facing and moving along the X axis.
        public FaceDirection direction = FaceDirection.Left;

        // How long this enemy has been waiting before turning around.
        private float waitTime;

        // How long to wait before turning around.
        private const float MaxWaitTime = 0.5f;

        // The speed at which this enemy moves along the X or Y axis.
        private float currMoveSpeed;
        private const float EnemySpeed = 64.0f;
        private const float BossSpeed = 100.0f;

        // Constructs a new Enemy.
        public Enemy(Level level, Vector2 position, string spriteSet, bool createdByEvent)
        {
            this.level = level;
            this.position = position;
            this.startPosition = position;
            this.monsterType = spriteSet;
            this.createdByEvent = createdByEvent;
            this.currMoveSpeed = EnemySpeed;
            this.bouncingBullets = false;
            this.fireVertical = false;
            this.multiDirectoinalFiring = false;
            this.enemyTurrets = new Turret[2];

            SetupEnemy();

            LoadContent(spriteSet);

            // For bosses other continously firing enemies make turrets to act as their guns
            if (isBoss)
            {
                    Vector2 turretOffset = new Vector2(BoundingRectangle.X + BoundingRectangle.Width / 2, BoundingRectangle.Y + BoundingRectangle.Height / 2);

                    // Water boss - fires 2 parallel guns
                    if (monsterType == "Boss1")
                    {
                        Vector2 upperGun = new Vector2(BoundingRectangle.X + BoundingRectangle.Width / 2, BoundingRectangle.Y + BoundingRectangle.Height * 0.25f);
                        Vector2 lowerGun = new Vector2(BoundingRectangle.X + BoundingRectangle.Width / 2, BoundingRectangle.Y + BoundingRectangle.Height * 0.75f);
                        enemyTurrets[0] = new Turret(level, upperGun, false, this, false);
                        enemyTurrets[1] = new Turret(level, lowerGun, false, this, false);
                    }

                    // Space boss - fires bouncing bullets
                    else if (monsterType == "Boss2")
                    {
                        this.bouncingBullets = true;
                        enemyTurrets[0] = new Turret(level, turretOffset, false, this, false);
                    }

                    // Witch boss - fires upwards
                    else if (monsterType == "Boss3")
                    {
                        this.fireVertical = true;
                        enemyTurrets[0] = new Turret(level, turretOffset, false, this, false);
                    }

                    // Angel boss
                    else if (monsterType == "Boss4")
                    {
                        enemyTurrets[0] = new Turret(level, turretOffset, false, this, true);
                    }

                    // Devil boss
                    else if (monsterType == "Boss5" || monsterType == "Boss5R")
                    {
                        switch (level12bossCount)
                        {
                            case 0:
                                this.bouncingBullets = true;
                                enemyTurrets[0] = new Turret(level, turretOffset, false, this, false);
                                break;
                            case 1:
                                //this.bouncingBullets = false;
                                this.fireVertical = true;
                                enemyTurrets[0] = new Turret(level, turretOffset, false, this, true);
                                break;
                            case 2:
                                this.bouncingBullets = true;
                                Vector2 upperGun = new Vector2(BoundingRectangle.X + BoundingRectangle.Width / 2, BoundingRectangle.Y + BoundingRectangle.Height * 0.25f);
                                Vector2 lowerGun = new Vector2(BoundingRectangle.X + BoundingRectangle.Width / 2, BoundingRectangle.Y + BoundingRectangle.Height * 0.75f);
                                enemyTurrets[0] = new Turret(level, upperGun, false, this, true);
                                enemyTurrets[1] = new Turret(level, lowerGun, false, this, false);
                                break;

                        }
                    }

                    // Multidirecitional firing enemies
                    else if (multiDirectoinalFiring)
                    {
                        enemyTurrets[0] = new Turret(level, turretOffset, false, this, true);
                    }
                   
                }
        }

        // Classifies each enemy depending on type an sets relevant charateristics
        private void SetupEnemy()
        {
            String m = "Monster";
            String b = "Boss";
            String mt = monsterType;

            if (mt == m + "D" || mt == b + "1" || mt == b + "2" || mt == b + "4" || mt == m + "H" || mt == m + "N")
                moveVertical = true;

            if (mt == m+"B" || mt == b+"3" || mt == m+"Q" || mt == m+"R" || mt == m+"L" || mt == m+"U" || mt == m+"Y")
                noGravity = true;

            // Manage different bosses in level 12 (Final hell level)
            if (level.LevelIndex == 12 && (mt == b + "5" || mt == b+"5R"))
            {
                switch (level12bossCount)
                {
                    case 0:
                        moveVertical = true;
                        break;
                    case 1:
                        moveVertical = false;
                        noGravity = true;
                        break;
                    case 2:
                        moveVertical = true;
                        break;
                }
            }

            // If monster in moving vertical gravity must not apply
            if (moveVertical)
                noGravity = true;

            // Make certain cars in race level go right
            if (mt == m+"R" || mt == m+"U" || mt == m+"Q")
                direction = FaceDirection.Right;

            if (mt == m+"Q" || mt == m+"U")
                multiDirectoinalFiring = true;

            // Set boss and harder enemy characteristics
            if (mt.Substring(0, 4) == b || mt == m + "M" || mt == m + "P" || mt == m+"Q" || mt == m+"U" || mt == m+"Y")
            {
                isBoss = true;
                this.currMoveSpeed = BossSpeed;
                // If is a monster (not a boss) make enemy a 'hard' enemy
                if (mt.Substring(0, 4) == m)
                {
                    hardEnemy = true;
                    bossReloadTime = EnemyReloadTime;
                }
                this.bossHealth = bossMaxHealth;
                this.shotsToKill = GetShotsToKill();
            }

        }

        // Gets number of shots required to kill specific enemies
        private int GetShotsToKill()
        {
            switch (monsterType)
            {
                case "Boss1": return 30;
                case "Boss2": return 20;
                case "Boss3": return 10;
                case "Boss5": return 8;
                case "Boss5R": return 2;
                case "MonsterM": return 4;
                case "MonsterU": return 8;
                case "MonsterQ": return 8;

                default: return 4;
            }
        }

        // Loads a particular enemy sprite sheet and sounds.
        public void LoadContent(string spriteSet)
        {
            // Load animations.
            spriteSet = "Sprites/" + spriteSet + "/";
            walkAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Walk"), 0.1f, true);
            idleAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Idle"), 0.15f, true);
            dieAnimation = new Animation(Level.Content.Load<Texture2D>(spriteSet + "Die"), 0.15f, false);
            sprite.PlayAnimation(idleAnimation);

            if (monsterType == "MonsterD")
                deathSound = Level.Content.Load<SoundEffect>("Sounds/Explosion");
            else
                deathSound = Level.Content.Load<SoundEffect>("Sounds/MonsterKilled");

            // Calculate bounds within texture size.
            int width = (int)(idleAnimation.FrameWidth * 0.35);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameHeight * 0.7);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);
        }


        // Paces back and forth along a platform, waiting at either end.
        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // If has been dead for a certain time, remove enemy from level
            if (died)
            {
                // Remove turrets associated with enemy if there is any
                   for (int i = 0; i < enemyTurrets.Length; i++)
                   {
                        Turret turret = enemyTurrets[i];
                        if (turret != null)
                            turret.Remove();
                    }
                deathTimer++;
                if (deathTimer > DeadTime)
                {
                    Level.Enemies.Remove(this);
                    // Update player score
                    if (!createdByEvent)
                        Level.Score += 30;
                    // Update sniper stats
                    if (level.inSniperMode())
                        Bullet.enemiesKilled++;
                }
            }

            else
            {

                // Calculate tile position based on the side we are walking towards.
                float posX = Position.X + localBounds.Width / 2 * (int)direction;
                int tileX = (int)Math.Floor(posX / Tile.Width) - (int)direction;
                int tileY = (int)Math.Floor(Position.Y / Tile.Height);
                int startX = (int)Math.Floor(startPosition.X / Tile.Width);
                int startY = (int)Math.Floor(startPosition.Y / Tile.Height);

                // Replace start tile with a appropriate tile depending on situation
                if (tileX != startX)
                {
                    switch (level.LevelIndex)
                    {
                        case 1:
                            if (monsterType == "MonsterB")
                                level.Tiles[startX, startY - 1] = level.LoadLevelTile("waterTile", TileCollision.Passable);
                            else
                                level.Tiles[startX, startY - 1] = new Tile(null, TileCollision.Passable);
                            break;
                        case 2:
                            level.Tiles[startX, startY - 1] = new Tile(null, TileCollision.Water);
                            break;
                        // Car race level
                        case 8:
                            if (monsterType == "MonsterQ")
                                level.Tiles[startX, startY - 1] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                            else if (monsterType == "MonsterU")
                                level.Tiles[startX, startY - 1] = level.LoadLevelTile("grassTile", TileCollision.Passable);
                            else
                                level.Tiles[startX, startY - 1] = new Tile(null, TileCollision.Passable);
                            break;
                        case 9:
                            if (monsterType == "MonsterQ")
                                level.Tiles[startX, startY - 1] = new Tile(null, TileCollision.Passable);
                            break;
                       
                        default:
                            level.Tiles[startX, startY - 1] = new Tile(null, TileCollision.Passable);
                            break;
                    }
                }
                
                // NB implement this using a 'vision rectangle' later
                // For hard enemys which do not fire continously add a bullet only whenplayer is within a certain range
                if (hardEnemy)
                {
                    timeSinceShot++;
                    if (timeSinceShot > bossReloadTime)
                    {
                        if ( !multiDirectoinalFiring && this.position.Y + EnemyVisionHeight > level.Player.Position.Y && this.position.Y - EnemyVisionHeight < level.Player.Position.Y
                       && this.position.X - EnemyVisionWidth < level.Player.Position.X && this.position.X + EnemyVisionWidth > level.Player.Position.X)
                        {
                            Vector2 gun = new Vector2(BoundingRectangle.X + BoundingRectangle.Width / 2, BoundingRectangle.Y + BoundingRectangle.Height / 2);
                            level.Bullets.Add(new Bullet(level, gun, gameTime, this));
                        }
                        timeSinceShot = 0;
                    }
                }
                 

                if (waitTime > 0)
                {
                    // Wait for some amount of time.
                    waitTime = Math.Max(0.0f, waitTime - (float)gameTime.ElapsedGameTime.TotalSeconds);
                    if (waitTime <= 0.0f)
                    {
                        // Then turn around.
                        direction = (FaceDirection)(-(int)direction);
                    }
                   
                }
                else
                {
                    // If moving vertically (& noGravity) sets vertical offset depending on direction
                    if (noGravity && moveVertical)
                        if ((int)direction == 1)
                            vertOffset = 0;
                        else vertOffset = -1*walkAnimation.FrameHeight/Tile.Height; // Works for any size enemy

                     // NB Make this into a vision rectangle and check for intersections
                     // If hard enemy turn around when on same y cordinates as player and within certain x range
                    if (hardEnemy && this.position.Y + EnemyVisionHeight > level.Player.Position.Y && this.position.Y - EnemyVisionHeight < level.Player.Position.Y
                        && this.position.X - EnemyVisionWidth < level.Player.Position.X && this.position.X + EnemyVisionWidth > level.Player.Position.X)
                    {
                        if (level.Player.Position.X < this.position.X)
                            direction = FaceDirection.Left;
                        else direction = FaceDirection.Right;
                    }

                    // If player jumps on (easy) enemy, enemy is killed
                    if (!isBoss && !hardEnemy && !level.inCarRaceMode() && !noGravity && this.UpperBoundingRectangle.Intersects(level.Player.BoundingRectangle))
                        this.OnKilled(null);

                    // For last boss
                    if (monsterType == "Boss5R" && Bullet.LevelTargetCount >= 2)
                        this.fireVertical = false;

                    // If about to swim into wall turn around
                    if (noGravity && !moveVertical && Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable)
                    {
                            waitTime = MaxWaitTime;
                    }

                    // For vertically moving enemies in water
                    else if (noGravity && moveVertical && Level.GetCollision(tileX + (int)direction , tileY + vertOffset )  == TileCollision.Impassable )
                    {
                            waitTime = MaxWaitTime;
                    }

                    // If we are about to run into a wall or off a cliff, start waiting.
                    else if (!noGravity && (Level.GetCollision(tileX + (int)direction, tileY - 1) == TileCollision.Impassable ||
                        Level.GetCollision(tileX + (int)direction, tileY) == TileCollision.Passable))
                    {
                        waitTime = MaxWaitTime;
                    }
                    else
                    {
                        // Move in the current direction.
                        float speed = (int)direction * currMoveSpeed * elapsed;

                        // In car race level make monsterU move as fast as players car
                        if (level.inCarRaceMode() && (monsterType == "MonsterU" || monsterType == "MonsterQ"))
                            speed *= 3.8f;

                        if (moveVertical)
                            velocity = new Vector2(0.0f, speed);
                        else
                            velocity = new Vector2(speed, 0.0f);

                        position += velocity;
                    }
                }
            }
        }

        // Called when the enemy has been killed.
        public void OnKilled(Bullet bullet)
        {
            if (isBoss)
            {
                if (bullet != null && bullet.powerBullet)
                {
                    if (Bullet.DropBomb)
                        bossHealth -= bossMaxHealth / shotsToKill;
                    else
                        bossHealth -= bossMaxHealth * 0.25f;
                }
                else
                    bossHealth -= bossMaxHealth / shotsToKill;
                if (bossHealth <= 1 && !died)
                {
                    Game.PlaySound(deathSound);
                    died = true;

                    HandleBossDeath();
                }
            }
            else
            {
                if (!died)
                {
                    Game.PlaySound(deathSound);
                    died = true;
                }
            }

        }

        // Handles any events which may need to occur when a boss dies (opening doors etc.)
        private void HandleBossDeath()
        {
            switch (level.LevelIndex)
            {
                case 2:
                    for (int i = 0; i < 4; i++)
                        level.RemoveLaser(219, 15 + i);
                    for (int i = 0; i < 4; i++)
                        level.RemoveLaser(227, 15 + i);
                    break;
                case 4:
                    for (int i = 0; i < 9; i++)
                        level.RemoveLaser(33 + i, 43);
                    break;
                case 6:
                    for (int i = 0; i < 3; i++)
                        level.RemoveLaser(155, 11 + i);
                    break;
                case 12:
                    level12bossCount++;
                    switch (level12bossCount)
                    {
                        case 1:
                            for (int i = 0; i < 11; i++)
                                level.RemoveLaser(97 + i, 13);
                            level.LoadEnemyTile(113, 30, "Boss5R", false);
                            break;

                        case 2:
                            for (int i = 0; i < 6; i++)
                                level.RemoveLaser(135, 27 + i);
                            level.LoadEnemyTile(161, 31, "Boss5", false);
                            break;

                        case 3:
                            for (int i=0; i < 4; i++)
                                level.RemoveLaser(178, 21 + i);

                            break;
                    }
                    break;
            }
        }

        // Removes enemy from level with no sound
        public void Remove()
        {
            died = true;
        }

        // Draws the animated enemy.
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (died)
                sprite.PlayAnimation(dieAnimation);

            else
            {
                // Stop running when the game is paused or before turning around.
                if (!Level.Player.IsAlive ||
                    Level.ReachedExit ||
                    Level.TimeRemaining == TimeSpan.Zero ||
                    waitTime > 0)
                {
                    sprite.PlayAnimation(idleAnimation);
                }
                else
                {
                    sprite.PlayAnimation(walkAnimation);
                }

                if (isBoss)
                    drawHealthBar(spriteBatch);
            }
            // Draw facing the way the enemy is moving.
            SpriteEffects flip;
            if (!moveVertical)
                flip = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            else
                flip = SpriteEffects.None;
            sprite.Draw(gameTime, spriteBatch, Position, flip);


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


        // Draws enemy (boss) health bar
        public void drawHealthBar(SpriteBatch spriteBatch)
        {
            int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
            int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

            int healthBarWidth = (int) (walkAnimation.FrameWidth * bossHealth/bossMaxHealth);
            int healthBarHeight = (int)(walkAnimation.FrameHeight/10);

            Color healthBarColor;
            if (bossHealth / bossMaxHealth >= 0.8f)
                healthBarColor = Color.Green;
            else if (bossHealth / bossMaxHealth >= 0.5f)
                healthBarColor = Color.Yellow;
            else if (bossHealth / bossMaxHealth >= 0.2f)
                healthBarColor = Color.Orange;
            else
                healthBarColor = Color.Red;

            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, healthBarWidth, healthBarHeight);

            Color[] data = new Color[healthBarWidth * healthBarHeight];
            for (int i = 0; i < data.Length; ++i)
                data[i] = healthBarColor;
            rect.SetData(data);

            Vector2 coor = new Vector2(position.X - walkAnimation.FrameWidth/2, position.Y - walkAnimation.FrameHeight - healthBarHeight - 5);
            spriteBatch.Draw(rect, coor, Color.White);
        }

    }
}

