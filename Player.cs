using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
//using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace RedMan
{
    /*
     * Gun Types
     */
    enum Gun { Pistol, Uzi, Sniper, Harpoon, SpaceBlaster, Multidirectional };

    /*
     * Vehicle Types
     */
    enum Vehicle { Submarine, Spaceship, Snowmobile, Broomstick, Car, Wings};

    /*
     * Implements graphics and physics for player
     */
    class Player
    {
        // Animations
        private Animation idleAnimation;
        private Animation walkAnimation;
        private Animation sprintAnimation;
        private Animation jumpAnimation;
        private Animation duckAnimation;
        private Animation climbAnimation;
        private Animation climbIdleAnimation;
        private Animation monkeyBarAnimation;
        private Animation monkeyBarIdleAnimation;
        private Animation celebrateAnimation;
        private Animation rollAnimation;
        private Animation swimAnimation;
        private Animation swimIdleAnimation;
        private Animation flyAnimation;
        private Animation dieAnimation;
        private Animation vehicleAnimation;


        // Sudo 3D extra animations
        public bool inSudo3D;
        private Animation walkUpAnimation;
        private Animation walkDownAnimation;

        private Texture2D swingPoleTex;
        private Vector2 swingOrigin;
        private float RotationAngle;

        
       
        private SpriteEffects flip = SpriteEffects.None;
        private AnimationPlayer sprite;

        // Sounds
        private SoundEffect killedSound;
        private SoundEffect jumpSound;
        private SoundEffect boingSound;
        private SoundEffect fallSound;
        private SoundEffect gunReloadSound;
        private SoundEffect checkpointSound;

        private SpriteFont infoFont;

        public Level Level
        {
            get { return level; }
        }
        Level level;

        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        // Score for the whole game so far
        public static int Score;

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 13000.0f;
        private const float MaxMoveSpeed = 1750.0f;
        private const float GroundDragFactor = 0.48f;
        private const float AirDragFactor = 0.58f;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.35f;
        private const float JumpLaunchVelocity = -3500.0f;
        private const float GravityAcceleration = 3400.0f;
        private const float MaxFallSpeed = 550.0f;
        private const float JumpControlPower = 0.14f;
        private const float MaxTrampolineJumpTime = 5f;
        private const float BounceLaunchVelocity = -15000.0f;
        private const float BounceControlPower = 10f;

        private float timeSinceShot = 50;

        // Constants for controlling vertical movement in space
        private const float SpaceMaxJumpTime = 2.0f;
        private const float SpaceGravityAcceleration = 5000.0f;
        private const float SpaceMaxFallSpeed = 150.0f;
        private const float SpaceJumpLaunchVelocity = -200.0f;

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return isOnGround; }
            set { isOnGround = value; }
        }
        bool isOnGround;

        /// <summary>
        /// Current user movement input.
        /// </summary>
        private float movement;
        public float vertMovement;

        // Player state
        private float playerHealth;
        private float playerMaxHealth = 100.0f;

        public bool isJumping;
        private bool wasJumping;
        private float jumpTime;
        private bool isDucking;
        private bool onLadder;
        private bool onMonkeyBar;
        public bool fallDown;
        private bool onSlope;
        private bool wasOnSlope;
        public bool isSprinting;
        private bool onSwingPole;
        private bool swingPoleRight;
        private bool onTrampoline;
        private bool onRightConveyor;
        private bool onLeftConveyor;
        private bool inWind;
        private float windCounter = 0;
        private bool inSpace;
        private bool inAirLock;
        private bool onRamp;
        private const float RampDelay = 150;
        private float rampTimer = RampDelay;
        private bool onOilSlick;
        private const float OilSlickDelay = 100;
        private float oilSlickTimer = OilSlickDelay;
        private bool onBooster;
        private const float BoosterDelay = 1000;
        private float boosterTimer = BoosterDelay;

        public bool shootWithMouse = false;
        public Vector2 mousePos;
        public Vector2 bulletVector;

        // Position of player within game window (ie not whole level)
        public Vector2 relativePosition;

        // Sublevel info
        public bool atDoor;
        public int levelToLoad;

        // Vehicle state
        public bool inVehicle;
        public Vehicle currVehicle;

        // Automatic vehicles move by themselves (horizontally)
        private bool inAutomaticVehicle;
        private bool automaticVehicleRight;
        private float automaticVehicleSpeed;
        public bool carRaceMode;

        private const float NormalVehicleSpeed = 3;

        // Automatic player variables
        private bool automaticRun;
        private bool automaticRunRight;
        private bool automaticRunUp = false;
        private float automaticRunSpeed;
        private float automaticRunVertSpeed = 4;
        private bool automaticJump;
        private List<Vector2> automaticJumpPoints = new List<Vector2>();

        private const float NormalRunSpeed = 1;
        private const float FastRunSpeed = 2;
        

        // Weapon state
        public bool hasGun;
        public int powerBulletCount;
        private bool isShooting;
        public Gun currGun;
        private float reloadTime;
        public bool sniperMode;

        // Level specific
        public bool showStats;
        public int level2AmuletCount = 0;
        public int level2AmuletMax = 5;
        public int level4StarCount = 0;
        public int level4StarMax = 5;
        public int level6PumpkinCount = 0;
        public int level6PumpkinMax = 6;
        public int level11StarCount = 0;
        public int level11StarMax = 4;

        private bool periodicLoop = false;
        private int periodCounter = 199;


        public bool DrawInfo
        {
            get { return drawInfo; }
            set { drawInfo = value; }
        }
        bool drawInfo;

        public string InfoString
        {
            get { return infoString; }
            set { infoString = value; }
        }
        string infoString;

        private int infoTimer;
        private const int InfoTimerMax = 300;
        

        public bool FacingRight
        {
            get { return facingRight; }
        }
        bool facingRight = true;

        public bool IsSwimming
        {
            get { return isSwimming; }
        }
        bool isSwimming;

        public bool OnWaterSurface
        {
            get { return onWaterSurface; }
        }
        bool onWaterSurface;

        private Rectangle localBounds;

        // Gets a rectangle which bounds this player in world space.
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X);
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y);

                return new Rectangle(left, top, idleAnimation.FrameWidth, idleAnimation.FrameHeight);
            }
        }

        // Gets a small rectangle which bounds this player in world space.
        public Rectangle SmallBoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X + localBounds.X);
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y + localBounds.Y);

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

  
        // Constructors a new player.
        public Player(Level level, Vector2 position)
        {
            this.level = level;
            this.showStats = true;
            this.inSudo3D = level.inSudo3D();

            hasGun = false;

            this.carRaceMode = level.inCarRaceMode();
            if (carRaceMode)
            {
                inVehicle = true;
                currVehicle = Vehicle.Car;
                inAutomaticVehicle = true;
                automaticVehicleRight = true;
                automaticVehicleSpeed = NormalVehicleSpeed;
            }

            this.automaticRun = level.inRunningMode();
            if (automaticRun)
            {
                automaticRunRight = true;
                automaticRunSpeed = NormalRunSpeed;
                this.playerHealth = playerMaxHealth;
            }

            sniperMode = level.inSniperMode();
            if (level.LevelIndex == 10)
                AssignAutomaticJumpPoints();
            LoadContent();

            Reset(position);
        }

        // Special case for level 10
        // Adds all automated jump point to list of vectors
        private void AssignAutomaticJumpPoints()
        {
            CreateJumpPoint(10, 23);
            CreateJumpPoint(37, 23);
            CreateJumpPoint(41, 21);
            CreateJumpPoint(54, 19);
            CreateJumpPoint(98, 21);
            CreateJumpPoint(105, 18);
            CreateJumpPoint(119, 20);
            CreateJumpPoint(127, 18);
            CreateJumpPoint(161, 11);
            CreateJumpPoint(173, 9);
            CreateJumpPoint(190, 23);
            CreateJumpPoint(224, 21);
            CreateJumpPoint(240, 17);
            CreateJumpPoint(254, 17);
            CreateJumpPoint(264, 9);
            CreateJumpPoint(274, 13);
            CreateJumpPoint(262, 21);
            CreateJumpPoint(279, 21);
            CreateJumpPoint(345, 16);
            CreateJumpPoint(367, 16);
            CreateJumpPoint(392, 21);
            CreateJumpPoint(403, 18);
            CreateJumpPoint(447, 22);
            CreateJumpPoint(423, 18);
            CreateJumpPoint(431, 14);
            CreateJumpPoint(433, 12);
            CreateJumpPoint(436, 14);
            CreateJumpPoint(437, 5);
        }

        // Helper method for AssignAutomaticJumpPoints()
        private void CreateJumpPoint(int x, int y)
        {
            automaticJumpPoints.Add(new Vector2(x, y));
        }

        // Loads the player sprite sheet and sounds.
        public void LoadContent()
        {
            // Gets whether in space/water etc.
            UpdateLevelSpecificState();

            if (carRaceMode)
            {
                idleAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "vehicle"), 0.1f, false);
                walkAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "vehicle"), 0.1f, true);
                jumpAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "jump"), 0.15f, false);
                rollAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "spin"), 0.1f, true);
                //celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);
                dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false);
            }
            else
            {

                // Load animated textures depending on level
                idleAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Idle"), 0.1f, true);
                walkAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Walk"), 0.1f, true);
                jumpAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Jump"), 0.1f, false);
                duckAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Duck"), 0.1f, false);
                sprintAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Sprint"), 0.1f, true);
                rollAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Roll"), 0.1f, true);
                vehicleAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "vehicle"), 0.1f, true);
                climbAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Climb"), 0.1f, true);
                climbIdleAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Climb"), 0.1f, false);

                if (inSudo3D)
                {
                    walkUpAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "WalkUp"), 0.1f, true);
                    walkDownAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "WalkDown"), 0.1f, true);
                }


                celebrateAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Celebrate"), 0.1f, false);

                if (level.LevelIndex == 4 || level.LevelIndex == 5 || level.LevelIndex == 10)
                    dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die2"), 0.1f, false);
                else
                    dieAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Die"), 0.1f, false);

                // If playing as square player
                if (level.LevelIndex == 10)
                {
                    monkeyBarAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "MonkeyBar"), 0.1f, true);
                    monkeyBarIdleAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "MonkeyBar"), 0.1f, false);
                }
                else
                {
                    monkeyBarAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/MonkeyBar"), 0.1f, true);
                    monkeyBarIdleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/MonkeyBar"), 0.1f, false);
                }

                swimAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Swim"), 0.1f, true);
                swimIdleAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/SwimIdle"), 0.1f, true);

                // If playing as square player
                if (level.LevelIndex == 10)
                    flyAnimation = new Animation(Level.Content.Load<Texture2D>(GetPlayerTheme() + "Fly"), 0.1f, true);
                else
                    flyAnimation = new Animation(Level.Content.Load<Texture2D>("Sprites/Player/Fly"), 0.1f, true);


                swingPoleTex = level.Content.Load<Texture2D>("Sprites/Player/Swing");
                swingOrigin.X = swingPoleTex.Width / 2;
                swingOrigin.Y = swingPoleTex.Height / 10;
            }

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            killedSound = Level.Content.Load<SoundEffect>("Sounds/PlayerKilled");
            jumpSound = Level.Content.Load<SoundEffect>("Sounds/PlayerJump");
            boingSound = Level.Content.Load<SoundEffect>("Sounds/Boing");
            fallSound = Level.Content.Load<SoundEffect>("Sounds/PlayerFall");
            gunReloadSound = Level.Content.Load<SoundEffect>("Sounds/GunReload");
            checkpointSound = Level.Content.Load<SoundEffect>("Sounds/Checkpoint");

            // Fonts
            infoFont = Level.Content.Load<SpriteFont>("Fonts/Hud");
        }

        private string GetPlayerTheme()
        {
            string location = "Sprites/Player/";
            switch (level.LevelIndex)
            {
                case -1: return location + "City/";
                case 4: return location + "Space/";
                case 5: return location + "Snow/";
                case 6: return location + "House/";
                case 7: return location + "Street/";
                case 8: return location + "Race/";
                case 10: return location + "Sniper/";
                case 11: return location + "Heaven/";
                case 12: return location + "Hell/";
                
                default: return location;
            }
        }

        /// Resets the player (brings him back to life).
        public void Reset(Vector2 position)
        {
            Level.Score -= 100;
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            level.movingPlatformContact = false;
            // At moment player keeps gun if already collected
            powerBulletCount = 0;
            // If player has health bar restore health to max
            if (automaticRun)
                this.playerHealth = playerMaxHealth;
            if (carRaceMode)
                automaticVehicleSpeed = NormalVehicleSpeed;
            sprite.PlayAnimation(idleAnimation);
        }

        // Handles input, performs physics, and animates the player sprite.
        public void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            MouseState mouseState,
            DisplayOrientation orientation)
        {
            infoTimer++;
            if (infoTimer > InfoTimerMax)
            {
                drawInfo = false;
                infoTimer = 0;
            }

            GetInput(keyboardState, mouseState, orientation);

            ApplyPhysics(gameTime);

            if (inVehicle && !carRaceMode)
                sprite.PlayAnimation(vehicleAnimation);

            else if (level.LevelIndex == 11 && inSpace)
                sprite.PlayAnimation(flyAnimation);

            else if (inSudo3D)
            {
                if (carRaceMode && onRamp)
                    sprite.PlayAnimation(jumpAnimation);
                else if (carRaceMode && onOilSlick)
                    sprite.PlayAnimation(rollAnimation);

                else if (Math.Abs(Velocity.X) - 0.02f > 0)
                    sprite.PlayAnimation(walkAnimation);
                else if (vertMovement > 0)
                    sprite.PlayAnimation(walkDownAnimation);
                else if (vertMovement < 0)
                    sprite.PlayAnimation(walkUpAnimation);
                else
                    sprite.PlayAnimation(idleAnimation);

            }


            else if (IsAlive && IsOnGround)
            {

                if (onSlope)
                    sprite.PlayAnimation(rollAnimation);

                else if (isSwimming)
                    sprite.PlayAnimation(swimAnimation);
                else if ((onRightConveyor || onLeftConveyor) && movement == 0)
                    sprite.PlayAnimation(idleAnimation);

                // Ignores small movements
                else if (isSprinting && Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(sprintAnimation);
                }
                else if (Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(walkAnimation);
                }

                else if (isDucking)
                {
                    sprite.PlayAnimation(duckAnimation);
                }
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }

            else if (inWind)
                sprite.PlayAnimation(flyAnimation);

            else if (onWaterSurface)
                sprite.PlayAnimation(swimIdleAnimation);

            else if (isSwimming)
                sprite.PlayAnimation(swimAnimation);

            // If falling downwards, crouch into ball
            else if (Velocity.Y > 0)
            {
                sprite.PlayAnimation(idleAnimation);
            }
            // Clear input.
            movement = 0.0f;
            isJumping = false;
        }

        // Gets player movement and jump commands from input.
        private void GetInput(
            KeyboardState keyboardState,
            MouseState mouseState,
            DisplayOrientation orientation)
        {
            if (!onSlope)
            {
                float moveSpeed;
                if (isDucking)
                    moveSpeed = 0;
                else if (keyboardState.IsKeyDown(Keys.LeftControl) && (!isSwimming || (isSwimming && inVehicle)) && !onMonkeyBar)
                {
                    moveSpeed = 2.0f;
                    isSprinting = true;
                }
                else if (onLeftConveyor || onRightConveyor)
                {
                    moveSpeed = 1.5f;
                    isSprinting = false;
                }
                else if (onMonkeyBar)
                {
                    moveSpeed = 0.5f;
                    isSprinting = false;
                }
                else
                {
                    moveSpeed = 1.0f;
                    isSprinting = false;
                }


                // If any digital horizontal movement input is found, override the analog movement.
                if (onSwingPole)
                {
                    if (keyboardState.IsKeyDown(Keys.Left) ||
                        keyboardState.IsKeyDown(Keys.A))
                    {
                        swingPoleRight = false;
                        facingRight = false;
                    }
                    else if (keyboardState.IsKeyDown(Keys.Right) ||
                             keyboardState.IsKeyDown(Keys.D))
                    {
                        swingPoleRight = true;
                        facingRight = true;
                    }
                }
                else
                {
                    if (keyboardState.IsKeyDown(Keys.Left) ||
                        keyboardState.IsKeyDown(Keys.A))
                    {
                        movement = -moveSpeed;
                        facingRight = false;
                    }
                    else if (keyboardState.IsKeyDown(Keys.Right) ||
                             keyboardState.IsKeyDown(Keys.D))
                    {
                        movement = moveSpeed;
                        facingRight = true;
                    }
                }

                if(isOnGround)
                    isDucking = keyboardState.IsKeyDown(Keys.Down);

                if (onWaterSurface)
                {
                    if (keyboardState.IsKeyDown(Keys.Down))
                    {
                        isSwimming = true;
                        onWaterSurface = false;
                        vertMovement = 3.0f;
                    }
                }

                if (onLadder || isSwimming || inWind || (inVehicle && !inAutomaticVehicle) || inSudo3D)
                {
                    isDucking = false;
                  
                    if (automaticRun && automaticRunUp)
                        vertMovement = -3.0f;
                    else if (keyboardState.IsKeyDown(Keys.Up))
                        vertMovement = -3.0f;
                    else if (keyboardState.IsKeyDown(Keys.Down))
                       vertMovement = 3.0f;
                    else vertMovement = 0.0f;

                    if (inSudo3D && keyboardState.IsKeyDown(Keys.LeftControl))
                        vertMovement *= 2;
                }


                if (onMonkeyBar)
                {
                    fallDown = keyboardState.IsKeyDown(Keys.Down);
                }

            }
            if (!isDucking)
            {
                // Check if the player wants to jump.
                isJumping =
                    ((keyboardState.IsKeyDown(Keys.Space) ||
                    keyboardState.IsKeyDown(Keys.Up) ||
                    keyboardState.IsKeyDown(Keys.W)) && level.LevelIndex != 10) ||
                    (onTrampoline ||
                    automaticJump);

                if (!onSwingPole && hasGun)
                {
                    if (shootWithMouse)
                    {
                        

                        mouseState = Mouse.GetState();
                        // Get current mouse position
                        mousePos = new Vector2(mouseState.X, mouseState.Y);
                        // Need to compensate for car velocity (fix this later)
                        if (carRaceMode)
                            mousePos += new Vector2(180, 0);
                        // Find top-left camera position
                        int left = (int)Math.Floor(level.cameraPosition.X);
                        int top = (int)Math.Floor(level.cameraPosition.Y);
                        // Get player's position relative to top-left of game window
                        relativePosition = new Vector2(this.Position.X - left, this.Position.Y - top);
                        // Get movement vector for bullet and normalize
                        bulletVector = mousePos - this.relativePosition;
                        if (bulletVector != Vector2.Zero)
                            bulletVector.Normalize();
                        // Determine if player wants to shoot
                        isShooting = mouseState.LeftButton == ButtonState.Pressed;
                    }

                    else
                        isShooting = keyboardState.IsKeyDown(Keys.S);
                }
            }

        }

        // Updates the player's velocity and position based on input, gravity, etc.
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            if (inVehicle && inAutomaticVehicle)
            {
                if (automaticVehicleRight)
                    velocity.X = automaticVehicleSpeed * MoveAcceleration * elapsed;
                else
                    velocity.X = -automaticVehicleSpeed * MoveAcceleration * elapsed;
            }
            // Only allow horizontal movement in automaticRun mode on level 11
            else if (automaticRun && level.LevelIndex != 11)
            {
                if (automaticRunRight)
                    velocity.X = automaticRunSpeed * MoveAcceleration * elapsed;
                else
                    velocity.X = -automaticRunSpeed * MoveAcceleration * elapsed;
            }

            else if (onSlope)
            {
                velocity.X = 2 * MoveAcceleration * elapsed;
                wasOnSlope = true;
            }
            else if (wasOnSlope && isJumping)
            {
                velocity.X = 2 * MoveAcceleration * elapsed;
            }
            else if (onRightConveyor)
                velocity.X = 1 * MoveAcceleration * elapsed + movement * MoveAcceleration * elapsed;
            else if (onLeftConveyor)
                velocity.X = -1 * MoveAcceleration * elapsed + movement * MoveAcceleration * elapsed;

            else
                velocity.X += movement * MoveAcceleration * elapsed;

            if (onLadder || isSwimming || onWaterSurface || onMonkeyBar || (inVehicle && !inAutomaticVehicle) || inSudo3D)// || onSwingPole)
                velocity.Y = 0;

            // If in wind make character flutter up and down alternately
            else if (inWind)
            {
                windCounter++;
                if (windCounter < 10)
                    velocity.Y = -100;
                else if (windCounter < 18)
                    velocity.Y = 100;
                else windCounter = 0;
            }
            else
            {
                if (inSpace)
                    velocity.Y = MathHelper.Clamp(velocity.Y + SpaceGravityAcceleration * elapsed, -SpaceMaxFallSpeed, SpaceMaxFallSpeed);
                else
                    velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);
            }
           
           velocity.Y = DoJump(velocity.Y, gameTime);
            
            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            if (!isAlive)
                Velocity = Vector2.Zero;

            // Apply velocity.
            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            if (onSwingPole)
            {
                if (swingPoleRight)
                    RotationAngle -= 8*elapsed;
                else
                    RotationAngle += 8*elapsed;
                float circle = MathHelper.Pi * 2;
                RotationAngle = RotationAngle % circle;
            }

            if (onMonkeyBar)
            {
                if (velocity.X != 0)
                    sprite.PlayAnimation(monkeyBarAnimation);
                else
                    sprite.PlayAnimation(monkeyBarIdleAnimation);
                if (fallDown)
                    position.Y += 33;
            }

           if (onLadder || isSwimming || inWind || (inVehicle && !inAutomaticVehicle) || inSudo3D) // || onWaterSurface)
           {
               position.Y += vertMovement;
               if (onLadder)
               {
               if (vertMovement == 0)
                   sprite.PlayAnimation(climbIdleAnimation);
               else
                   sprite.PlayAnimation(climbAnimation);
               }
         
               else if (isSwimming)
               {
                   Animation swimType = (inVehicle && currVehicle == Vehicle.Submarine) ? vehicleAnimation : swimAnimation;
                   if (level.LevelIndex == 2)
                       sprite.PlayAnimation(swimType);
                   else
                       sprite.PlayAnimation(swimAnimation);
               }
           }

           timeSinceShot++;
           if (powerBulletCount == 0)
           {
               Bullet.DropBomb = false;
               if (hasGun)
                  UpdateReloadTime();
           }

           if (isShooting && timeSinceShot > reloadTime)
           {
               // Slight bullet offset from player depending on which direction we are firing
               int bulletOffsetX = 0;
               // Having 0 bullet offset when firing with mouse makes shots more accurate
               int bulletOffsetY = (shootWithMouse) ? 0 : -Tile.Width/2;
               Vector2 pos;

               if (sniperMode)
                   pos = mousePos + new Vector2(level.cameraPosition.X, level.cameraPosition.Y);
               else
                   pos = new Vector2(position.X + bulletOffsetX, position.Y + bulletOffsetY);

               bool inWater = isSwimming || onWaterSurface;

               // If out of water cannot fire harpoon / if in water can only fire harpoon
               if ((!inWater && currGun != Gun.Harpoon) || (inWater && currGun == Gun.Harpoon))
               {
                   level.Bullets.Add(new Bullet(level, pos, gameTime, (Turret)null));
                   timeSinceShot = 0;
                   if (powerBulletCount > 0)
                   powerBulletCount--;
               }
           }

            // If the player is now colliding with the level, separate them.
            HandleCollisions();
            UpdateLevelSpecificState();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;
        }


        // Updates gun reload time every time player picks up a gun
        private void UpdateReloadTime()
        {
            // Default to false and set to true if required
            shootWithMouse = false;
            if (powerBulletCount > 0)
                reloadTime = Bullet.powerBulletReload;
            else if (inVehicle)
            {
                switch (currVehicle)
                {
                    case Vehicle.Submarine:
                        reloadTime = Bullet.uziReload;
                        break;
                    case Vehicle.Spaceship:
                        reloadTime = Bullet.spaceBlasterReload;
                        break;
                    case Vehicle.Snowmobile:
                        reloadTime = Bullet.spaceBlasterReload;
                        break;
                    case Vehicle.Car:
                        reloadTime = Bullet.multiDirectionalReload;
                        shootWithMouse = true;
                        break;
                    case Vehicle.Wings:
                        reloadTime = Bullet.multiDirectionalReload;
                        shootWithMouse = (level.LevelIndex == 11);
                        break;
                    default:
                        reloadTime = Bullet.uziReload;
                        break;
                }
            }
            else
            {
                switch (currGun)
                {
                    case Gun.Pistol:
                        reloadTime = Bullet.pistolReload; break;
                    case Gun.Uzi:
                        reloadTime = Bullet.uziReload; break;
                    case Gun.Sniper:
                        reloadTime = Bullet.sniperReload; break;
                    case Gun.Harpoon:
                        reloadTime = Bullet.harpoonReload; break;
                    case Gun.SpaceBlaster:
                        reloadTime = Bullet.spaceBlasterReload; break;
                    case Gun.Multidirectional:
                        reloadTime = (sniperMode) ? Bullet.sniperReload : Bullet.multiDirectionalReload;
                        shootWithMouse = true;
                        
                        break;
                }
            }
        }

        /* Calculates the Y velocity accounting for jumping and
         * animates accordingly.
         * During the accent of a jump, the Y velocity is completely
         * overridden by a power curve. During the decent, gravity takes
         * over. The jump velocity is controlled by the jumpTime field
         * which measures time into the accent of the current jump.
         */
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (!onLadder && isJumping)
            {
                // Begin or continue a jump
                if ((!wasJumping && (IsOnGround || onWaterSurface)) || jumpTime > 0.0f) // add monkey bar jump here if required
                {
                    if (jumpTime == 0.0f)
                    {
                        if (onTrampoline)
                            Game.PlaySound(boingSound);
                        else
                            Game.PlaySound(jumpSound);
                    }
                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    sprite.PlayAnimation(jumpAnimation);
                }

                // If we are in the ascent of the jump
                float maxTime;
                if (inSpace)
                    maxTime = SpaceMaxJumpTime;
                else
                    maxTime = (onTrampoline) ? MaxTrampolineJumpTime : MaxJumpTime;
                if (0.0f < jumpTime && jumpTime <= maxTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    if (onTrampoline)
                        velocityY = BounceLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxTrampolineJumpTime, BounceControlPower));
                    else if (inSpace)
                        velocityY = SpaceJumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxTrampolineJumpTime, BounceControlPower));
                    else
                        velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;
                    automaticJump = false;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }
            wasJumping = isJumping;
            
            return velocityY;
        }

        // Sets checkpoint for player
        private void SetCheckPoint(int x, int y)
        {
            Level.LastCheckPoint = new Vector2(x * Tile.Width, y * Tile.Height);
            level.TimeAtLastCheckpoint = level.TimeRemaining;
            Game.PlaySound(checkpointSound);
        }

        // Sets main level checkpoint for player
        private void SetMainLevelCheckPoint(int x, int y)
        {
            Level.LastMainLevelCheckPoint = RectangleExtensions.GetBottomCenter(Level.GetBounds(x, y));
            level.TimeAtLastCheckpoint = level.TimeRemaining;
            Game.PlaySound(checkpointSound);
        }


        private void UpdateLevelSpecificState()
        {
            switch (level.LevelIndex)
            {
                // Level 2 - Water level
                case 2:
                    {
                        // To create effect of constant swimming underwater
                        isSwimming = true;
                        isOnGround = false;
                        onWaterSurface = false;

                        // To prevent level deadlock
                        if (inVehicle)
                        {
                            for (int i = 0; i < 2; i++)
                                level.RemoveLaser(105, 20 + i);
                        }

                        if (level2AmuletCount >= 1)
                        {
                            // Remove lasers
                            for (int i = 0; i < 9; i++)
                            {
                                if (i == 2) i = 7;
                                level.RemoveLaser(63 + i, 24);
                            }
                            for (int i = 0; i < 5; i++)
                                level.RemoveLaser(27 + i, 21);
                        }
                        if (level2AmuletCount >= 3)
                        {
                            // Remove laser 1
                            for (int i = 0; i < 2; i++)
                                level.RemoveLaser(127, 17 + i);
                        }
                        if (level2AmuletCount >= 4)
                        {
                            // Remove laser 2
                            for (int i = 0; i < 8; i++)
                                level.RemoveLaser(130, 14 + i);
                        }
                        if (level2AmuletCount >= 5)
                        {
                            // Remove laser 3
                            for (int i = 0; i < 14; i++)
                                level.RemoveLaser(132, 11 + i);
                        }
                    }
                    break;

                // Level 4 - Space level
                case 4:
                    // if inSpace then not in airlock and vica versa
                    if (inVehicle)
                    {
                        inSpace = false;
                        for (int i = 0; i < 8; i++)
                            level.RemoveLaser(131 + i, 44);
                    }
                    else
                        inSpace = !inAirLock;

                    if (level.Tiles[204, 22].Collision == TileCollision.Passable)
                    {
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(229, 14 + i);
                    }

                    if (level.Tiles[230, 4].Collision == TileCollision.Passable)
                    {
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(231 + i, 6);
                    }


                    if (level4StarCount >= 3)
                    {
                        // Remove laser 1
                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(166 + i, 34);
                        // Remove laser 1 guarding last star
                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(178 + i, 23);

                    }
                    if (level4StarCount >= 4)
                    {
                        // Remove laser 2
                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(166 + i, 35);
                        // Remove laser 2 guarding last star
                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(178 + i, 24);
                    }
                    if (level4StarCount >= 5)
                    {
                        // Remove laser 3
                        for (int i = 0; i < 8; i++)
                            level.RemoveLaser(165 + i, 36);
                    }
                    break;

                // Level 5 - Snow level
                case 5:

                    // Manages horizontal moving platform section
                    if (level.MovingPlatforms.Count != 0)
                    {
                        // Assume all platforms are past point, if any are not set 'allastPoint' to false
                        bool allPastPoint = true;
                        for (int i = 0; i < level.MovingPlatforms.Count; i++)
                        {
                            MovingPlatform platform = level.MovingPlatforms[i];
                            if (platform.Position.X > 54 * Tile.Width)
                                allPastPoint = false;
                        }

                        // If all platfroms are a certain distance away, make trigger for making platforms reappear
                        if (allPastPoint)
                            level.Tiles[73, 105] = level.LoadLevelTile("visibleTriggerTile", TileCollision.Trigger);
                    }

                    break;

                // Level 6 - Haunted house level
                case 6:

                    // Remove laser on collecting pumpkin 1
                    if (level.Tiles[63, 20].Collision == TileCollision.Passable)
                    {
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(66, 18 + i);
                    }

                    if (level.Tiles[7, 4].Collision == TileCollision.Passable)
                    {
                        // Remove laser (to prevent deadlock)
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(35, 6 + i);
                    }

                    if (level.Tiles[34, 2].Collision == TileCollision.Passable)
                    {
                        // Remove laser 1
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(77, 1 + i);
                    }

                    if (level.Tiles[29, 3].Collision == TileCollision.Passable)
                    {
                        // Remove laser 2
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(72, 1 + i);
                    }



                    if (level.Tiles[157, 3].Collision == TileCollision.Passable)
                    {
                        level.Tiles[60, 10] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);

                        // Remove laser
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(152 + i, 5);

                        // Remove laser 2
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(119 + i, 5);

                        // Remove laser 3
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(77, 1 + i);

                        // Remove laser 4
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(73 + i, 3);

                        // Remove laser 5
                            level.RemoveLaser(108, 3);

                        // Remove laser 6
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(116, 3 + i);

                        // Remove targets
                        level.Tiles[112, 3] = new Tile(null, TileCollision.Passable);
                        level.Tiles[124, 4] = new Tile(null, TileCollision.Passable);

                        periodicLoop = true;

                        periodCounter++;
                        if (periodicLoop && periodCounter % 100 == 0)
                        {
                            // Add moving spikes 1
                            for (int i = 0; i < 4; i++)
                                Level.LoadMovingSpikeTile(154, 1 + i, true, false, false);

                            // Add moving spikes 2
                            for (int i = 0; i < 2; i++)
                                Level.LoadMovingSpikeTile(89, 6 + i, true, false, true);
                        }

                        // Remove moving spikes that are out of range
                        for (int i = 0; i < level.MovingSpikes.Count; i++)
                        {
                            MovingSpike spike = level.MovingSpikes[i];
                            if (spike.Position.Y < 6*Tile.Height && spike.Position.X < 123*Tile.Width
                                || spike.Position.Y > 6 * Tile.Height && spike.Position.X > 117 * Tile.Width)
                                spike.Remove();
                        }
                    }


                    if (level6PumpkinCount >= 4)
                    {
                        // Remove laser 1
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(90, 11 + i);
                    }
                    if (level6PumpkinCount >= 5)
                    {
                        // Remove laser 2
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(92, 10 + i);

                        // Remove laser to final amulet
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(85, 6 + i);
                    }
                    if (level6PumpkinCount >= 6)
                    {
                        // Remove laser 3
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(94, 11 + i);

                        // Turn off cycling lasers
                        periodicLoop = false;
                    }
                    break;

                // Level 7 - Street/Rooftop level
                case 7:
                    
                    // Target shooting section
                    if (level.Tiles[218, 19].Collision == TileCollision.Passable && level.Tiles[239, 18].Collision == TileCollision.Passable)
                    {
                        // Remove laser 1
                        for (int i = 0; i < 24; i++)
                            level.RemoveLaser(218 + i, 22);
                    }

                    if (level.Tiles[216, 11].Collision == TileCollision.Passable)
                    {
                        // Remove laser 2
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(221, 13 + i);
                    }

                    if (level.Tiles[241, 9].Collision == TileCollision.Passable)
                    {
                        // Remove laser 3
                        for (int i = 0; i < 8; i++)
                            level.RemoveLaser(213 + i, 12);
                    }

                    if (level.Tiles[229, 4].Collision == TileCollision.Passable)
                    {
                        // Remove laser 4
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(243, i);
                    }

                    // Car section
                    if (level.Tiles[286, 13].Collision == TileCollision.Passable)
                    {
                        // Remove laser 1
                        for (int i = 0; i < 7; i++)
                            level.RemoveLaser(288 + i, 7);
                    }

                    if (level.Tiles[396, 24].Collision == TileCollision.Passable)
                    {
                        // Remove laser 2
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(429, 28 + i);
                    }

                    if (level.Tiles[419, 17].Collision == TileCollision.Passable)
                    {
                        // Remove laser 3
                        for (int i = 0; i < 5; i++)
                            level.RemoveLaser(430 + i, 27);
                    }

                    if (level.Tiles[489, 16].Collision == TileCollision.Passable)
                    {
                        // Remove laser 4
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(503, 21 + i);
                    }

                    break;

                // Sniper level
                case 9:

                    // NB Could also make player have to get over 50% accuracy and add strict limit
                    // If player has hit all targets and killed all enemies load next level
                    if (!level.ReachedExit && Bullet.targetsHit >= Bullet.targetMax && Bullet.enemiesKilled >= Bullet.enemiesMax)
                        level.OnExitReached();

                    break;

                // Sniper protection level
                case 10:

                    if (level.Tiles[50, 16].Collision == TileCollision.Passable)
                    {
                        // Remove laser 1
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(50, 18 + i);
                    }

                    if (level.Tiles[83, 11].Collision == TileCollision.Passable)
                    {
                        // Remove laser 2
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(84, 16 + i);
                    }

                    if (level.Tiles[93, 17].Collision == TileCollision.Passable)
                    {
                        // Add platform
                        for (int i = 0; i < 5; i++)
                            level.Tiles[85 + i, 21] = Level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                    }

                    if (level.Tiles[109, 13].Collision == TileCollision.Passable)
                    {
                        // Add platform
                        for (int i = 0; i < 7; i++)
                            level.Tiles[106 + i, 17] = Level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                    }

                    if (level.Tiles[143, 15].Collision == TileCollision.Passable)
                    {
                        // Add platform
                        for (int i = 0; i < 12; i++)
                            level.Tiles[138 + i, 20] = Level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                    }

                    if (level.Tiles[167, 6].Collision == TileCollision.Passable)
                    {
                        // Remove laser 3
                        for (int i = 0; i < 2; i++)
                            level.RemoveLaser(167, 8 + i);
                    }

                    if (level.Tiles[286, 10].Collision == TileCollision.Passable)
                    {
                        // Remove laser 4
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(282 + i, 13);
                    }

                    if (level.Tiles[260, 15].Collision == TileCollision.Passable)
                    {
                        // Add platform
                        for (int i = 0; i < 4; i++)
                            level.Tiles[258 + i, 22] = Level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                    }

                    if (level.Tiles[272, 18].Collision == TileCollision.Passable)
                    {
                        // Add platform
                        for (int i = 0; i < 8; i++)
                            level.Tiles[268 + i, 22] = Level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                    }

                    if (level.Tiles[289, 13].Collision == TileCollision.Passable)
                    {
                       // Remove laser 4
                        for (int i = 0; i < 5; i++)
                            level.RemoveLaser(287 + i, 15);
                    }


                    if (level.Tiles[429, 10].Collision == TileCollision.Passable)
                    {
                        // Remove laser 5
                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(431 + i, 9);
                    }

                    break;

                // Heaven level
                case 11:

                    // Removes laser when enemy is killed
                    for (int i = 0; i < level.Enemies.Count; i++)
                    {
                        Enemy enemy = level.Enemies[i];
                        if (enemy.createdByEvent && enemy.died)
                        {
                            for (int j = 0; j < 5; j++)
                                level.RemoveLaser(1 + j, 75);
                        }
                    }


                    // Manages horizontal moving platform section
                    if (level.MovingPlatforms.Count != 0)
                    {
                        // Assume all platforms are past point, if any are not set 'allPastPoint' to false
                        bool allPastPoint = true;
                        for (int i = 0; i < level.MovingPlatforms.Count; i++)
                        {
                            MovingPlatform platform = level.MovingPlatforms[i];
                            if (platform.Position.X < 211 * Tile.Width)
                                allPastPoint = false;
                        }

                        // If all platfroms are a certain distance away, make trigger for making platforms reappear
                        if (allPastPoint)
                            level.Tiles[190, 52] = level.LoadLevelTile("visibleTriggerTile", TileCollision.Trigger);
                    }

                    if (level11StarCount == 2)
                    {
                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(117 + i, 37);
                    }

                    // Star 2
                    if (level.Tiles[299, 35].Collision == TileCollision.Passable)
                    {
                        for (int i = 0; i < 3; i++)
                            level.RemoveLaser(248, 34 + i);

                        for (int i = 0; i < 6; i++)
                            level.RemoveLaser(170, 50 + i);

                        for (int i = 0; i < 6; i++)
                            level.Tiles[173, 50 + i] = new Tile(null, TileCollision.Passable);

                        for (int i = 0; i < 9; i++)
                            level.Tiles[174 + i, 56] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);

                            level.Tiles[165, 55] = level.LoadLevelTile("vehicleTile", TileCollision.Vehicle);
                    }

                    // Star 3
                    if (level.Tiles[22, 27].Collision == TileCollision.Passable)
                    {
                        for (int i = 0; i < 4; i++)
                            level.RemoveLaser(76, 1 + i);
                    }

                        // Manage moving spikes section
                        periodCounter++;
                        if (periodicLoop && periodCounter % 200 == 0)
                        {
                            //Add moving spikes 1
                            for (int i = 0; i < 40; i++)
                                Level.LoadMovingSpikeTile(1 + i, 1, true, true, true);

                        }

                        // Remove moving spikes that are out of range
                        for (int i = 0; i < level.MovingSpikes.Count; i++)
                        {
                            MovingSpike spike = level.MovingSpikes[i];
                            if (spike.Position.Y > 24 * Tile.Height)
                                spike.Remove();
                        }


                    // Last section
                        // Collect all keys 1
                        if (level.Tiles[251, 10].Collision == TileCollision.Passable
                            && level.Tiles[267, 10].Collision == TileCollision.Passable
                            && level.Tiles[251, 28].Collision == TileCollision.Passable
                            && level.Tiles[267, 28].Collision == TileCollision.Passable)
                        {
                            for (int i = 0; i < 6; i++)
                                level.Tiles[269, 17 + i] = new Tile(null, TileCollision.Passable);
                        }

                        if (level.Tiles[256, 6].Collision == TileCollision.Passable)
                        {
                            for (int i = 0; i < 7; i++)
                                level.RemoveLaser(287, 1 + i);
                        }

                        if (level.Tiles[256, 6].Collision == TileCollision.Passable)
                        {
                            for (int i = 0; i < 7; i++)
                                level.RemoveLaser(287, 1 + i);
                        }

                        if (level.Tiles[295, 3].Collision == TileCollision.Passable)
                        {
                            for (int i = 0; i < 7; i++)
                                level.RemoveLaser(264, 1 + i);
                        }

                        // Collect all keys 2
                        if (level.Tiles[252, 2].Collision == TileCollision.Passable
                            && level.Tiles[301, 6].Collision == TileCollision.Passable)
                        {
                            for (int i = 0; i < 6; i++)
                                level.Tiles[292, 17 + i] = new Tile(null, TileCollision.Passable);
                            for (int i = 0; i < level.Enemies.Count; i++)
                            {
                                level.Enemies[i].Remove();
                            }
                        }

                    break;
            }
        }


        /* Detects and resolves all collisions between the player and his neighboring
         * tiles. When a collision is detected, the player is pushed away along one
         * axis to prevent overlapping. There is some special logic for the Y axis to
         * handle platforms which behave differently depending on direction of movement.
         */
        private void HandleCollisions()
        {
            // Use small bounding rectangle for ladder detection
            // Get the player's small bounding rectangle and find neighboring tiles.
            Rectangle bounds = SmallBoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

             // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // Get collision type for this tile
                    TileCollision collision = Level.GetCollision(x, y);

                    // If player is on a ladder
                    if (collision == TileCollision.Ladder)
                    {
                            if (Level.GetCollision(x, y - 1) != TileCollision.Ladder)
                                onLadder = false;
                            else
                                onLadder = true;
                    }
                    else
                        onLadder = false;
                }
            }



            // Get the player's bounding rectangle and find neighboring tiles.
            bounds = BoundingRectangle;
            leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            isOnGround = false;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);

                    // If player falls into spikes/pit
                    if (collision == TileCollision.Death)
                    {
                        if (carRaceMode && onRamp)
                            break;
                        this.OnKilled(null);
                    }

                    // Special collisions for car race mode
                    if (carRaceMode)
                    {
                        // Car goes over a ramp
                        if (collision == TileCollision.Ramp)
                        {
                            onRamp = true;
                            rampTimer = 0;
                        }

                        else if (collision == TileCollision.Rconveyor)
                        {
                            onBooster = true;
                            boosterTimer = 0;
                            automaticVehicleSpeed = 5;
                        }

                        // Car goes over oil slick
                        else if (collision == TileCollision.Oil)
                        {
                            onOilSlick = true;
                            oilSlickTimer = 0;
                            automaticVehicleSpeed = 1;
                        }
                        else
                        {
                            rampTimer++;
                            if (rampTimer >= RampDelay)
                                onRamp = false;

                            boosterTimer++;
                            if (boosterTimer >= BoosterDelay)
                                onBooster = false;

                            oilSlickTimer++;
                            if (oilSlickTimer >= OilSlickDelay)
                                onOilSlick = false;

                            if (!onBooster && !onOilSlick)
                                automaticVehicleSpeed = NormalVehicleSpeed;

                        }
                    }

                    // If player is picking up gun
                    if (collision == TileCollision.Gun)
                    {
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                        if (Level.LevelChars[x, y] == 'Z')
                        {
                            powerBulletCount++;

                            switch (level.LevelIndex)
                            {
                                case 6:
                                    // Power bullets become bombs in level 6 (for Witch boss)
                                    Bullet.DropBomb = true;
                                    if (x == 128 && y == 11)
                                    {
                                        Level.LevelChars[153, 9] = 'Z';
                                        level.Tiles[153, 9] = level.LoadTile("powerBulletTile", TileCollision.Gun);
                                    }
                                    else if (x == 153 && y == 9)
                                    {
                                        Level.LevelChars[128, 11] = 'Z';
                                        level.Tiles[128, 11] = level.LoadTile("powerBulletTile", TileCollision.Gun);
                                    }
                                    break;

                                case 12:
                                    Bullet.DropBomb = true;
                                    if (x == 129 && y == 17)
                                    {
                                        Level.LevelChars[98, 17] = 'Z';
                                        level.Tiles[98, 17] = level.LoadTile("powerBulletTile", TileCollision.Gun);
                                    }
                                    else if (x == 98 && y == 17)
                                    {
                                        Level.LevelChars[129, 17] = 'Z';
                                        level.Tiles[129, 17] = level.LoadTile("powerBulletTile", TileCollision.Gun);
                                    }
                                    break;
                            }
                        }
                        else
                        {
                            hasGun = true;
                            if (Level.LevelChars[x, y] == 'p')
                                currGun = Gun.Pistol;
                            else if (Level.LevelChars[x, y] == 'u')
                                currGun = Gun.Uzi;
                            else if (Level.LevelChars[x, y] == 's')
                                currGun = Gun.Sniper;
                            else if (Level.LevelChars[x, y] == 'h')
                                currGun = Gun.Harpoon;
                            else if (Level.LevelChars[x, y] == 'B')
                                currGun = Gun.SpaceBlaster;
                            else if (Level.LevelChars[x, y] == 'X')
                                currGun = Gun.Multidirectional;
                        }
                        UpdateReloadTime();
                        Game.PlaySound(gunReloadSound);
                    }

                    // If player is hits info tile
                    if (collision == TileCollision.Info)
                    {
                        for (int i = 0; i < Level.Infos.Count; i++)
                        {
                            if (Level.Infos[i].BoundingRectangle.Contains(new Point((int)position.X, (int)position.Y)))
                            {
                                drawInfo = true;
                                infoString = Info.getInfo(Level.LevelIndex, i);
                            }
                        }
                    }

                    // If player is picking up wings
                    if (collision == TileCollision.Wings)
                    {
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                    }

                    // If player reaches a checkpoint
                    if (collision == TileCollision.Save)
                    {
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                        SetCheckPoint(x, y);
                    }

                    // If player hits a timer (also acts as a checkpoint
                    if (collision == TileCollision.Timer)
                    {
                        // Remove timer so can only be hit once
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);

                        switch (level.LevelIndex)
                        {
                            case 2:
                                // Remove laser beam
                                for (int i = 0; i < 3; i++)
                                    level.RemoveLaser(32, 13 + i);

                                // Add enemies and lasers in player's path
                                level.LoadEnemyTile(26, 6, "MonsterB", true);
                                level.LoadEnemyTile(31, 16, "MonsterD", true);
                                level.LoadEnemyTile(51, 3, "MonsterB", true);
                                level.LoadEnemyTile(44, 3, "MonsterD", true);
                                level.LoadLaserTile(55, 5, true, true, true, RotationDirection.None);
                                level.LoadLaserTile(37, 2, true, true, true, RotationDirection.None);
                                level.LoadLaserTile(37, 3, true, true, true, RotationDirection.None);
                                break;

                            case 5:
                                // Remove laser beams
                                for (int i = 0; i < 3; i++)
                                {
                                    level.RemoveLaser(140, 49 + i);
                                    level.RemoveLaser(139, 59 + i);
                                    // Ensure entry laser is removed (in case player gets stuck outside wall)
                                    level.RemoveLaser(146, 15 + i);
                                }

                                // Block off ladder
                                level.Tiles[189, 21] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);
                                level.Tiles[190, 21] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);
                                level.Tiles[190, 22] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);

                                    

                                // Add enemies and lasers in player's path
                                level.LoadEnemyTile(189, 44, "MonsterK", true);
                                level.LoadEnemyTile(198, 46, "MonsterK", true);
                                level.LoadEnemyTile(190, 50, "MonsterK", true);
                                level.LoadEnemyTile(215, 28, "MonsterK", true);
                                level.LoadEnemyTile(218, 51, "MonsterK", true);
                                for (int i = 0; i < 2; i++)
                                    level.LoadLaserTile(207, 24 + i, true, true, true, RotationDirection.None);
                                for (int i = 0; i < 2; i++)
                                    level.LoadLaserTile(225, 30 + i, true, true, true, RotationDirection.None);
                                for (int i = 0; i < 12; i++)
                                    level.LoadLaserTile(223 + i, 39, true, false, true, RotationDirection.None);

                                // Add moving spikes
                                for (int i = 0; i < 47; i++)
                                    Level.LoadMovingSpikeTile(189 + i, 9, true, true, true);

                                SetCheckPoint(x-2, y);
                                break;

                            default:
                               Level.LastCheckPoint = new Vector2(x * Tile.Width, y * Tile.Height);
                               level.TimeAtLastCheckpoint = level.TimeRemaining;
                                break;
                        }
                        level.startTimer();
                        
                    }

                    // If player collects a +1minute item
                    if (collision == TileCollision.PlusOne)
                    {
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                        level.addOneMinToTime();
                    }


                    // If player collects collectable item
                    if (collision == TileCollision.Collectable)
                    {
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                        // Level specific increment
                        switch (level.LevelIndex)
                        {
                        // Water level
                            case 2:
                                level2AmuletCount++;
                                SetCheckPoint(x, y);
                                break;

                            case 4:
                                level4StarCount++;
                                SetCheckPoint(x, y);

                                // Remove laser on collecting star
                                if (x == 230 && y == 4)
                                {
                                    for (int i = 0; i < 4; i++)
                                        level.RemoveLaser(231 + i, 6);
                                }
                                break;

                            case 6:
                                level6PumpkinCount++;
                                if (x == 34 && y == 2)
                                    SetCheckPoint(x, y + 1);
                                else
                                    SetCheckPoint(x, y);

                                break;

                            case 11:
                                level11StarCount++;
                                SetCheckPoint(x, y);

                                // Each items teleports player
                                // Star 1
                                if (x == 35 && y == 78)
                                {
                                    Position = new Vector2(76 * Tile.Width, 55 * Tile.Height);
                                    inVehicle = true;
                                    SetCheckPoint(76, 55);

                                    for (int i = 0; i < 6; i++)
                                        level.Tiles[67, 50 + i] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                }


                                // Star 3
                                if (x == 22 && y == 27)
                                {
                                    Position = new Vector2(71 * Tile.Width, 3 * Tile.Height);
                                    inVehicle = true;
                                    // Turn off moving spikes
                                    periodicLoop = false;
                                    SetCheckPoint(71, 3);
                                }

                                // Star 4
                                if (x == 119 && y == 5)
                                {
                                    for (int i = 0; i < 7; i++)
                                        level.Tiles[116 + i, 6] = new Tile(null, TileCollision.Passable);
                                    for (int i = 0; i < 6; i++)
                                        level.Tiles[117 + i, 30] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                    for (int i = 0; i < 6; i++)
                                        level.Tiles[115, 24 + i] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                    // Remove laser 1
                                    for (int i = 0; i < 7; i++)
                                        level.RemoveLaser(116 + i, 17);
                                    // Remove laser 2
                                    for (int i = 0; i < 7; i++)
                                        level.RemoveLaser(116 + i, 23);
                                    // Remove laser 3s (into boss)
                                    for (int i = 0; i < 5; i++)
                                        level.RemoveLaser(249, 17 + i);
                                    SetCheckPoint(130, 29);
                                }
                                break;
                        }
                    }

                     // If player finds a vehicle
                    if (collision == TileCollision.Vehicle)
                    {
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);
                        // Set inAutomaticVehicle to true if required
                        inAutomaticVehicle = false;
                        switch (level.LevelIndex)
                        {
                        // Water level - Submarine vehicle
                            case 2:
                                inVehicle = true;
                                currVehicle = Vehicle.Submarine;
                                hasGun = true;
                                currGun = Gun.Harpoon;
                                UpdateReloadTime();

                                SetCheckPoint(x, y);
                                break;

                         // Space level - Spaceship vehicle
                            case 4:
                                inVehicle = true;
                                currVehicle = Vehicle.Spaceship;
                                hasGun = true;
                                currGun = Gun.SpaceBlaster;
                                UpdateReloadTime();

                                // Reset target count
                                Bullet.Level4TargetCount = 0;
                                SetCheckPoint(x, y);
                                break;

                            // Snow level - Snowmobile vehicle
                            case 5:
                                inVehicle = true;
                                currVehicle = Vehicle.Snowmobile;
                                inAutomaticVehicle = true;
                                automaticVehicleRight = true;
                                automaticVehicleSpeed = NormalVehicleSpeed;
                                hasGun = true;
                                currGun = Gun.SpaceBlaster;
                                UpdateReloadTime();
                                SetCheckPoint(x, y);
                                // Display message
                                Level.Player.InfoString = "Use Up/Space to jump and Left/Right to aim your gun before you shoot!";
                                Level.Player.DrawInfo = true;
                                break;

                            // Haunted house level - Broomstick vehicle
                            case 6:
                                inVehicle = true;
                                currVehicle = Vehicle.Broomstick;
                                hasGun = true;
                                currGun = Gun.Uzi;
                                UpdateReloadTime();
                                SetCheckPoint(x, y);

                                // Display message
                                Level.Player.InfoString = "Time to fly!";
                                Level.Player.DrawInfo = true;
                                break;

                            // Street level - automatic car + shoot with mouse
                            case 7:
                                inVehicle = true;
                                currVehicle = Vehicle.Car;
                                inAutomaticVehicle = true;
                                automaticVehicleRight = true;
                                automaticVehicleSpeed = NormalVehicleSpeed;
                                //shootWithMouse = true;
                                hasGun = true;
                                currGun = Gun.Multidirectional;
                                UpdateReloadTime();
                                SetCheckPoint(x, y);
                                // Display message
                                Level.Player.InfoString = "Use your mouse to aim and shoot, and UP to jump!";
                                Level.Player.DrawInfo = true;
                                break;

                            // Heaven level - wings
                            case 11:
                                inVehicle = true;
                                currVehicle = Vehicle.Wings;
                                SetCheckPoint(x, y+1);
                                if (x == 252 && y == 21)
                                {
                                    hasGun = true;
                                    currGun = Gun.Multidirectional;
                                    // Add enemies
                                    level.LoadEnemyTile(272, 19, "Boss4", false);
                                    level.LoadEnemyTile(295, 13, "Boss4", false);
                                    level.LoadEnemyTile(295, 25, "Boss4", false);
                                }
                                break;

                            // Hell level - wings
                            case 12:
                                inVehicle = true;
                                currVehicle = Vehicle.Wings;
                                hasGun = true;
                                currGun = Gun.Multidirectional;
                                // Add boss1
                                level.LoadEnemyTile(128, 5, "Boss5", false);
                                // Add laser
                                for (int i = 0; i < 4; i++)
                                    level.LoadLaserTile(96, 6 + i, false, true, false, RotationDirection.None);
                                SetCheckPoint(x-5, y);
                                break;

                            default:

                                break;
                        }
                    }



                    // If player hits trigger point
                    if (collision == TileCollision.Trigger)
                    {
                        // Remove trigger from level
                        level.Tiles[x, y] = new Tile(null, TileCollision.Passable);

                        switch (level.LevelIndex)
                        {
                            case 1:
                                // Special case in level 1
                                // Visible trigger - Wheel
                                if (x == 71 && y == 24)
                                {
                                    level.Tiles[x, y] = level.LoadTile("waterTile", TileCollision.Water);
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(76 + i, 14);
                                }
                                else
                                {
                                    for (int i = 0; i < 3; i++)
                                      level.LoadLaserTile(x - 2, y - i, true, true, false, RotationDirection.None);
                                }
                                break;

                            case 2:
                                // Special case in level 2 (Water level)
                                // Remove other trigger tiles
                                for (int i = 0; i < 3; i++)
                                    level.Tiles[176, 10 + i] = new Tile(null, TileCollision.Passable);
                                // Add laser
                                for (int i = 0; i < 3; i++)
                                    level.LoadLaserTile(174, 10 + i, true, true, false, RotationDirection.None);
                                // Add boss
                                level.LoadEnemyTile(216, 16, "Boss1", false);
                                break;

                            case 3:
                                // Special cases in level 3 (Office level)
                                if (x == 12 && y == 22)
                                {
                                    // Add moving spikes
                                    for (int i = 0; i < 28; i++)
                                        Level.LoadMovingSpikeTile(1, 6 + i, true, false, true);
                                }
                                else if (x == 159 && y == 33)
                                {
                                    // Add moving spikes
                                    for (int i = 0; i < 15; i++)
                                        Level.LoadMovingSpikeTile(153 + i, 0, true, true, true);
                                }

                                else if (x == 175 && y == 5)
                                {
                                    // Add moving spikes
                                    for (int i = 0; i < 21; i++)
                                        Level.LoadMovingSpikeTile(168 + i, 2, true, true, true);
                                }

                                else if (x == 191 && y == 33)
                                {
                                    // Add laser
                                    level.LoadLaserTile(189, 33, true, true, false, RotationDirection.None);

                                    // Add moving spikes 1 (lower)
                                    for (int i = 0; i < 46; i++)
                                    {
                                        if (i == 17) i = 22;
                                        Level.LoadMovingSpikeTile(190 + i, 15, true, true, true);
                                    }
                                    // Add moving spikes 2 (upper)
                                    for (int i = 0; i < 46; i++)
                                    {
                                        if (i == 28) i = 32;
                                        Level.LoadMovingSpikeTile(190 + i, 10, true, true, true);
                                    }
                                }

                                else if (x == 189 && y == 21)
                                {
                                    // Add moving spikes
                                    for (int i = 0; i < 10; i++)
                                        Level.LoadMovingSpikeTile(181 + i, 17, true, true, true);
                                    // Add laser
                                    level.LoadLaserTile(191, 21, true, true, false, RotationDirection.None);
                                }

                                else if (x == 183 && y == 27)
                                {
                                    // Remove lasers when collect key
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(197, 15 + i);
                                    level.RemoveLaser(191, 21);
                                }

                                // On entering double moving spike pit (and half way through pit)
                                else if (((x == 194 || x == 195 || x == 196) && y == 14) || (x == 218 && (y == 11 || y == 12 || y == 13)))
                                {
                                    // Add moving spikes (left side)
                                    for (int i = 0; i < 5; i++)
                                        Level.LoadMovingSpikeTile(192, 10 + i, true, false, true);

                                    // Add moving spikes(right side - switch orientation)
                                    for (int i = 0; i < 5; i++)
                                        Level.LoadMovingSpikeTile(234, 10 + i, true, false, false);

                                    //Increase spike speed
                                    level.SetMovingSpikeSpeed(MovingSpike.FastMoveSpeed);
                                }

                                else if (x == 233 && y == 8)
                                {
                                    // Add moving spikes (left side)
                                    for (int i = 0; i < 4; i++)
                                        Level.LoadMovingSpikeTile(218, 6 + i, true, false, true);

                                    // Increase spike speed
                                    level.SetMovingSpikeSpeed(MovingSpike.FastMoveSpeed);
                                }

                                else
                                {
                                    SetCheckPoint(x, y);
                                    // Visible trigger - Key
                                    if (Level.LevelChars[x, y] == 'O')
                                    {
                                        // Remove laser 1
                                        for (int i = 0; i < 6; i++)
                                            level.RemoveLaser(21 + i, 6);
                                        // Remove laser 2
                                        for (int i = 0; i < 4; i++)
                                            level.RemoveLaser(151, 30 + i);
                                    }

                                    else
                                    {
                                        // When player gets to top of elevator shaft display message
                                        drawInfo = true;
                                        infoString = "Phew that was a close call!";
                                    }
                                    // Repeated for both triggers (to ensure no gameplay deadlock)
                                    // Insert blocks in elevator shaft
                                    level.Tiles[145, 5] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);
                                    level.Tiles[145, 10] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);
                                    level.Tiles[145, 12] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                }
                                break;

                            case 4:
                                // Triger inside air lock
                                if (x == 174 && (y == 38 || y == 39))
                                {
                                    // Don't show start count anymore and set to 0 so can replace
                                    // old lasers without any interference (from UpdateLevelSpecificState())
                                    showStats = false;
                                    level4StarCount = 0;

                                    // Remove exit laser
                                    for (int i = 0; i < 2; i++)
                                        level.RemoveLaser(177, 38 + i);

                                    // Replace old lasers
                                    // Laser 1 & Laser 2
                                    for (int i = 0; i < 6; i++)
                                    {
                                        for (int j = 0; j < 2; j++)
                                            level.LoadLaserTile(166 + i, 34 + j, false, false, false, RotationDirection.None);
                                    }
                                    // Laser 3
                                    for (int i = 0; i < 8; i++)
                                        level.LoadLaserTile(165 + i, 36, true, false, false, RotationDirection.None);

                                    inAirLock = true;
                                    SetCheckPoint(x, y);
                                }

                                // Trigger at entry to space ship dock
                                else if (x == 155 && (y == 38 || y == 39))
                                {
                                    // Add laser
                                    for (int i = 0; i < 2; i++)
                                        level.LoadLaserTile(158, 38 + i, true, true, false, RotationDirection.None);
                                    inAirLock = false;
                                }


                                // Trigger on entering maze
                                else if (x == 56 && (y == 20 || y == 21))
                                {
                                    // Remove both triggers
                                    for (int i = 0; i < 2; i++)
                                        level.Tiles[56, 20 + i] = new Tile(null, TileCollision.Passable);
                                    // Add 12 turrets
                                    for (int i = 0; i < 6; i++)
                                    {
                                        level.LoadTurretTile(22, 10 + i, true, false);
                                        level.LoadTurretTile(27, 10 + i, true, false);
                                    }
                                    SetCheckPoint(56, 21);
                                }

                                // Triggers in maze before boss
                                else if ((x == 2 || x == 56) && (y == 1 || y == 27))
                                {
                                    if (x == 56 && y == 27)
                                    {
                                        // Remove exit laser and wall
                                        for (int i = 0; i < 6; i++)
                                            level.RemoveLaser(47 + i, 28);
                                        for (int i = 0; i < 6; i++)
                                            level.Tiles[47 + i, 29] = new Tile(null, TileCollision.Passable);
                                    }
                                    else
                                    {
                                        // Remove 4 turrets from central section
                                        int removedCounter = 0;
                                        for (int i = 0; i < level.Turrets.Count; i++)
                                        {
                                            Turret turret = level.Turrets[i];
                                            if (turret.createdByEvent)
                                            {
                                                turret.OnKilled();
                                                removedCounter++;
                                            }
                                            if (removedCounter >= 8) // ??
                                                break;
                                        }
                                    }
                                }

                                // Trigger before boss (removes all enemies/turrets from level to ensure game doesn't slow down)
                                else if ((x >= 47 && x <= 52) && y == 30)
                                {
                                    // Remove all triggers
                                    for (int i = 47; i < 53; i++)
                                        level.Tiles[i, 30] = new Tile(null, TileCollision.Passable);

                                    for (int i = 0; i < level.Turrets.Count; i++)
                                    {
                                        Turret turret = level.Turrets[i];
                                        turret.Remove();
                                    }
                                    for (int i = 0; i < level.Enemies.Count; i++)
                                    {
                                        Enemy enemy = level.Enemies[i];
                                        enemy.Remove();
                                    }
                                    // Add boss
                                    level.LoadEnemyTile(29, 32, "Boss2", false);

                                    // Add laser
                                    for (int i = 0; i < 6; i++)
                                        level.LoadLaserTile(47 + i, 28, false, false, false, RotationDirection.None);

                                    SetCheckPoint(49, 31);
                                }
                                break;

                            case 5:
                                // Remove laser from gem area below spikes
                                if ((x >= 172 && x <= 174) && y == 91)
                                {
                                    for (int i = 0; i < 3; i++)
                                        level.RemoveLaser(151 + i, 92);
                                }

                                // Add falling icicles
                                if ((x == 158 && (y >= 102 && y <= 105)) || (x == 166 && (y >= 100 && y <= 103)))
                                {
                                    for (int i = 0; i < 2; i++)
                                        Level.LoadMovingSpikeTile(x + i, 99, true, true, true);

                                    // Increase spike speed
                                    level.SetMovingSpikeSpeed(MovingSpike.FastMoveSpeed);

                                    level.Tiles[x, y] = level.LoadTile("transparentTile", TileCollision.Trigger); // was triggerTile
                                }

                                // Chain of falling icicles
                                if (y == 111)
                                {

                                    for (int i = 0; i < 2; i++)
                                        Level.LoadMovingSpikeTile(x + i, 107, true, true, true);
                                    // Increase spike speed
                                    level.SetMovingSpikeSpeed(MovingSpike.FastMoveSpeed);
                                    level.Tiles[x, y] = level.LoadTile("transparentTile", TileCollision.Trigger); // was triggerTile
                                }

                                // Make moving platform appear
                                if (x == 73 && y == 105)
                                {
                                    level.LoadMovingPlatformTile(72, 103, true, false, true);
                                }


                                // Turn snowmobile around
                                if (x == 223 && y == 143)
                                {
                                    automaticVehicleRight = false;
                                    automaticVehicleSpeed = 2;
                                    SetCheckPoint(x, y);
                                }

                                // Increase snowmobile speed
                                if (x == 139 && y == 145)
                                {
                                    automaticVehicleSpeed = 4;
                                    SetCheckPoint(x, y);
                                }

                                // Remove laser and open end of level entry
                                if (x == 54 && y == 130)
                                {
                                    for (int i = 0; i < 5; i++)
                                        level.RemoveLaser(53 + i, 127);
                                    for (int i = 0; i < 9; i++)
                                        level.Tiles[15 + i, 124] = new Tile(null, TileCollision.Passable);
                                    for (int i = 0; i < 13; i++)
                                        level.Tiles[15, 124 - i] = level.LoadLevelTile("concreteTile", TileCollision.Impassable);
                                }
                                break;

                            // Haunted House level
                            case 6:

                                // Add falling spikes
                                if (x == 73 && (y == 38 || y == 40))
                                {
                                    for (int i = 0; i < 8; i++)
                                        Level.LoadMovingSpikeTile(x + i, 35, true, true, true);

                                    // Increase spike speed
                                    //level.SetMovingSpikeSpeed(MovingSpike.FastMoveSpeed);

                                    level.Tiles[x, y] = level.LoadTile("transparentTile", TileCollision.Trigger); // was triggerTile
                                }

                                if ((x >= 80 || x <= 82) && y == 13)
                                {
                                    // Add laser
                                    for (int i = 0; i < 3; i++)
                                        level.LoadLaserTile(80 + i, 15, true, false, false, RotationDirection.None);
                                }

                                if (x == 13 && (y == 18 || y == 19))
                                {
                                    // Remove both triggers
                                    for (int i = 0; i < 2; i++)
                                        level.Tiles[13, 18 + i] = new Tile(null, TileCollision.Passable);
                                    
                                     // Add moving spikes
                                    for (int i = 0; i < 4; i++)
                                        Level.LoadMovingSpikeTile(19, 17 + i, true, false, false);

                                    //Increase spike speed
                                    level.SetMovingSpikeSpeed(MovingSpike.FastMoveSpeed);
                                }

                                if (x == 60 && y == 10)
                                {
                                    level.Player.inVehicle = false;
                                    SetCheckPoint(x, y);
                                }

                                if (x == 111 && (y == 17 || y == 18))
                                {
                                    // Remove both triggers
                                    for (int i = 0; i < 2; i++)
                                        level.Tiles[111, 17 + i] = new Tile(null, TileCollision.Passable);

                                    // Add lasers
                                    for (int i = 0; i < 2; i++)
                                        level.LoadLaserTile(109, 17 + i, false, true, false, RotationDirection.None);

                                    // Add boss
                                    level.LoadEnemyTile(141, 17,
                                        "Boss3", false);

                                    SetCheckPoint(x, 18);
                                }


                                break;

                            // Street/Rooftop level
                            case 7:

                                // Turn car around
                                if (x == 387 && y == 13)
                                {
                                    automaticVehicleRight = false;
                                    SetCheckPoint(x, y);
                                }

                                // Turn car around again
                                if (x == 322 && y == 31)
                                {
                                    automaticVehicleRight = true;
                                    //automaticVehicleSpeed = 4;
                                    SetCheckPoint(x, y);
                                }
                                break;

                            // Car level
                            case 8:

                                // Grass section

                                // Make enemy trucks
                                if (x == 261)
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 11; i++)
                                        level.Tiles[x, 7 + i] = new Tile(null, TileCollision.Passable);
                                    level.LoadEnemyTile(261, 5, "MonsterU", true);
                                    level.LoadEnemyTile(261, 20, "MonsterU", true);
                                    SetCheckPoint(252, 11);
                                }

                                // Make enemy trucks
                                if (x == 418)
                                {
                                    // Give player gun
                                    hasGun = true;
                                    currGun = Gun.Multidirectional;
                                    // Remove all triggers
                                    for (int i = 0; i < 21; i++)
                                        level.Tiles[x, 2 + i] = level.LoadLevelTile("grassTile", TileCollision.Passable);
                                    level.LoadEnemyTile(419, 4, "MonsterU", true);
                                    level.LoadEnemyTile(439, 11, "MonsterU", true);
                                    level.LoadEnemyTile(419, 19, "MonsterU", true);
                                    SetCheckPoint(x-1, 9);
                                }

                                // Middle of sand section
                                if (x == 610)
                                {   
                                    // Remove all triggers
                                    for (int i = 0; i < 21; i++)
                                        level.Tiles[x, 2 + i] = level.LoadLevelTile("pavementTile", TileCollision.Passable);

                                    // Remove all created enemies
                                    for (int i = 0; i < level.Enemies.Count; i++)
                                    {
                                        Enemy enemy = level.Enemies[i];
                                        if (enemy.createdByEvent)
                                            enemy.Remove();
                                    }
                                    SetCheckPoint(x, 17);
                                }

                                // Snow section
                                // Add checkpoint
                                if (x == 693)
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 23; i++)
                                        level.Tiles[x, 1 + i] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                                    SetCheckPoint(x, 12);
                                }

                                // Make enemy helicopters
                                if (x == 884)
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 23; i++)
                                        level.Tiles[x, 1 + i] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                                    level.LoadEnemyTile(879, 3, "MonsterQ", true);
                                    level.LoadEnemyTile(894, 21, "MonsterQ", true);
                                    SetCheckPoint(884, 14);
                                }

                                // Remove laser 1
                                if (x == 995)
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 23; i++)
                                        level.Tiles[x, 1 + i] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                                    for (int i = 0; i < 17; i++)
                                        level.RemoveLaser(1003, 4 + i);
                                }

                                // Remove laser 2
                                if (x == 1008)
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 23; i++)
                                        level.Tiles[x, 1 + i] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                                    for (int i = 0; i < 13; i++)
                                        level.RemoveLaser(1013, 6 + i);
                                }

                                // Remove laser 3 and add enemy cars
                                if (x == 1018)
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 23; i++)
                                        level.Tiles[x, 1 + i] = level.LoadLevelTile("iceFloorTile", TileCollision.Passable);
                                    for (int i = 0; i < 9; i++)
                                        level.RemoveLaser(1023, 8 + i);
                                    level.LoadEnemyTile(1147, 2, "MonsterL", false);
                                    level.LoadEnemyTile(1080, 3, "MonsterL", false);
                                    level.LoadEnemyTile(1096, 5, "MonsterL", false);
                                    level.LoadEnemyTile(1133, 5, "MonsterL", false);
                                    level.LoadEnemyTile(1152, 7, "MonsterL", false);
                                    level.LoadEnemyTile(1111, 8, "MonsterL", false);
                                    level.LoadEnemyTile(1091, 9, "MonsterL", false);
                                    level.LoadEnemyTile(1122, 10, "MonsterL", false);

                                    level.LoadEnemyTile(1072, 14, "MonsterR", false);
                                    level.LoadEnemyTile(1117, 14, "MonsterR", false);
                                    level.LoadEnemyTile(1142, 16, "MonsterR", false);
                                    level.LoadEnemyTile(1099, 17, "MonsterR", false);
                                    level.LoadEnemyTile(1083, 19, "MonsterR", false);
                                    level.LoadEnemyTile(1142, 19, "MonsterR", false);
                                    level.LoadEnemyTile(1117, 20, "MonsterR", false);
                                    SetCheckPoint(x - 1, 12);
                                }

                                // End of level
                                if (x == 1157)
                                {
                                    level.OnExitReached();
                                    for (int i = 0; i < 2; i++)
                                        level.Tiles[x + i, y] = level.LoadTile("carRaceEndFlagTile", TileCollision.Passable);

                                }

                                break;

                            // Sniper protection level
                            case 10:

                                // Replace this with a foreach loop
                                for (int i = 0; i < automaticJumpPoints.Count; i++)
                                {
                                    Vector2 jumpPoint = automaticJumpPoints[i];
                                    if (x == jumpPoint.X && y == jumpPoint.Y)
                                        automaticJump = true;
                                }

                                // Reset speed if brought back to start
                                if (x == 10 && y == 23)
                                    automaticRunSpeed = NormalRunSpeed;

                                if (x == 114 && y == 19)
                                {
                                    for (int i = 0; i < 8; i++)
                                        level.Tiles[112 + i, 21] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                }

                                if ((x == 119 && y == 20) || (x == 161 && y == 11))
                                    automaticRunSpeed = FastRunSpeed;

                                if (x == 127 && y == 18)
                                    automaticRunSpeed = NormalRunSpeed;

                                if (x == 189 && y == 16)
                                    automaticRunRight = false;

                                if (x == 163 && y == 20)
                                    automaticRunRight = true;

                                if (x == 224 && y == 21)
                                {
                                    automaticRunSpeed = 0;
                                    automaticRunUp = true;
                                }

                                if (x == 224 && y == 5)
                                {
                                    automaticRunSpeed = FastRunSpeed;
                                    automaticRunUp = false;
                                }

                                if (x == 254 && y == 17)
                                {
                                    automaticRunSpeed = 0;
                                    automaticRunUp = true;
                                }

                                if (x == 253 && y == 2)
                                {
                                    automaticRunSpeed = NormalRunSpeed;
                                    automaticRunUp = false;
                                }
                               

                                if (x == 287 && y == 21)
                                {
                                    automaticRunSpeed = NormalRunSpeed;
                                    automaticRunUp = false;
                                }

                                if (x == 284 && y == 16)
                                    automaticRunRight = false;

                                if (x == 258 && y == 21)
                                    automaticRunRight = true;


                                if (x == 304 && y == 14)
                                    automaticRunSpeed = 5;

                                if (x == 426 && y == 22)
                                {
                                    automaticRunSpeed = NormalRunSpeed;
                                    // Display message
                                    Level.Player.InfoString = "I'm nearly there now.";
                                    Level.Player.DrawInfo = true;
                                }

                                if (x == 447 && y == 22)
                                    automaticRunRight = false;

                                if (x == 423 && y == 18)
                                    automaticRunRight = true;

                                if (x == 433 && y == 12)
                                    automaticRunRight = false;

                                if (x == 415 && y == 8)
                                    automaticRunRight = true;

                                if (x == 437 && y == 5)
                                    automaticRunSpeed = 5;

                                // Collect key
                                if (x == 448 && y == 4)
                                {
                                    // Turn around and slow down
                                    automaticRunRight = false;
                                    automaticRunSpeed = NormalRunSpeed;
                                    // Remove laser
                                    for (int i = 0; i < 4; i++)
                                        level.RemoveLaser(438 + i, 6);
                                }

                                if (x == 438 && y == 7)
                                    automaticRunRight = true;
                                

                                if (x == 447 && y == 10)
                                    automaticRunRight = false;

                                break;

                            // Heaven level
                            case 11:

                                // First section - keys unlocks laser
                                if (x == 178 && y == 79)
                                {
                                    for (int i = 0; i < 19; i++)
                                        level.RemoveLaser(110 + i, 69);
                                
                                }

                                // Right/Left doors
                                if ((x == 64 || x == 173) && (y >= 50 && y <= 55))
                                {
                                    inVehicle = false;
                                    int laserOffset = (x==64)? 3 : -3;
                                    for (int i = 0; i < 6; i++)
                                        level.LoadLaserTile(x + laserOffset, 50 + i, false, true, false, RotationDirection.None);
                                    SetCheckPoint(x, 55);
                                }

                                // Automtaically fly upwards with multidirectional gun
                                if (x == 48 && y == 80)
                                {
                                    inVehicle = true;
                                    currVehicle = Vehicle.Wings;
                                    automaticRun = true;
                                    automaticRunUp = true;
                                    automaticRunSpeed = 0;
                                    hasGun = true;
                                    currGun = Gun.Multidirectional;
                                    UpdateReloadTime();
                                    SetCheckPoint(x, y);
                                }

                                // Top of flying section - return player to normal
                                if (y == 39 && (x >= 57 && x <= 63))
                                {
                                    inVehicle = false;
                                    automaticRun = false;
                                    hasGun = false;
                                    shootWithMouse = false;
                                    isShooting = false;
                                    for (int i = 0; i < 6; i++)
                                        level.Tiles[58 + i, 41] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                }

                                if (x == 33 && (y >= 31 && y <= 33))
                                {
                                    inSpace = true;
                                    SetCheckPoint(x, 33);
                                }

                                if (y == 66 && (x >= 7 && x <= 26))
                                {
                                    // Remove all triggers
                                    for (int i = 0; i < 3; i++)
                                        level.Tiles[7 + i, 66] = new Tile(null, TileCollision.Passable);
                                    for (int i = 0; i < 3; i++)
                                        level.Tiles[24 + i, 66] = new Tile(null, TileCollision.Passable);
                                    inSpace = false;
                                    level.LoadEnemyTile(18, 74, "MonsterY", true);
                                }

                                if (x == 302 && y == 77)
                                {
                                    for (int i = 0; i < 12; i++)
                                        level.RemoveLaser(278 + i, 70);
                                }

                                // Make moving platform appear
                                if (x == 190 && y == 52)
                                {
                                    level.LoadMovingPlatformTile(192, 52, true, false, true);
                                }

                                // Laser grid section

                                if (x == 56 && y == 26)
                                {
                                        for (int i = 0; i < 13; i++)
                                            level.RemoveLaser(50 + i, 22);
                                        for (int i = 0; i < 6; i++)
                                            level.LoadLaserTile(48, 24 + i, false, true, false, RotationDirection.None);
                                }

                                if (x == 56 && y == 19)
                                {
                                    for (int i = 0; i < 3; i++)
                                        level.RemoveLaser(48, 18 + i);
                                    for (int i = 0; i < 13; i++)
                                        level.LoadLaserTile(50 + i, 22, false, false, false, RotationDirection.None);
                                }

                                if (x == 44 && y == 19)
                                {
                                    for (int i = 0; i < 6; i++)
                                        level.RemoveLaser(41 + i, 16);
                                    for (int i = 0; i < 3; i++)
                                        level.LoadLaserTile(48, 18 + i, false, true, false, RotationDirection.None);
                                }

                                if (x == 44 && y == 11)
                                {
                                    for (int i = 0; i < 6; i++)
                                        level.RemoveLaser(48, 9 + i);
                                    for (int i = 0; i < 6; i++)
                                        level.LoadLaserTile(41 + i, 16, false, false, false, RotationDirection.None);
                                }

                                if (x == 56 && y == 11)
                                {
                                    for (int i = 0; i < 6; i++)
                                        level.RemoveLaser(64, 9 + i);

                                    periodicLoop = true;
                                }

                                if (x == 77 && (y >= 1 && y <= 4))
                                {
                                    // Remove triggers
                                    for (int i = 0; i < 4; i++)
                                        level.Tiles[77, 1 + i] = new Tile(null, TileCollision.Passable);
                                    // Return player to normal mode
                                    inVehicle = false;
                                    SetCheckPoint(78, 4);
                                }

                                /*
                                // To ensure no dealock before boss
                                if (x == 119 && y == 20)
                                {
                                    for (int i = 0; i < 7; i++)
                                        level.Tiles[116 + i, 6] = new Tile(null, TileCollision.Passable);
                                    for (int i = 0; i < 6; i++)
                                        level.Tiles[117 + i, 30] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                    for (int i = 0; i < 6; i++)
                                        level.Tiles[115, 24 + i] = level.LoadLevelTile("grassFloorTile", TileCollision.Impassable);
                                    // Remove laser 1
                                    for (int i = 0; i < 7; i++)
                                        level.RemoveLaser(116 + i, 17);
                                    // Remove laser 2
                                    for (int i = 0; i < 7; i++)
                                        level.RemoveLaser(116 + i, 23);
                                    // Remove laser 3s (into boss)
                                    for (int i = 0; i < 5; i++)
                                        level.RemoveLaser(249, 17 + i);
                                }
                                 */

                                if (x == 306 && (y >= 21 && y <= 23))
                                {
                                    // Remove triggers
                                    for (int i = 0; i < 3; i++)
                                        level.Tiles[306, 21 + i] = new Tile(null, TileCollision.Passable);
                                    // Return player to normal mode
                                    inVehicle = false;
                                }

                                if ((x >= 306 && x <= 308) && y == 47)
                                {
                                    // Remove triggers
                                    for (int i = 0; i < 3; i++)
                                        level.Tiles[306 + i, 47] = new Tile(null, TileCollision.Passable);
                                    level.OnExitReached();
                                }

                                break;

                            default:
                                // Bonus level
                                // for (int i = 0; i < 3; i++)
                                    // level.LoadLaserTile(x - 2, y - i, true, true, false, RotationDirection.None);
                                break;
                        }
                    }

                    
                    // If player enters door -> load sub-level
                    if (collision == TileCollision.Door)
                    {

                        if (Level.IsMainWorld)
                        {
                            // Default oen door tile
                            String d = "openDoorTile";

                            // Training 1
                            if (x == 85 && y == 18 && Level.FurthestLevelReached >= 0)
                                OpenDoor(x, y, 0, d);
                            // Training 2
                            else if (x == 91 && y == 18 && Level.FurthestLevelReached >= 1)
                                OpenDoor(x, y, 1, d);
                            // Water level (at boat house)
                            else if (x == 237 && y == 47 && Level.FurthestLevelReached >= 2)
                                OpenDoor(x, y, 2, d);
                            // Office level (at stationary shop)
                            else if (x == 25 && y == 18 && Level.FurthestLevelReached >= 3)
                                OpenDoor(x, y, 3, "officeOpenDoorTile");
                            // Space level (at space station)
                            else if (x == 156 && y == 69 && Level.FurthestLevelReached >= 4)
                                OpenDoor(x, y, 4, d);
                            // Snow level (at snow slope)
                            else if (x == 218 && y == 16 && Level.FurthestLevelReached >= 5)
                                OpenDoor(x, y, 5, "iceDoorTile");
                            // Haunted house level
                            else if (x == 23 && y == 81 && Level.FurthestLevelReached >= 6)
                                OpenDoor(x, y, 6, "hauntedOpenDoorTile");
                            // City level
                            else if (x == 98 && y == 63 && Level.FurthestLevelReached >= 7)
                                OpenDoor(x, y, 7, d);
                            // Car race level
                            else if (x == 76 && y == 71 && Level.FurthestLevelReached >= 8)
                                OpenDoor(x, y, 8, d);
                            // Sniper level level (2 levels)
                            else if (x == 132 && y == 18 && Level.FurthestLevelReached >= 9)
                                OpenDoor(x, y, 9, d);
                            // Heaven level
                            else if (x == 159 && y == 18 && Level.FurthestLevelReached >= 11)
                                OpenDoor(x, y, 11, "cloudOpenDoorTile");
                            // Hell level
                            else if (x == 187 && y == 18 && Level.FurthestLevelReached >= 12)
                                OpenDoor(x, y, 12, "hellOpenDoorTile");
                            else
                            {
                                Level.Player.InfoString = "This level is locked at the moment.";
                                Level.Player.DrawInfo = true;
                            }
                        }

                    }


                    // If player is in wind
                    if (collision == TileCollision.Wind)
                    {
                        inWind = true;
                        isOnGround = false;
                    }
                    else inWind = false;

                    // If player is on slope
                    if (collision == TileCollision.Slope)
                    {
                        onSlope = true;
                        isOnGround = true;
                    }
                    // else onSlope = false;

                
                    // If player is on right conveyor belt
                    if (!carRaceMode && collision == TileCollision.Rconveyor)
                    {
                            onRightConveyor = true;
                            isOnGround = true;
                    }
                    else
                       onRightConveyor = false;

                    // If player is on left conveyor belt
                    if (collision == TileCollision.Lconveyor)
                    {

                        onLeftConveyor = true;
                        isOnGround = true;
                    }
                    else onLeftConveyor = false;

                    // If player is on trampoline
                    if (collision == TileCollision.Bounce)
                    {
                        onTrampoline = true;
                        isOnGround = true;
                    }
                    else onTrampoline = false;

                    // If player is on swing pole
                    if (collision == TileCollision.SwingPole)
                    {
                        if (!inVehicle)
                        {
                            onSwingPole = true;
                            isOnGround = false;
                        }
                    }
                    else onSwingPole = false;

                    // If tile above is water then must be under water (or if current tile is water && tile above is impassable then must be under water)
                    if (Level.GetCollision(x, y - 1) == TileCollision.Water || (collision == TileCollision.Water && Level.GetCollision(x, y - 1) == TileCollision.Impassable))
                    {
                        isSwimming = true;
                        onWaterSurface = false;
                    }
                    // Else if tile under player is water must be on surface (NB not on ground)
                    else if (collision == TileCollision.Water)
                    {
                        isSwimming = false;
                        onWaterSurface = true;
                    }
                    // Otherwise not in contact with water
                    else
                    {
                        isSwimming = false;
                        onWaterSurface = false;
                    }

                    // If player is on monkey bar
                    if (collision == TileCollision.Monkeybar)
                    {
                        onMonkeyBar = true;
                        //onLadder = false;
                    }
                    else
                        onMonkeyBar = false;

                    // List of things player should not be able to pass through freely
                    if (collision == TileCollision.Impassable || collision == TileCollision.Platform || collision == TileCollision.Slope
                        || collision == TileCollision.Bounce || collision == TileCollision.Rconveyor || collision == TileCollision.Lconveyor
                        || collision == TileCollision.Turret || collision == TileCollision.Target || collision == TileCollision.SwingPole
                        || level.movingPlatformContact)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform || collision == TileCollision.Rconveyor || collision == TileCollision.Lconveyor || level.movingPlatformContact)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    isOnGround = true;

                                //if (onLadder)
                                  //  isOnGround = false;

                                if (onSlope)
                                {
                                    Position = new Vector2(Position.X, Position.Y + MathHelper.Clamp(Position.X - tileBounds.X, 0, 16));// + depth.Y);
                                    bounds = BoundingRectangle;
                                }

                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || IsOnGround) // || level.movingPlatformContact)
                                {
                                    wasOnSlope = false;
                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable || collision == TileCollision.Bounce || level.movingPlatformContact) // Ignore platforms.
                            {
                                onSlope = false;
                                wasOnSlope = false;
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }


        // Loads level and opens level door
        private void OpenDoor(int x, int y, int levelIndex, String openDoorTile)
        {
            SetMainLevelCheckPoint(x, y + 1);
            level.Tiles[x, y] = level.LoadLevelTile(openDoorTile, TileCollision.Passable);
            levelToLoad = levelIndex;
            atDoor = true;
        }

        /* Called when the player has been killed.
         * The enemy who killed the player. This parameter is null if the player was
         * not killed by an enemy (fell into a hole).
         */
        public void OnKilled(Enemy killedBy)
        {
            // If killed by an object
                // If in automatic run mode player has a health bar
                if (automaticRun)
                {
                    playerHealth -= playerMaxHealth * 0.25f;
                    if (playerHealth <= 1)
                    {
                        if (isAlive)
                            Game.PlaySound(killedSound);
                        isAlive = false;
                        level.DoorTimerOn = false;
                        sprite.PlayAnimation(dieAnimation);
                    }
                }
                else
                {
                    if (isAlive)
                         Game.PlaySound(killedSound);
                    isAlive = false;
                    level.DoorTimerOn = false;
                    sprite.PlayAnimation(dieAnimation);

                    if (level.LevelIndex == 12)
                        Enemy.level12bossCount = 0;

                    // If killed by explosive mine also explode the mine
                    if (killedBy != null && killedBy.monsterType == "MonsterD")
                        killedBy.OnKilled(null);

                }
        }

        // Called when this player reaches the level's exit.
        public void OnReachedExit()
        {
            switch (level.LevelIndex)
            {
                case 0:
                case 1:
                case 3:
                case 9:
                    sprite.PlayAnimation(celebrateAnimation);
                    break;
                default:
                    break;
            }
        }

        // Draws the animated player.
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Flip the sprite to face the way we are moving.
            if (Velocity.X > 0)
                flip = SpriteEffects.None;
            else if (Velocity.X < 0)
                flip = SpriteEffects.FlipHorizontally;

            // Draw sprite.
            if (onSwingPole)
            {
                if (swingPoleRight)
                    flip = SpriteEffects.None;
                else
                    flip = SpriteEffects.FlipHorizontally;

                Vector2 pos = new Vector2(Position.X, Position.Y + 25);
                    spriteBatch.Draw(swingPoleTex, pos, null, Color.White, RotationAngle, swingOrigin, 1.0f, flip, 0f);

            }
            else
                sprite.Draw(gameTime, spriteBatch, Position, flip);

            // In automatic run mode player has health bar
            if (isAlive && level.LevelIndex == 10)
                drawHealthBar(spriteBatch);

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

        // Draws info strings (defined in 'Info' class) and also a rectangular background for the text
        public void DrawInfoString(SpriteBatch spriteBatch)
        {
            if (drawInfo)
            {
                Viewport vp = spriteBatch.GraphicsDevice.Viewport;
                float strWidth = infoFont.MeasureString(infoString).X;
                float strHeight = infoFont.MeasureString(infoString).Y;
                int lineCount = 1;

                // Count number of lines in this particular info string (used to determine background rectangle size)
                for (int i = 0; i < infoString.Length; i++)
                {
                    if (infoString[i] == '\n')
                        lineCount++;
                }


                // Draw background rectangle (appropriate size depending on text)
                Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, vp.Width, (int)(lineCount*(strHeight+10)));

                Color[] data = new Color[rect.Width * rect.Height];
                for (int i = 0; i < data.Length; ++i) data[i] = Color.Black;
                rect.SetData(data);

                Vector2 coor = new Vector2(0, vp.Height - strHeight - 20);
                spriteBatch.Draw(rect, coor, Color.White);

                // Draw text string defined in 'Info' class)
                spriteBatch.DrawString(infoFont, infoString, new Vector2((vp.Width - strWidth) / 2, vp.Height - strHeight - 20), Color.Yellow);
            }

            
        }

        // Draws player health bar
        public void drawHealthBar(SpriteBatch spriteBatch)
        {
            int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
            int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

            int healthBarWidth = (int)(walkAnimation.FrameWidth * playerHealth / playerMaxHealth);
            int healthBarHeight = (int)(walkAnimation.FrameHeight / 10);

            Color healthBarColor;
            if (playerHealth / playerMaxHealth >= 0.8f)
                healthBarColor = Color.Green;
            else if (playerHealth / playerMaxHealth >= 0.5f)
                healthBarColor = Color.Yellow;
            else if (playerHealth / playerMaxHealth >= 0.2f)
                healthBarColor = Color.Orange;
            else //if (bossHealth / bossMaxHealth >= 0.2f)
                healthBarColor = Color.Red;

            Texture2D rect = new Texture2D(spriteBatch.GraphicsDevice, healthBarWidth, healthBarHeight);

            Color[] data = new Color[healthBarWidth * healthBarHeight];
            for (int i = 0; i < data.Length; ++i)
                data[i] = healthBarColor;
            rect.SetData(data);

            Vector2 coor = new Vector2(position.X - walkAnimation.FrameWidth / 2, position.Y - walkAnimation.FrameHeight - healthBarHeight - 5);
            spriteBatch.Draw(rect, coor, Color.White);
        }
    }
}