using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;


namespace RedMan
{
    class Layer
    {

        public Texture2D[] Textures { get; private set; }
        public float ScrollRate { get; private set; }

        public Layer(ContentManager content, string basePath, float scrollRate)
        {
            // Assumes each layer only has 3 segments.
            Textures = new Texture2D[3];
            for (int i = 0; i < 3; ++i)
                Textures[i] = content.Load<Texture2D>(basePath + "_" + i);

            ScrollRate = scrollRate;
        }

            
      public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition)
        {
        // Assume each segment is the same width and height.
         int segmentWidth = Textures[0].Width;
         //int segmentHeight = Textures[0].Height;

         // Calculate which segments to draw and how much to offset them.
         float x = cameraPosition.X * ScrollRate;
         int leftSegment = (int)Math.Floor(x / segmentWidth);
         int rightSegment = leftSegment + 1;
         x = (x / segmentWidth - leftSegment) * -segmentWidth;

         //float y = cameraPosition.Y * ScrollRate;
         //int topSegment = (int)Math.Floor(x / segmentHeight);
         //int bottomSegment = leftSegment + 1;
         //y = (y / segmentHeight - topSegment) * -segmentHeight;

         spriteBatch.Draw(Textures[leftSegment % Textures.Length], new Vector2(x, 0.0f), Color.White);
         spriteBatch.Draw(Textures[rightSegment % Textures.Length], new Vector2(x + segmentWidth, 0.0f), Color.White);

         //spriteBatch.Draw(Textures[topSegment % Textures.Length], new Vector2(x, 0.0f), Color.White);
         //spriteBatch.Draw(Textures[rightSegment % Textures.Length], new Vector2(x + segmentWidth, 0.0f), Color.White);
        }
    }
}
