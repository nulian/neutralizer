using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Neutralizer
{
    class EmptyTile
    {
        Texture2D texture;
        Vector2 position;
        private Vector2 origin;
        public Vector2 Position
        {
            get { return position; }
        }

        public Vector2 Size
        {
            get
            {
                return new Vector2(texture.Width, texture.Height);
            }
        }
        private Point tilePosition;
        public Point TilePosition
        {
            get { return tilePosition; }
            set { tilePosition = value; }
        }

        
        private Vector2 newPosition;

        public Vector2 NewPosition
        {
            get { return newPosition; }
            set { newPosition = value; }
        }

        public EmptyTile(Level level, Vector2 position, Point tilePosition)
        {
            texture = level.Content.Load<Texture2D>("Tiles/NormaalBlok");
            this.position = position;
            newPosition = position;
            this.tilePosition = tilePosition;
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
        }

        /// <summary>
        /// Moves the item to the new position.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (Position != NewPosition)
            {
                float newY = newPosition.Y - Position.Y;
                position.Y += 8;

            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture, Position, null, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);

        }
    }

 
}
