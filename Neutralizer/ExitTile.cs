using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Neutralizer
{
    class ExitTile
    {
        Texture2D doorTexture;
        Vector2 position;

        public Vector2 Position
        {
            get { return position; }
        }

        public Vector2 Size
        {
            get
            {
                return new Vector2(doorTexture.Width, doorTexture.Height);
            }
        }
        private Point exitPoint;
        public Point ExitPoint
        {
            get { return exitPoint; }
            set { exitPoint = value; }
        }
        private Vector2 newPosition;

        public Vector2 NewPosition
        {
            get { return newPosition; }
            set { newPosition = value; }
        }

        public ExitTile(Level level, Vector2 position)
        {
            doorTexture = level.Content.Load<Texture2D>("Tiles/Deur");
            position.Y -= doorTexture.Height - 30;
            position.X -= doorTexture.Width / 2;
            this.position = position;
            newPosition = position;
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
            spriteBatch.Draw(doorTexture, position, Color.White);

        }
    }

 
}
