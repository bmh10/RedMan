#region File Description
//-----------------------------------------------------------------------------
// Circle.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using Microsoft.Xna.Framework;

namespace RedMan
{
    /*
     * Represents a 2D circle.
     */
    struct Circle
    {
        // Center position of the circle.
        public Vector2 Center;
        public float Radius;

        // Constructs a new circle.
        public Circle(Vector2 position, float radius)
        {
            Center = position;
            Radius = radius;
        }

        // Determines if a circle intersects a rectangle.
        // <returns>True if the circle and rectangle overlap. False otherwise.</returns>
        public bool Intersects(Rectangle rectangle)
        {
            Vector2 v = new Vector2(MathHelper.Clamp(Center.X, rectangle.Left, rectangle.Right),
                                    MathHelper.Clamp(Center.Y, rectangle.Top, rectangle.Bottom));

            Vector2 direction = Center - v;
            float distanceSquared = direction.LengthSquared();

            return ((distanceSquared > 0) && (distanceSquared < Radius * Radius));
        }
    }
}
