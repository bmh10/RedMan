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
        // NB Platform is passable from below but impassable from above
        enum TileCollision { Passable, Impassable, Platform, Ladder, Death, Slope, Water, SwingPole, Bounce,
            Rconveyor, Lconveyor, Turret, Gun, Elevator, Save, Stopper, Wind, Timer, Trigger, PlusOne, Target, Wings,
            Info, Collectable, Vehicle, Door, Monkeybar, Ramp, Oil }

    /*
     * Stores appearance and collision behaviour of a tile
     */
    struct Tile
    {
        public Texture2D Texture;
        public TileCollision Collision;

        public const int Width = 32, Height = 32;
        public static readonly Vector2 Size = new Vector2(Width, Height);

        public Tile(Texture2D texture, TileCollision collision)
        {
            Texture = texture;
            Collision = collision;
        }
    }
}
