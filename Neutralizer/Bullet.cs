using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Neutralizer
{
    class Bullet
    {
        Texture2D positiveBulletTexture;
        Texture2D negativeBulletTexture;
        Vector2 position;
        private bool positive;

        public bool Positive
        {
            get { return positive; }
            set { positive = value; }
        }

        public Level Level
        {
            get { return level; }
        }
        Level level;
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }

        Vector2 velocity;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }

        SpriteEffects flip;

        /// <summary>
        /// Gets a circle which bounds this gem in world space.
        /// </summary>
        public Circle BoundingCircle
        {
            get
            {
                return new Circle(Position, Tile.Width / 3.0f);
            }
        }

        public Bullet(Level level, Player player, bool positive, bool isDucking)
        {
            Position = player.Position;
            if (!isDucking)
                this.position.Y = Position.Y - 84;
            else
                this.position.Y = Position.Y - 42;
            Velocity = player.Velocity;
            this.level = level;
            flip = player.Flip;
            this.positive = positive;
            LoadContent();
        }

        public void LoadContent()
        {
            positiveBulletTexture = level.Content.Load<Texture2D>("Bullet/plus-bullet");
            negativeBulletTexture = level.Content.Load<Texture2D>("Bullet/min-bullet");
        }

        public bool Update(GameTime gameTime)
        {
            if (Velocity.X == 0)
            {
                if (flip == SpriteEffects.FlipHorizontally)
                    position.X += 10.0f;
                else
                    position.X -= 10.0f;
            }
            else if (Velocity.X > 0)
                position.X += 10.0f;
            else if (Velocity.X < 0)
                position.X -= 10.0f;
            if (position.X > 1280 || position.X < 0)
                return false;
            return true;
        }


        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
           
            if (positive)
                spriteBatch.Draw(positiveBulletTexture, Position, Color.White);
            else
                spriteBatch.Draw(negativeBulletTexture, Position, Color.White);

        }
    }
}
