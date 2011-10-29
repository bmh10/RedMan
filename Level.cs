using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace RedMan
{
    /*
     * Level class gets the tile layout for the current level, stores and gems and enemies and also owns the player.
     * The level also controls the game's win/loose conditions as well as scoring.
     */
    class Level : IDisposable
    {
        //** Physical level structure **\\
        public Tile[,] Tiles
        {
            get { return tiles; }
        }
        private Tile[,] tiles;
        private Layer[] layers;

        // Stores original level read in from txt file as chars
        public char[,] LevelChars
        {
            get { return levelChars; }
        }
        char[,] levelChars;

        private const int EntityLayer = 2;

        //** Entities in the level **\\
        public Player Player
        {
            get { return player; }
        }
        Player player;

        private List<Gem> gems = new List<Gem>();
        private List<Elevator> elevators = new List<Elevator>();

        
        public List<MovingPlatform> MovingPlatforms
        {
            get { return movingPlatforms; }
        }
        List<MovingPlatform> movingPlatforms = new List<MovingPlatform>();
        public bool movingPlatformContact;


        public List<MovingSpike> MovingSpikes
        {
            get { return movingSpikes; }
        }
        List<MovingSpike> movingSpikes = new List<MovingSpike>();


        public List<Laser> Lasers
        {
            get { return lasers; }
        }
        List<Laser> lasers = new List<Laser>();


        public List<Info> Infos
        {
            get { return infos; }
        }
        List<Info> infos = new List<Info>();


        public List<Enemy> Enemies
        {
            get { return enemies; }
        }
        List<Enemy> enemies = new List<Enemy>();


        public List<Turret> Turrets
        {
            get { return turrets; }
        }
        List<Turret> turrets = new List<Turret>();
      

        public List<FallingPlatform> FallingPlatforms
        {
            get { return fallingPlatforms; }
        }
        List<FallingPlatform> fallingPlatforms = new List<FallingPlatform>();


        public List<Bullet> Bullets
        {
            get { return bullets; }
        }
        List<Bullet> bullets = new List<Bullet>();


        // Key locations in the level
        private static readonly Point InvalidPosition = new Point(-1, -1);
        private Vector2 start;
        private Point exit = InvalidPosition;

        public static Vector2 LastCheckPoint
        {
            get { return lastCheckPoint; }
            set { lastCheckPoint = value; }
        }
        static Vector2 lastCheckPoint;

        public static Vector2 LastMainLevelCheckPoint;

        public static int FurthestLevelReached;

        //** Level state **\\

        private Random random = new Random(354668); // Arbitrary, but constant seed
        public Vector2 cameraPosition;

        public int LevelIndex
        {
            get { return levelIndex; }
        }
        int levelIndex;


        public static bool IsMainWorld;
        public static bool LoadMainWorldFromLevel = false;
        private bool pWasReleased;

        // Score for this specific level
        public int Score
        {
            get { return score; }
            set
            {
                if (value < 0)
                    score = 0;
                else 
                    score = value;
            }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        // Time remaining to finish level
        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;

        // Has countdown timer been activated
        public bool CountDownTimerOn
        {
            get { return countDownTimerOn; }
        }
        bool countDownTimerOn;


        // Time remaining before door closes
        public TimeSpan DoorTimer
        {
            get { return doorTimer; }
        }
        TimeSpan doorTimer;

        // Has door timer been activated
        public bool DoorTimerOn
        {
            get { return doorTimerOn; }
            set { doorTimerOn = value; }
        }
        bool doorTimerOn;


        // Time remaining at the last checkpoint reached (used when countdown timer is on to give player
        // a fair amount of time if they die during a closely timed section)
        public TimeSpan TimeAtLastCheckpoint
        {
            get { return timeAtLastCheckpoint; }
            set { timeAtLastCheckpoint = value; }
        }
        TimeSpan timeAtLastCheckpoint;


        // Number of points awarded per second left over at end of level
        private const int PointsPerSecond = 3;

        // Level content
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        private SoundEffect exitReachedSound;
        private Song backingTrack, pauseMusic;

        #region Loading

        public Level(IServiceProvider serviceProvider, Stream fileStream, int levelIndex) // , int subLevelIndex)
        {
            this.levelIndex = levelIndex;
            IsMainWorld = (levelIndex == -1);

            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");

            SetLevelTimer();

            Bullet.ResetTargetCounts();

            LoadTiles(fileStream);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            string levelTheme = GetLevelTheme();
            layers = new Layer[3];
            layers[0] = new Layer(Content, "Backgrounds/" + levelTheme + "/Layer0", 0.2f);
            layers[1] = new Layer(Content, "Backgrounds/" + levelTheme + "/Layer1", 0.5f);
            layers[2] = new Layer(Content, "Backgrounds/" + levelTheme + "/Layer2", 0.8f);

            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Sounds/ExitReached");
            // Load backing track depending on level
            
            backingTrack = Content.Load<Song>("Sounds/Backing/" + levelIndex);
            pauseMusic = Content.Load<Song>("Sounds/Backing/pause");
            MediaPlayer.Play(backingTrack);
            MediaPlayer.IsRepeating = true;
        }


        // NB ADD THEMES FOR EACH NEW LEVEL
        // Gets theme (folder name) for current level backgrounds
        private string GetLevelTheme()
        {
            switch (levelIndex)
            {
                case -1:
                    return "City";
                case 0:
                    return "Training";
                case 1:
                    return "Training";
                case 2:
                    return "Water";
                case 3:
                    return "Office";
                case 4:
                    return "Space";
                case 5:
                    return "Snow";
                case 6:
                    return "House";
                case 7:
                    return "Training";
                case 8:
                    return "City";
                case 9:
                    return "Training";
                case 10:
                    return "Training";
                case 11:
                    return "Heaven";
                case 12:
                    return "Hell";
                case 13:
                    return "Training";
               
                default:
                    return "Jungle";
            }
        }

        // Sets whether a level is in 2D (false) or sudo 3D (true) perspective
        public bool inSudo3D()
        {
            switch (levelIndex)
            {
                case -1:
                case 8: return true;
                default: return false;
            }
        }

        // Sets whether a level is in car race mode or not
        public bool inCarRaceMode()
        {
            switch (levelIndex)
            {
                case 8: return true;
                default: return false;
            }

        }

        // Sets whether a level is in sniper mode or not
        public bool inSniperMode()
        {
            switch (levelIndex)
            {
                case 9:
                case 10: return true;
                default: return false;
            }
        }

        // Sets whether a level is in running mode or not
        public bool inRunningMode()
        {
            switch (levelIndex)
            {
                case 10: return true;
                default: return false;
            }

        }

        // NB ADD TIMES FOR EACH NEW LEVEL
        // Sets timer depending on length/difficulty of level
        private void SetLevelTimer()
        {
            switch(levelIndex)
            {
                case -1:
                     timeRemaining = TimeSpan.FromMinutes(60.0); break;
                case 0:
                    timeRemaining = TimeSpan.FromMinutes(10.0); break;
                case 1:
                     timeRemaining = TimeSpan.FromMinutes(10.0); break;
                case 2:
                    timeRemaining = TimeSpan.FromMinutes(15.0); break;
                case 3:
                    timeRemaining = TimeSpan.FromMinutes(15.0); break;
                case 13:
                    timeRemaining = TimeSpan.FromMinutes(5.0); break;
                default:
                   timeRemaining = TimeSpan.FromMinutes(20.0); break;
            }
        }



        /*
         * Loads a level from a text file. Verifies that all lines are same length, then uses the characters to load the tile
         * texture and collision data. Also checks to see that levels is valid (ie has en entry and exit point).
         */
        private void LoadTiles(Stream fileStream)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                string line = reader.ReadLine();
                width = line.Length;
                while (line != null)
                {
                    lines.Add(line);
                    if (line.Length != width)
                        throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                    line = reader.ReadLine();
                }
            }

            
            // Allocate the tile grid.
            tiles = new Tile[width, lines.Count];
            levelChars = new char[width, lines.Count];

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    // Store level as chars for reloading platforms
                    levelChars[x,y] = tileType;
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");

                if (exit == InvalidPosition)
                    throw new NotSupportedException("A level must have an exit.");

                Player.DrawInfo = true;
                Player.InfoString = Info.getInfo(levelIndex, -1);
        }

        // Loads an individual tile's appearance and behavior.
        private Tile LoadTile(char tileType, int x, int y)
        {
            // Load all generic tiles here - always used
                switch (tileType)
                {
                    // Blank tile
                    case '.':
                        return new Tile(null, TileCollision.Passable);
                    

                    //******WEAPONS******//

                    // Pistol tile
                    case 'p':
                        return LoadTile("pistolTile", TileCollision.Gun);

                    // Uzi tile
                    case 'u':
                        return LoadTile("uziTile", TileCollision.Gun);

                    // Sniper tile
                    case 's':
                        return LoadTile("sniperTile", TileCollision.Gun);

                    // Harpoon tile
                    case 'h':
                        return LoadTile("harpoonTile", TileCollision.Gun);

                    // Space blaster tile
                    case 'B':
                        return LoadTile("spaceBlasterTile", TileCollision.Gun);

                    // Multidirectional gun tile
                    case 'X':
                        return LoadTile("multidirectionalGunTile", TileCollision.Gun);

                    // Power bullet tile
                    case 'Z':
                        return LoadTile("powerBulletTile", TileCollision.Gun);

                    //******END WEAPONS******//

                    // Save tile (Checkpoint)
                    case 'C':
                        return LoadTile("saveTile", TileCollision.Save);

                    // Info tile
                    case 'i':
                        return LoadInfoTile(x, y);

                    //**Transparent Tiles**//

                    // Stopper tile (Change to transparent when finished)
                    case '#':
                        return LoadTile("transparentTile", TileCollision.Stopper); // was "stopperTile"

                    //**End Transparent Tiles**//


                    // Turret tile (horizontal fire)
                    case 'T':
                        return LoadTurretTile(x, y, false, false);

                    // Turret tile (multidirectional fire)
                    case 'M':
                        return LoadTurretTile(x, y, false, true);

                    // Player 1 start point
                    case 'S':
                        return LoadStartTile(x, y);

                    // Exit tile
                    case 'E':
                        return LoadExitTile(x, y);
                    

                    //******VEHICLES******//
                    // Vehcile tile - loads different vehicle depending on level
                    case 'v':
                        return LoadLevelTile("vehicleTile", TileCollision.Vehicle);

                    //******END VEHICLES******//

                    // Normal Gem
                    case 'G':
                        return LoadGemTile(x, y, 0);

                    // Water Gem
                    case '@':
                        return LoadGemTile(x, y, 1);

                    // Wind Gem
                    case 'w':
                        return LoadGemTile(x, y, 2);

                    // Plus one minute tile
                    //case 'P':
                        //return LoadTile("plusOneTile", TileCollision.PlusOne);

                   
                    // If not match pass to other tile loading methods
                    default:
                        if (inSudo3D())
                            return Load3DTile(tileType, x, y);
                        else
                            return Load2DTile(tileType, x, y);
            }
        }

         // Loads tiles for sudo 2D levels
        private Tile Load2DTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Trigger tile (Change to transparent when finished)
                case 't':
                    return LoadTile("transparentTile", TileCollision.Trigger); // was "triggerTile"

                // Grass floor tile
                case '1':
                    return LoadLevelTile("grassFloorTile", TileCollision.Impassable);

                // Concrete block
                case '2':
                    return LoadLevelTile("concreteTile", TileCollision.Impassable);

                // Dirt tile
                case '3':
                    return LoadTile("dirtTile", TileCollision.Impassable);

                // Sloping grass tile
                case '4':
                    return LoadLevelTile("slopeGrassFloorTile", TileCollision.Slope);

                // Tile for visual purposes only
                case '9':
                    return LoadLevelTile("visualTile", TileCollision.Passable);

                // Tile for visual purposes only
                case '0':
                    return LoadLevelTile("visualTile2", TileCollision.Passable);

                // Tile for visual purposes only
                case 'V':
                    return LoadLevelTile("visualTile3", TileCollision.Impassable);

                // Spike tile (up)
                case '5':
                    return LoadLevelTile("spikeTile", TileCollision.Death);

                // Spike tile (down)
                case '8':
                    if (GetTileSet() == "Street/")
                        return LoadTile("City/doorTile", TileCollision.Impassable);
                    return LoadLevelTile("spikeDownTile", TileCollision.Death);

                // Spike tile (left)
                case ')':
                    return LoadLevelTile("spikeLeftTile", TileCollision.Death);

                // Spike tile (Right)
                case '(':
                    return LoadLevelTile("spikeRightTile", TileCollision.Death);

                // Moving spike tile (horizontal)
                case '[':
                    return LoadMovingSpikeTile(x, y, false, false, true);

                // Moving spike tile (vertical)
                case ']':
                    return LoadMovingSpikeTile(x, y, false, true, true);

                // Ladder tile
                case '6':
                    return LoadLevelTile("ladderTile", TileCollision.Ladder);

                // Monkey bar tile
                case '7':
                    return LoadTile("monkeyBarTile", TileCollision.Monkeybar);

                // Water block
                case '~':
                    return LoadTile("waterTile", TileCollision.Water);

                // Water top block
                case '/':
                    return LoadTile("waterTopTile", TileCollision.Passable);

                // Swing pole block
                case 'o':
                    return LoadTile("swingPoleTile", TileCollision.SwingPole);

                // Trampoline block
                case '^':
                    if (levelIndex == 11)
                        return LoadLevelTile("flyUpTile", TileCollision.Trigger);
                    return LoadLevelTile("bounceTile", TileCollision.Bounce);

                // Right conveyor tile
                case '>':
                    return LoadTile("rightTile", TileCollision.Rconveyor);

                // Left conveyor tile
                case '<':
                    return LoadTile("leftTile", TileCollision.Lconveyor);


                // Moving platform tile (horizontal movement, horizontal orientation)
                case '_':
                    return LoadMovingPlatformTile(x, y, false, false, true);

                // Moving platform tile (horizontal movement, vertical orientation)
                case '|':
                    return LoadMovingPlatformTile(x, y, false, false, false);

                // Moving platform tile (vertical movement, horizontal orientation)
                case ':':
                    return LoadMovingPlatformTile(x, y, false, true, true);

                // Moving platform tile (vertical movement, vertical orientation)
                case ';':
                    return LoadMovingPlatformTile(x, y, false, true, false);

                // Floating platform
                case '-':
                    return LoadFallingPlatformTile(x, y);

                // Target tile
                case '+':
                    return LoadTile("targetTile", TileCollision.Target);

                //******LASER TILES*******//

                // Laser tile (vertical)
                case 'l':
                    return LoadLaserTile(x, y, false, true, false, RotationDirection.None);

                // Laser tile (horizontal)
                case '=':
                    return LoadLaserTile(x, y, false, false, false, RotationDirection.None);

                // Flashing Laser tile (vertical)
                case '*':
                    if (levelIndex == 9)
                        return LoadTile("brickTargetTile", TileCollision.Target);
                    return LoadLaserTile(x, y, false, true, true, RotationDirection.None);

                // Flashing Laser tile (horizontal)
                case '&':
                    return LoadLaserTile(x, y, false, false, true, RotationDirection.None);

                // Laser tile (rotation clockwise, not flashing)
                case '{':
                    return LoadLaserTile(x, y, false, true, false, RotationDirection.Clockwise);
                //******END LASER TILES******//

                // Elevator tile (Lift)
                case 'L':
                    return LoadElevatorTile(x, y);

                // Wind tile
                case 'W':
                    return LoadTile("windTile", TileCollision.Wind);

                // Invisible wind tile
                case ',':
                    if (GetTileSet() == "Street/")
                        return LoadLevelTile("brickPlatformTile", TileCollision.Impassable);
                    return new Tile(null, TileCollision.Wind);

                // Fan tile
                case 'F':
                    return LoadTile("fanTile", TileCollision.Impassable);

                // Wing tile
                case '%':
                    return LoadTile("wingTile", TileCollision.Wings);

                // Collectable item tile
                case 'A':
                    return LoadLevelTile("collectableTile", TileCollision.Collectable);

                // Trigger tile (Object)
                case 'O':
                    return LoadLevelTile("visibleTriggerTile", TileCollision.Trigger);

                // Timer tile
                case '!':
                    return LoadTile("timerTile", TileCollision.Timer);


                //******ENEMIES******// (2D)
                case 'a':
                    return LoadEnemyTile(x, y, "MonsterA", false);
                case 'b':
                    return LoadEnemyTile(x, y, "MonsterB", false);
                case 'c':
                    return LoadEnemyTile(x, y, "MonsterC", false);
                case 'd':
                    return LoadEnemyTile(x, y, "MonsterD", false);
                case 'e':
                    return LoadEnemyTile(x, y, "Boss1", false);
                case 'f':
                    return LoadEnemyTile(x, y, "MonsterF", false);
                case 'g':
                    return LoadEnemyTile(x, y, "MonsterG", false);
                case 'H':
                    return LoadEnemyTile(x, y, "MonsterH", false);
                case 'I':
                    return LoadEnemyTile(x, y, "MonsterI", false);
                case 'j':
                    return LoadEnemyTile(x, y, "Boss2", false);
                case 'k':
                    return LoadEnemyTile(x, y, "MonsterK", false);
                case 'm':
                    return LoadEnemyTile(x, y, "MonsterM", false);
                case 'n':
                    return LoadEnemyTile(x, y, "MonsterN", false);
                case 'N':
                    return LoadEnemyTile(x, y, "Boss3", false);
                case 'P':
                    return LoadEnemyTile(x, y, "MonsterP", false);
                case 'q':
                    return LoadEnemyTile(x, y, "MonsterQ", false);
                case 'x':
                    return LoadEnemyTile(x, y, "MonsterX", false);
                case 'y':
                    return LoadEnemyTile(x, y, "MonsterY", false);
                case 'z':
                    return LoadEnemyTile(x, y, "Boss4", false);
                case 'Y':
                    return LoadEnemyTile(x, y, "Boss5", false);
                //******END ENEMIES*****//


                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }


        // Loads tiles for sudo 3D levels (which have different tile set requirements)
        private Tile Load3DTile(char tileType, int x, int y)
        {
                switch (tileType)
                {

                    // Tiles to use in both car race mode and in main world

                    // Tree block
                    case '2':
                        return LoadLevelTile("treeTile", TileCollision.Impassable);
                    

                    // Road marking tile (horiz)
                    case '-':
                        return LoadLevelTile("roadMarkingHorizTile", TileCollision.Passable);

                    // Pavement tile
                    case '1':
                        return LoadLevelTile("pavementTile", TileCollision.Passable);

                    // Grass tile
                    case ',':
                        return LoadLevelTile("grassTile", TileCollision.Passable);

                    // Ice brick tile
                    case '"':
                        return LoadLevelTile("iceFloorTile", TileCollision.Passable);

                    // Concrete tile
                    case 'c':
                        return LoadLevelTile("concreteTile", TileCollision.Impassable);
                   

                    default:

                        if (inCarRaceMode())
                        {
                            // Tiles only used in car race mode
                            switch (tileType)
                            {
                                // Load different triggers depending on zone in level
                                case 't':
                                    if (x > 690)
                                        return LoadLevelTile("iceFloorTile", TileCollision.Trigger);
                                    if (x > 524)
                                        return LoadLevelTile("pavementTile", TileCollision.Trigger);
                                    if (x > 263)
                                        return LoadLevelTile("grassTile", TileCollision.Trigger);
                                    return LoadTile("transparentTile", TileCollision.Trigger);


                                case '5':
                                    return LoadLevelTile("spikeTile", TileCollision.Death);

                                case 'o':
                                    return LoadLevelTile("holeTile", TileCollision.Death);
                                case 'a':
                                    if (x > 524 && x < 694)
                                        return LoadLevelTile("holeSandTLTile", TileCollision.Death);
                                    return LoadLevelTile("holeTLTile", TileCollision.Death);
                                case 'd':
                                    if (x > 524 && x < 694)
                                        return LoadLevelTile("holeSandTRTile", TileCollision.Death);
                                    return LoadLevelTile("holeTRTile", TileCollision.Death);
                                case 'z':
                                    if (x > 524 && x < 694)
                                        return LoadLevelTile("holeSandBLTile", TileCollision.Death);
                                    return LoadLevelTile("holeBLTile", TileCollision.Death);
                                case 'x':
                                    if (x > 524 && x < 694)
                                        return LoadLevelTile("holeSandBRTile", TileCollision.Death);
                                    return LoadLevelTile("holeBRTile", TileCollision.Death);

                                case 'r':
                                    return LoadEnemyTile(x, y, "MonsterR", false);
                                case 'l':
                                    return LoadEnemyTile(x, y, "MonsterL", false);
                                case 'U':
                                    return LoadEnemyTile(x, y, "MonsterU", false);
                                case '0':
                                    return LoadLevelTile("oilSlickTile", TileCollision.Oil);
                                case '/':
                                    return LoadLevelTile("rampTile", TileCollision.Ramp);
                                // Boost tile
                                case '>':
                                    return LoadTile("rightTile", TileCollision.Rconveyor);
                                // Lamp post tile
                                case 'L':
                                    return LoadLevelTile("lamppostTile", TileCollision.Passable);

                                case '+':
                                        return LoadTile("iceTargetTile", TileCollision.Target);

                                //**Water Tiles**//
                                // Water tile
                                case '~':
                                    return LoadTile("waterTile", TileCollision.Death);

                                // Water tile (top right)
                                case ')':
                                    if (x > 694)
                                        return LoadLevelTile("waterIceTRTile", TileCollision.Death);
                                    return LoadLevelTile("waterGrassTRTile", TileCollision.Death);

                                // Water tile (top left)
                                case '(':
                                    if (x > 694)
                                        return LoadLevelTile("waterIceTLTile", TileCollision.Death);
                                    return LoadLevelTile("waterGrassTLTile", TileCollision.Death);

                                // Water tile (bottom right)
                                case ']':
                                    if (x > 694)
                                        return LoadLevelTile("waterIceBRTile", TileCollision.Death);
                                    return LoadLevelTile("waterGrassBRTile", TileCollision.Death);

                                // Water tile (bottom left)
                                case '[':
                                    if (x > 694)
                                        return LoadLevelTile("waterIceBLTile", TileCollision.Death);
                                    return LoadLevelTile("waterGrassBLTile", TileCollision.Death);

                                // Laser tile (vert)
                                case '|':
                                    return LoadLaserTile(x, y, false, true, false, RotationDirection.None);

                                // Laser tile (horiz)
                                case '=':
                                    return LoadLaserTile(x, y, false, false, false, RotationDirection.None);

                                // Laser tile (rotating)
                                case '%':
                                    return LoadLaserTile(x, y, false, true, false, RotationDirection.Clockwise);

                                // Unknown tile type character
                                default:
                                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
                            }

                        }
                        else
                        {
                            // Tiles only used in main world
                            switch (tileType)
                            {
                                // Pavement edge tiles
                                case 'U':
                                    return LoadLevelTile("pavementEdgeUpTile", TileCollision.Passable);

                                case 'D':
                                    return LoadLevelTile("pavementEdgeDownTile", TileCollision.Passable);

                                case 'r':
                                    return LoadLevelTile("pavementEdgeRightTile", TileCollision.Passable);

                                case 'l':
                                    return LoadLevelTile("pavementEdgeLeftTile", TileCollision.Passable);

                                // Road marking tile (vert)
                                case '|':
                                    return LoadLevelTile("roadMarkingTile", TileCollision.Passable);

                                // Lamp post tile
                                case 'L':
                                    return LoadLevelTile("lamppostTile", TileCollision.Impassable);

                                // House brick tile
                                case '4':
                                    return LoadLevelTile("brickTile", TileCollision.Impassable);

                                case '+':
                                    return LoadLevelTile("sandTargetTile", TileCollision.Impassable);

                                //**Haunted House Bricks**//
                                // Dark brick tile
                                case '3':
                                    return LoadLevelTile("darkBrickTile", TileCollision.Impassable);

                                // Haunted door tile
                                case 'n':
                                    return LoadLevelTile("hauntedDoorTile", TileCollision.Door);

                                // Grave tile
                                case 'g':
                                    return LoadLevelTile("graveTile", TileCollision.Impassable);

                                //**Office Bricks**//
                                // Office brick tile
                                case '7':
                                    return LoadLevelTile("officeTile", TileCollision.Impassable);

                                // Office roof tile
                                case 'm':
                                    return LoadLevelTile("officeRoofTile", TileCollision.Impassable);

                                // Office door tile
                                case 'N':
                                    return LoadLevelTile("officeDoorTile", TileCollision.Door);

                                //**Heaven Bricks**//
                                // Cloud tile
                                case 'a':
                                    return LoadLevelTile("cloudTile", TileCollision.Impassable);

                                // Cloud roof tile
                                case 'F':
                                    return LoadLevelTile("cloudRoofTile", TileCollision.Impassable);

                                // Cloud door tile
                                case 'e':
                                    return LoadLevelTile("cloudDoorTile", TileCollision.Door);

                                //**Hell Bricks**//
                                // Hell brick tile
                                case 'H':
                                    return LoadLevelTile("hellBrickTile", TileCollision.Impassable);

                                // Hell roof tile
                                case 'j':
                                    return LoadLevelTile("hellRoofTile", TileCollision.Impassable);

                                // Hell door tile
                                case 'J':
                                    return LoadLevelTile("hellDoorTile", TileCollision.Door);

                                //**Ice bricks**//
                                // Ice brick tile
                                case 'I':
                                    return LoadLevelTile("iceBrickTile", TileCollision.Impassable);

                                // Ice roof tile
                                case 'k':
                                    return LoadLevelTile("iceRoofTile", TileCollision.Impassable);

                                // Ice door tile
                                case 'K':
                                    return LoadLevelTile("iceDoorTile", TileCollision.Door);

                                // Snowball tile
                                case '%':
                                    return LoadLevelTile("snowBallTile", TileCollision.Impassable);

                                // Icicle tile
                                case 't':
                                    return LoadLevelTile("icicleTile", TileCollision.Impassable);

                                //**Space bricks**//
                                case 'O':
                                    return LoadLevelTile("ufoTile", TileCollision.Impassable);
                                case '!':
                                    return LoadLevelTile("rocketTile", TileCollision.Impassable);

                                //**Water Tiles**//
                                // Water tile
                                case '~':
                                    return LoadTile("waterTile", TileCollision.Impassable);

                                // Water tile (top right)
                                case ')':
                                    return LoadLevelTile("waterTRTile", TileCollision.Impassable);

                                // Water tile (top left)
                                case '(':
                                    return LoadLevelTile("waterTLTile", TileCollision.Impassable);

                                // Water tile (bottom right)
                                case ']':
                                    return LoadLevelTile("waterBRTile", TileCollision.Impassable);

                                // Water tile (bottom left)
                                case '[':
                                    return LoadLevelTile("waterBLTile", TileCollision.Impassable);

                                
                                //**Car Tiles**//
                                case 'A':
                                    return LoadLevelTile("carTile", TileCollision.Impassable);

                                case 'd':
                                    return LoadLevelTile("carTile1", TileCollision.Impassable);


                                // House door tile
                                case '5':
                                    return LoadLevelTile("doorTile", TileCollision.Door);

                                // House roof tile
                                case '8':
                                    return LoadLevelTile("roofTile", TileCollision.Impassable);

                                // House roof tile 2
                                case '9':
                                    return LoadLevelTile("roofTile2", TileCollision.Impassable);

                                // Ladder tile
                                case '6':
                                    return LoadLevelTile("ladderTile", TileCollision.Ladder);

                                // Grass tile
                                case ',':
                                    return LoadLevelTile("grassTile", TileCollision.Passable);

                                // Window tile
                                case 'o':
                                    return LoadLevelTile("windowTile", TileCollision.Impassable);
                               
                                // Fence tile (horizontal)
                                case '=':
                                    return LoadLevelTile("fenceHorizTile", TileCollision.Impassable);

                                // Fence tile (vertical)
                                case '/':
                                    return LoadLevelTile("fenceVertTile", TileCollision.Impassable);

                                // Bench tile
                                case 'b':
                                    return LoadLevelTile("benchTile", TileCollision.Impassable);

                                // Fountain tile
                                case 'f':
                                    return LoadLevelTile("fountainTile", TileCollision.Impassable);

                                // Tube tile
                                case '&':
                                    return LoadLevelTile("tubeTile", TileCollision.Impassable);

                                // Unknown tile type character
                                default:
                                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
                            }
                        }
                }
            }

        /*
        * Loads a new tile (loads different tiles depending on level)
        */
        public Tile LoadLevelTile(string name, TileCollision collision)
        {
           
            return new Tile(Content.Load<Texture2D>("Tiles/" + GetTileSet() + name), collision);
        }

        // Returns the folder name where the current level's tiles are stored
        public string GetTileSet()
        {
            switch (levelIndex)
            {
                case -1: return "City/";
                case 3: return "Office/";
                case 4: return "Space/";
                case 5: return "Snow/";
                case 6: return "House/";
                case 7: return "Street/";
                case 8: return "City/";
                case 9: return "Street/";
                case 10: return "Sniper/";
                case 11: return "Heaven/";
                case 12: return "Hell/";
                
                default: return "";
            }
        }


        /*
         * Loads a new tile (loads global tiles which are the same in every level)
        */
        public Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }


        /*
         * Reloads all levels platforms, lifts and gun pickups as the may have been disturbed during previous play
         */
        private void reloadNonstaticComponents()
        {
                // Loop over every tileType in stored char array of level and reload only falling platforms (so dont have to reload whole
                // level on each death)
                for (int y = 0; y < Height; ++y)
                {
                    for (int x = 0; x < Width; ++x)
                    {
                        char tileType = levelChars[x, y];
                        if (inSudo3D())
                        {
                            switch (tileType)
                            {
                                case 'T':
                                    tiles[x, y] = LoadTurretTile(x, y, false, false); break;
                                case 'M':
                                    tiles[x, y] = LoadTurretTile(x, y, false, true); break;
                                // Reload enemies
                                case 'r':
                                    tiles[x, y] = LoadEnemyTile(x, y, "MonsterR", false); break;
                                case 'l':
                                    tiles[x, y] = LoadEnemyTile(x, y, "MonsterL", false); break;
                                case 'U':
                                    tiles[x, y] = LoadEnemyTile(x, y, "MonsterU", false); break;

                                case '|':
                                    tiles[x, y] = LoadLaserTile(x, y, false, true, false, RotationDirection.None); break;
                                case '=':
                                    tiles[x, y] = LoadLaserTile(x, y, false, false, false, RotationDirection.None); break;
                                case '%':
                                    tiles[x, y] = LoadLaserTile(x, y, false, true, false, RotationDirection.Clockwise); break;
                                case 't':
                                    if (inCarRaceMode())
                                    {
                                        // Load different triggers depending on zone in level
                                        if (x > 690)
                                            tiles[x, y] = LoadLevelTile("iceFloorTile", TileCollision.Trigger);
                                        else if (x > 524)
                                            tiles[x, y] = LoadLevelTile("pavementTile", TileCollision.Trigger);
                                        else if (x > 263)
                                            tiles[x, y] = LoadLevelTile("grassTile", TileCollision.Trigger);
                                        else
                                            tiles[x, y] = LoadTile("transparentTile", TileCollision.Trigger);
                                    }
                                    else
                                        tiles[x, y] = LoadTile("transparentTile", TileCollision.Trigger); break; // was "triggerTile"
                                case '+':
                                    tiles[x, y] = LoadTile("iceTargetTile", TileCollision.Target); break;
                            }
                        }

                        else 
                        {

                        switch (tileType)
                        {
                            case '^':
                                if (levelIndex == 11)
                                    tiles[x, y] = LoadLevelTile("flyUpTile", TileCollision.Trigger); break;
                            case '4':
                                tiles[x, y] = LoadLevelTile("slopeGrassFloorTile", TileCollision.Slope); break;
                            case '-':
                                tiles[x, y] = LoadFallingPlatformTile(x, y); break;
                            case 'p':
                                tiles[x, y] = LoadTile("pistolTile", TileCollision.Gun); break;
                            case 'Z':
                                tiles[x, y] = LoadTile("powerBulletTile", TileCollision.Gun); break;
                            case 'v':
                                 tiles[x, y] = LoadLevelTile("vehicleTile", TileCollision.Vehicle); break;
                            case 'L':
                                tiles[x, y] = LoadElevatorTile(x, y); break;
                            case '[':
                                tiles[x, y] = LoadMovingSpikeTile(x, y, false, false, true); break;
                            case ']':
                                tiles[x, y] = LoadMovingSpikeTile(x, y, false, true, true); break;
                            case '_':
                                tiles[x, y] = LoadMovingPlatformTile(x, y, false, false, true); break;
                            case '|':
                                tiles[x, y] = LoadMovingPlatformTile(x, y, false, false, false); break;
                            case ':':
                                tiles[x, y] = LoadMovingPlatformTile(x, y, false, true, true); break;
                            case ';':
                                tiles[x, y] = LoadMovingPlatformTile(x, y, false, true, false); break;
                            case 't':
                                tiles[x, y] = LoadTile("transparentTile", TileCollision.Trigger); break; // was "triggerTile"
                            case 'O':
                                tiles[x, y] = LoadLevelTile("visibleTriggerTile", TileCollision.Trigger); break;
                            case 'l':
                                tiles[x, y] = LoadLaserTile(x, y, false, true, false, RotationDirection.None); break;
                            case '=':
                                tiles[x, y] = LoadLaserTile(x, y, false, false, false, RotationDirection.None); break;
                            case '*':
                                if (levelIndex == 9)
                                {
                                    tiles[x, y] = LoadTile("brickTargetTile", TileCollision.Target); break;
                                }
                                tiles[x, y] = LoadLaserTile(x, y, false, true, true, RotationDirection.None); break;
                            case '&':
                                tiles[x, y] = LoadLaserTile(x, y, false, false, true, RotationDirection.None); break;
                            case '{':
                                tiles[x, y] = LoadLaserTile(x, y, false, true, false, RotationDirection.Clockwise); break;
                            case '+':
                                tiles[x, y] = LoadTile("targetTile", TileCollision.Target); break;
                            case '!':
                                tiles[x, y] = LoadTile("timerTile", TileCollision.Timer); break;
                            case 'T':
                                tiles[x, y] = LoadTurretTile(x, y, false, false); break;
                            case 'M':
                                tiles[x, y] = LoadTurretTile(x, y, false, true); break;
                            // Reload enemies
                            case 'a':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterA", false); break;
                            case 'b':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterB", false); break;
                            case 'c':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterC", false); break;
                            case 'd':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterD", false); break;
                            case 'e':
                                tiles[x, y] = LoadEnemyTile(x, y, "Boss1", false); break;
                            case 'f':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterF", false); break;
                            case 'g':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterG", false); break;
                            case 'H':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterH", false); break;
                            case 'I':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterI", false); break;
                            case 'j':
                                tiles[x, y] = LoadEnemyTile(x, y, "Boss2", false); break;
                            case 'k':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterK", false); break;
                            case 'm':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterM", false); break;
                            case 'n':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterN", false); break;
                            case 'N':
                                tiles[x, y] = LoadEnemyTile(x, y, "Boss3", false); break;
                            case 'P':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterP", false); break;
                            case 'q':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterQ", false); break;
                            case 'x':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterX", false); break;
                            case 'y':
                                tiles[x, y] = LoadEnemyTile(x, y, "MonsterY", false); break;
                            case 'z':
                                tiles[x, y] = LoadEnemyTile(x, y, "Boss4", false); break;
                            case 'Y':
                                tiles[x, y] = LoadEnemyTile(x, y, "Boss5", false); break;
                            case '.':
                                tiles[x, y] = new Tile(null, TileCollision.Passable); break;
                            case ',':
                                if (GetTileSet() == "Street/")
                                    tiles[x, y] = LoadLevelTile("brickPlatformTile", TileCollision.Impassable);
                                else
                                    tiles[x, y] = new Tile(null, TileCollision.Wind); break;
                            default: break;
                        }
                    }
                }
            }
        }


        // Loads a tile with a random appearance.
        private Tile LoadVarietyTile(string baseName, int variationCount, TileCollision collision)
        {
            int index = random.Next(variationCount);
            return LoadTile(baseName + index, collision);
        }


        // Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            // Loading main world after finishing or quiting a level
            if (Level.IsMainWorld && LoadMainWorldFromLevel)
                player = new Player(this, LastMainLevelCheckPoint);
            // Loading main world at beginning of game
            else if (Level.IsMainWorld)
            {
                LastMainLevelCheckPoint = start;
                player = new Player(this, LastMainLevelCheckPoint);
            }
            // Loading a normal level (from main world)
            else
            {
                lastCheckPoint = start;
                player = new Player(this, lastCheckPoint);
            }

            return new Tile(null, TileCollision.Passable);
        }

        // Remembers the location of the level's exit.
        private Tile LoadExitTile(int x, int y)
        {
            if (!inCarRaceMode() && exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;

            if (inCarRaceMode())
                return LoadTile("carRaceEndFlagTile", TileCollision.Trigger);
            return LoadTile("endFlagTile", TileCollision.Passable);
        }

        // Instantiates an info tile and puts it in the level.
        private Tile LoadInfoTile(int x, int y)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            infos.Add(new Info(this, position));

            return LoadTile("infoTile", TileCollision.Info);
        }

        // Instantiates a flashing laser tile and puts it in the level.
        public Tile LoadLaserTile(int x, int y, bool createdByEvent, bool vertical, bool flashing, RotationDirection rotationDir)
        {
           
            lasers.Add(new Laser(this, x, y, createdByEvent, vertical, flashing, rotationDir));

            return new Tile(null, TileCollision.Passable);
        }

        // Removes laser from level
        public void RemoveLaser(int x, int y)
        {
            for (int i = 0; i < lasers.Count; i++)
            {
                Laser laser = lasers[i];
                if (laser.tileCoords.X == x && laser.tileCoords.Y == y)
                {
                    laser.Remove();
                    if (inCarRaceMode())
                        Tiles[x, y] = LoadLevelTile("iceFloorTile", TileCollision.Passable);

                }
            }
        }


        // Instantiates an enemy and puts him in the level.
        public Tile LoadEnemyTile(int x, int y, string spriteSet, bool createdByEvent)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(new Enemy(this, position, spriteSet, createdByEvent));

            return new Tile(null, TileCollision.Passable);
        }

        // Instantiates a gem and puts it in the level.
        // Types: 0 = normal gem tile, 1 = water gem tile, 2 = wind gem tile
        private Tile LoadGemTile(int x, int y, int type)
        {
            Point position = GetBounds(x, y).Center;
            gems.Add(new Gem(this, new Vector2(position.X, position.Y)));
            switch (type)
            {
                case 0:
                    return new Tile(null, TileCollision.Passable);
                case 1:
                    return LoadTile("waterTile", TileCollision.Water);
                case 2:
                    return new Tile(null, TileCollision.Wind);
                default:
                    return new Tile(null, TileCollision.Passable);
            }
        }

        // Instantiates a falling Platform and puts it in the level.
        private Tile LoadFallingPlatformTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            fallingPlatforms.Add(new FallingPlatform(this, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Platform);
        }

        // Instantiates a turret and puts it in the level.
        public Tile LoadTurretTile(int x, int y, bool createdByEvent, bool multiDirectionalFire)
        {
            Vector2 position = new Vector2(x*Tile.Width, y*Tile.Height);
            turrets.Add(new Turret(this, position, createdByEvent, null, multiDirectionalFire));

            return new Tile(null, TileCollision.Turret);
        }

        // Instantiates an elevator and puts it in the level.
        private Tile LoadElevatorTile(int x, int y)
        {
            Vector2 position = new Vector2(x * Tile.Width, y * Tile.Height);
            elevators.Add(new Elevator(this, position, true));

            return new Tile(null, TileCollision.Elevator);
        }

        // Instantiates a moving spike and puts it in the level.
        public Tile LoadMovingSpikeTile(int x, int y, bool createdByEvent, bool vertical, bool goRight)
        {
            //Point position = GetBounds(x, y).Center;
            Vector2 position = new Vector2(x * Tile.Width, (y + 1) * Tile.Height);
            movingSpikes.Add(new MovingSpike(this, position, createdByEvent, vertical, goRight));

            return new Tile(null, TileCollision.Passable);
        }

        // Instantiates a moving platform and puts it in the level.
        public Tile LoadMovingPlatformTile(int x, int y, bool createdByEvent, bool verticalMotion, bool horizontalOrientation)
        {
            //Point position = GetBounds(x, y).Center;
            Vector2 position = new Vector2(x * Tile.Width, (y) * Tile.Height);
            movingPlatforms.Add(new MovingPlatform(this, position, createdByEvent, verticalMotion, horizontalOrientation));

            //return LoadTile("movingPlatformTile", TileCollision.Impassable);
            return new Tile(null, TileCollision.Passable);
        }

        // Unloads the level content.
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /* Gets the collision mode of the tile at a particular location.
         * This method handles tiles outside of the levels boundries by making it
         * impossible to escape past the left or right edges, but allowing things
         * to jump beyond the top of the level and fall off the bottom.
         */
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        // Gets the bounding rectangle of a tile in world space.
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        // Width of level measured in tiles.
        public int Width
        {
            // Gets length of 1st-dimension ie num of cols
            get { return tiles.GetLength(0); }
        }

        // Height of the level measured in tiles.
        public int Height
        {
            // Gets 2nd-dimension ie num of rows
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        // Updates all objects in the world, performs collision between them,
        // and handles the time limit with scoring.
        public void Update(
            GameTime gameTime,
            KeyboardState keyboardState,
            MouseState mouseState,
            DisplayOrientation orientation)
        {
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                timeRemaining -= TimeSpan.FromSeconds(seconds);
                score += seconds * PointsPerSecond;
            }
            else
            {
                // Check if player wants to pause game
                if ((keyboardState.IsKeyDown(Keys.P) && pWasReleased))
                {
                    Game.paused = !Game.paused;
                    if (Game.paused)
                        MediaPlayer.Play(pauseMusic);
                    else
                        MediaPlayer.Play(backingTrack);
                    pWasReleased = false;
                }
               pWasReleased = keyboardState.IsKeyUp(Keys.P);

                if (!Game.paused)
                {
                    timeRemaining -= gameTime.ElapsedGameTime;
                    Player.Update(gameTime, keyboardState, mouseState, orientation);

                    UpdateDoorTimer(gameTime);
                    
                    UpdateMovingSpikes(gameTime);
                    UpdateMovingPlatforms(gameTime);
                    UpdateElevators(gameTime);
                    UpdateTurrets(gameTime);
                    UpdateBullets(gameTime);
                    UpdateFallingPlatforms(gameTime);
                    UpdateEnemies(gameTime);
                }

                UpdateGems(gameTime);
                UpdateLasers(gameTime);

                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height && levelIndex != -1)
                    OnPlayerKilled(null);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.
                if (Player.IsAlive &&
                    //Player.IsOnGround &&
                    Player.BoundingRectangle.Contains(exit))
                {
                    OnExitReached();
                }
            }

            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;
        }


        // Updates door timer if on. If timer reaches 0 handles situation separately depending on level and makes timer switch appear again.
        private void UpdateDoorTimer(GameTime gameTime)
        {
            if (doorTimerOn)
            {
                doorTimer -= gameTime.ElapsedGameTime;

                if (doorTimer.Seconds <= 0)
                {
                    // Turn timer diplay off
                    doorTimerOn = false;

                    // If door timer has been switched off reset everything that was changed
                    // Handle situation depending on level
                    switch (levelIndex)
                    {
                        case 2:
                            // Put laser beam back in place
                            for (int i = 0; i < 3; i++)
                                LoadLaserTile(32, 13 + i, false, true, false, RotationDirection.None);

                            // Put timer switch back in place
                            Tiles[59, 5] = LoadTile("timerTile", TileCollision.Timer);

                            break;

                        case 5:
                            // Put lasers back in place
                            for (int i = 0; i < 3; i++)
                            {
                                LoadLaserTile(140, 49 + i, false, true, false, RotationDirection.None);
                                LoadLaserTile(139, 59 + i, false, true, false, RotationDirection.None);
                            }

                            // Unblock ladder
                            Tiles[189, 21] = new Tile(null, TileCollision.Passable);
                            Tiles[190, 21] = new Tile(null, TileCollision.Passable);
                            Tiles[190, 22] = new Tile(null, TileCollision.Passable);

                            // Put timer switch back in place
                            Tiles[215, 13] = LoadTile("timerTile", TileCollision.Timer);

                            break;

                        default:   
                            break;
                    }

                    // Remove enemies created by timer event (for timers in every level)
                    for (int i = 0; i < Enemies.Count; i++)
                    {
                        Enemy enemy = Enemies[i];
                        if (enemy.createdByEvent)
                            enemy.Remove();
                    }

                    // Remove flashing lasers created by timer event (for timers in every level)
                    for (int i = 0; i < lasers.Count; i++)
                    {
                        Laser laser = lasers[i];
                        if (laser.createdByEvent && laser.flashing)
                            laser.Remove();
                    }
                     
                }
            }
        }



        // Animates each gem and checks if player has collected a gem.
        private void UpdateGems(GameTime gameTime)
        {
            for (int i = 0; i < gems.Count; ++i)
            {
                Gem gem = gems[i];

                gem.Update(gameTime);

                if (gem.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    gems.RemoveAt(i--);
                    OnGemCollected(gem, Player);
                }
            }
        }

        // Checks to see is player has hit a platform
        private void UpdateFallingPlatforms(GameTime gameTime)
        {
            for (int i = 0; i < fallingPlatforms.Count; ++i)
            {
                FallingPlatform platform = fallingPlatforms[i];

                platform.Update(gameTime);

                if (platform.BoundingRectangle.Intersects(Player.BoundingRectangle))
                    platform.OnContact(Player);
            }
        }

        // Updates lasers
        private void UpdateLasers(GameTime gameTime)
        {
            for (int i = 0; i < lasers.Count; ++i)
            {
                Laser laser = lasers[i];

                laser.Update(gameTime);
            }
        }

        // Updates bullet position
        private void UpdateBullets(GameTime gameTime)
        {
            for (int i = 0; i < bullets.Count; ++i)
            {
                Bullet bullet = bullets[i];
                bullet.Update(gameTime);

                // If enemy bullet hits player
                if (bullet.BoundingRectangle.Intersects(player.BoundingRectangle) && !bullet.PlayerBullet)
                {
                    // Player is killed or health is reduced
                    OnPlayerKilled(null);
                    // Bullet is removed
                    bullets.Remove(bullet);
                }
            }
        }

        // Updates turrets
        private void UpdateTurrets(GameTime gameTime)
        {
            for (int i = 0; i < turrets.Count; ++i)
            {
                Turret turret = turrets[i];
                turret.Update(gameTime);

                // Turrets are killed by player's bullets (but not there own)
                for (int j = 0; j < bullets.Count; ++j)
                {
                    Bullet bullet = bullets[j];
                    if (turret.BoundingRectangle.Intersects(bullet.BoundingRectangle) && bullet.PlayerBullet)
                        turret.OnKilled();
                }
            }
        }

        // Updates moving spikes
        private void UpdateMovingSpikes(GameTime gameTime)
        {
            for (int i = 0; i < movingSpikes.Count; ++i)
            {
                MovingSpike spike = movingSpikes[i];
                spike.Update(gameTime);

                // If moving spike hits player
                if (spike.BoundingRectangle.Intersects(player.BoundingRectangle))
                     OnPlayerKilled(null);
            }
        }


        // Sets moving spike's speed if it was created by an event
        public void SetMovingSpikeSpeed(float speed)
        {
            for (int i = 0; i < MovingSpikes.Count; i++)
            {
                MovingSpike spike = MovingSpikes[i];
                if (spike.createdByEvent)
                    spike.MySpeed = speed;
            }

        }

        // Updates moving platforms
        private void UpdateMovingPlatforms(GameTime gameTime)
        {
            for (int i = 0; i < movingPlatforms.Count; ++i)
            {
                MovingPlatform platform = movingPlatforms[i];
                platform.Update(gameTime);

                // If player is is contact with one moving platform apply collision handling (in player class)
                movingPlatformContact = CheckMovingPlatformContact();
            }
        }

        // If one of the moving platforms has contact with the player then return true
        private bool CheckMovingPlatformContact()
        {
            for (int i = 0; i < movingPlatforms.Count; ++i)
            {
                MovingPlatform platform = movingPlatforms[i];
                if (platform.playerContact)
                {
                    return true;
                }
            }
            return false;
        }


        // Updates elevators
        private void UpdateElevators(GameTime gameTime)
        {
            for (int i = 0; i < elevators.Count; ++i)
            {
                Elevator elevator = elevators[i];
               // elevator.Update(gameTime, this);

                if (elevator.BoundingRectangle.Intersects(Player.BoundingRectangle))
                    elevator.Update(gameTime, this); // OnContact(Player);
            }
        }

        // Animates each enemy and allow them to kill the player.
        private void UpdateEnemies(GameTime gameTime)
        {
            for (int i = 0; i < enemies.Count; ++i)
            {
                Enemy enemy = enemies[i];
                enemy.Update(gameTime);

                // Touching an enemy instantly kills the player
                if (enemy.BoundingRectangle.Intersects(Player.BoundingRectangle) && !enemy.died)
                {
                    OnPlayerKilled(enemy);
                }

                
                if (!player.sniperMode)
                {
                    // Enemies are killed by player bullets (but not other enemy bullets)
                    for (int j = 0; j < bullets.Count; ++j)
                    {
                        Bullet bullet = bullets[j];
                        if (enemy.BoundingRectangle.Intersects(bullet.BoundingRectangle) && bullet.PlayerBullet)
                        {
                            bullets.Remove(bullet);
                            enemy.OnKilled(bullet);

                        }
                    }
                }
            }
        }

        // Called when a gem is collected.
        private void OnGemCollected(Gem gem, Player collectedBy)
        {
            score += Gem.PointValue;

            gem.OnCollected(collectedBy);
        }

        // Called when the player is killed.
        private void OnPlayerKilled(Enemy killedBy)
        {
            Player.OnKilled(killedBy);
        }

        // Called when the player reaches the level's exit.
        public void OnExitReached()
        {
            Player.OnReachedExit();
            Game.PlaySound(exitReachedSound);
            reachedExit = true;
        }


        // Restores the player to the starting point to try the level again.
        public void StartNewLife()
        {
            // If countdown timer is on reset timer to time at the last checkpoint
            if (countDownTimerOn)
                timeRemaining = timeAtLastCheckpoint;
            
            // Remove all bullets from level
                Bullets.RemoveRange(0, Bullets.Count);
            // Remove all moving spikes from level
                movingSpikes.RemoveRange(0, movingSpikes.Count);
            // Remove all moving platforms from level
                movingPlatforms.RemoveRange(0, movingPlatforms.Count);
            // Remove all lifts from level
                elevators.RemoveRange(0, elevators.Count); 
            // Remove all turrets from level
                Turrets.RemoveRange(0, Turrets.Count);
            // Remove all lasers from level
                lasers.RemoveRange(0, lasers.Count);
            // Remove all enemies from level
                Enemies.RemoveRange(0, Enemies.Count);
            
            // Reset all level platforms, lifts, enemies, turrets and gun pickups
            this.reloadNonstaticComponents();

            // Level special cases
            Bullet.ResetTargetCounts();


            // Reset player
            if (!Level.IsMainWorld)
                Player.Reset(lastCheckPoint);
            else Player.Reset(LastMainLevelCheckPoint);
            
        }

        // Starts countdown timer
        public void startTimer()
        {
            switch (levelIndex)
            {
            // Water level
            case 2:
                doorTimer = new TimeSpan(0, 0, 15);
                doorTimerOn = true;
                break;
            // Snow level
            case 5:
                doorTimerOn = true;
                doorTimer = new TimeSpan(0, 0, 30);
                break;
            default:
                timeRemaining = new TimeSpan(0, 2, 0);
                countDownTimerOn = true;
                break;
            }
        }

        // Starts countdown timer
        public void addOneMinToTime()
        {
            timeRemaining += new TimeSpan(0, 1, 0);
        }

        #endregion


        #region Draw

        // Draw everything in the level from background to foreground. Using parallax scrolling on camera (not actually used)
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin();
            for (int i = 0; i <= EntityLayer; ++i)
                layers[i].Draw(spriteBatch, cameraPosition);
            spriteBatch.End();
            

            ScrollCamera(spriteBatch.GraphicsDevice.Viewport);
            Matrix cameraTransform = Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0.0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, cameraTransform);

            DrawTiles(spriteBatch);

            // N.B. Flashing lasers are draw/updated within own class

            foreach (Gem gem in gems)
                gem.Draw(gameTime, spriteBatch);

            foreach (Bullet bullet in bullets)
                bullet.Draw(gameTime, spriteBatch);

            foreach (Turret turret in turrets)
                turret.Draw(gameTime, spriteBatch);

            foreach (Laser laser in lasers)
                laser.Draw(gameTime, spriteBatch);

            foreach (MovingSpike spike in movingSpikes)
                spike.Draw(gameTime, spriteBatch);

            foreach (MovingPlatform platform in movingPlatforms)
                platform.Draw(gameTime, spriteBatch);

            foreach (Elevator elevator in elevators)
                elevator.Draw(gameTime, spriteBatch);

            foreach (FallingPlatform platform in fallingPlatforms)
                platform.Draw(gameTime, spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                layers[i].Draw(spriteBatch, cameraPosition);

            if (DoorTimerOn)
            {
                Viewport vp = spriteBatch.GraphicsDevice.Viewport;
                string str = doorTimer.Seconds.ToString();
                float timeWidth = Game.bigFont.MeasureString(str).X;
                spriteBatch.DrawString(Game.bigFont, str, new Vector2((vp.Width - timeWidth) / 2, vp.Height / 8), Color.Red);
            }

            spriteBatch.End();
        }

        // Draws each tile in the level.
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // To avoid any slowdown only draw the tiles currently visible to the player. (Gems + enemies are still drawn off-screen)
            // Calculate the visible range of the tiles.
            int left = (int)Math.Floor(cameraPosition.X / Tile.Width);
            int right =  left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);

            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        private void ScrollCamera(Viewport viewport)
        {
            const float ViewMargin = 0.35f;

            // Calculate the edges of the screen
            float marginWidth = viewport.Width * ViewMargin;
            // Sniper mode has smaller marin for mouse scrolling
            if (inSniperMode() && levelIndex == 9) marginWidth /= 3;

            float marginLeft;
            float marginRight;

            // In car race mode make left margin small and right margin large (so player can see far enough ahead)
            if (inCarRaceMode())
            {
                marginLeft = cameraPosition.X + marginWidth * 0.1f;
                marginRight = cameraPosition.X + viewport.Width - marginWidth * 2.0f;
            }
            // Sniper protection level
            else if (levelIndex == 10)
            {
                marginLeft = cameraPosition.X + viewport.Width * 0.4f;
                marginRight = cameraPosition.X + viewport.Width - (viewport.Width * 0.4f);
            }
            else
            {
                marginLeft = cameraPosition.X + marginWidth;
                marginRight = cameraPosition.X + viewport.Width - marginWidth;
            }

            float marginHeight = viewport.Height * ViewMargin;
            float marginTop = cameraPosition.Y + marginHeight;
            float marginBottom = cameraPosition.Y + viewport.Height - marginHeight;

            // Calculate how far to scroll when the player is near the edges of the screen
            Vector2 cameraMovement = new Vector2(0, 0);


            // If in sniper mode move camera when crosshair is moved to edge of screen
            if (player.sniperMode && levelIndex == 9)
            {
                if (cameraPosition.X + Player.mousePos.X < marginLeft)
                    cameraMovement.X = (cameraPosition.X + Player.mousePos.X - marginLeft)* 0.25f;
                else if (cameraPosition.X + Player.mousePos.X > marginRight)
                    cameraMovement.X = (cameraPosition.X + Player.mousePos.X - marginRight) * 0.25f;

                if (Player.mousePos.Y < marginTop)
                    cameraMovement.Y = Player.mousePos.Y - marginTop;
                else if (Player.mousePos.Y > marginBottom)
                    cameraMovement.Y = Player.mousePos.Y - marginBottom;
                cameraMovement.Y = 0;

                if (player.mousePos.X < 0 || player.mousePos.X > viewport.Width || player.mousePos.Y < 0 || player.mousePos.Y > viewport.Height)
                    cameraMovement = new Vector2(0, 0);
            }
                
            else
            {
                // NB In car race mode we have a small left margin as player is always moving to the right
                if (Player.Position.X < marginLeft)
                    cameraMovement.X = Player.Position.X - marginLeft;
                else if (Player.Position.X > marginRight)
                    cameraMovement.X = Player.Position.X - marginRight;

                if (Player.Position.Y < marginTop)
                    cameraMovement.Y = Player.Position.Y - marginTop;
                else if (Player.Position.Y > marginBottom)
                    cameraMovement.Y = Player.Position.Y - marginBottom;
            }

            // Update the camera position, but prevent scrolling off the ends of the level
            float maxXCameraPosition = Tile.Width * Width - viewport.Width;
            float maxYCameraPosition = Tile.Height * Height - viewport.Height;
            cameraPosition.X = MathHelper.Clamp(cameraPosition.X + cameraMovement.X, 0.0f, maxXCameraPosition);
            cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y + cameraMovement.Y, 0.0f, maxYCameraPosition);
        }
    }
}

        #endregion