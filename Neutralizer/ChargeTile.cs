        using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Neutralizer
{
    class ChargeTile
    {

        private Texture2D positiveTexture;
        private Texture2D negativeTexture;
        private Vector2 origin;


        public const int PointValue = 30;
        public readonly Color Color = Color.Yellow;
        private bool positive = false;
        private Vector2 tilePosition;
        private Vector2 newPosition;

        public Vector2 NewPosition
        {
            get { return newPosition; }
            set { newPosition = value; }
        }
        public Vector2 TilePosition
        {
            get { return tilePosition; }
            set { tilePosition = value; }
        }

        public bool Positive
        {
            get { return positive; }
            set { positive = value; }
        }

        // The gem is animated from a base position along the Y axis.
        private Vector2 basePosition;

        public Vector2 Size
        {
            get {
                return new Vector2(positiveTexture.Width, positiveTexture.Height);
            }
        }
        public Level Level
        {
            get { return level; }
        }
        Level level;
        
        /// <summary>
        /// Gets the current position of this gem in world space.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return basePosition;
            }
        }

        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X);
                int top = (int)Math.Round(Position.Y);

                return new Rectangle(left, top - positiveTexture.Height/2, positiveTexture.Width, positiveTexture.Height);
            }
        }

        /// <summary>
        /// Constructs a new gem.
        /// </summary>
        public ChargeTile(Level level, Vector2 position, Vector2 tilePosition, bool positive)
        {
            this.level = level;
            this.basePosition = position;
            this.positive = positive;
            this.tilePosition = tilePosition;
            newPosition = position;
            LoadContent();
        }

        /// <summary>
        /// Loads the gem texture and collected sound.
        /// </summary>
        public void LoadContent()
        {
            positiveTexture = Level.Content.Load<Texture2D>("Tiles/PlusBlok");
            negativeTexture = Level.Content.Load<Texture2D>("Tiles/MinBlok");
            origin = new Vector2(positiveTexture.Width / 2.0f, positiveTexture.Height / 2.0f);
        }

        /// <summary>
        /// Moves the item to the new position.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (Position != NewPosition)
            {
                float newY = newPosition.Y - Position.Y;
                basePosition.Y += 8;
                
            }
        }


        /// <summary>
        /// Draws a gem in the appropriate color.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Texture2D texture;
            if (positive)
                texture = positiveTexture;
            else
                texture = negativeTexture;
            spriteBatch.Draw(texture, Position, null, Color.White, 0.0f, origin, 1.0f, SpriteEffects.None, 0.0f);
        }
    }
}

    

