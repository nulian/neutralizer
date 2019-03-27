using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.IO;

namespace Neutralizer
{
    /// <summary>
    /// A uniform grid of tiles with collections of gems and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    class Level : IDisposable
    {
        // Physical structure of the level.
        private Tile[,] tiles;
        private Texture2D background;
        // The layer which entities are drawn on top of.
        private const int EntityLayer = 2;
        // Entities in the level.
        public Player Player
        {
            get { return player; }
        }
        Player player;

        Game game;

        public Game Game
        {
            get { return game; }
            set { game = value; }
        }
        private List<EmptyTile> emptyTiles = new List<EmptyTile>();
        private List<ChargeTile> charge = new List<ChargeTile>();
        private List<Bullet> bullets = new List<Bullet>();
        private List<FallingTile> fallingTiles = new List<FallingTile>();
        public List<Bullet> Bullet
        {
            get { return bullets; }
            set { bullets = value; }
        }
        // Key locations in the level.        
        private Vector2 start;
        private Point exit = InvalidPosition;
        private static readonly Point InvalidPosition = new Point(-1, -1);

        // Level game state.
        private Random random = new Random(354668); // Arbitrary, but constant seed

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        private ExitTile exitTile;
        // Level content.        
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        

        private SoundEffect exitReachedSound;

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        public Level(Game game, IServiceProvider serviceProvider, string path)
        {
            // Create a new content manager to load content used just by this level.
            content = new ContentManager(serviceProvider, "Content");
            this.Game = game;


            LoadTiles(path);

            
           
           background = Content.Load<Texture2D>("Backgrounds/Background");
           
            // Load sounds.
            exitReachedSound = Content.Load<SoundEffect>("Sounds/ExitReached");
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        private void LoadTiles(string path)
        {
            // Load the level and ensure all of the lines are the same length.
            int width;
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(path))
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

            // Loop over every tile position,
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
                {
                    // to load each tile.
                    char tileType = lines[y][x];
                    tiles[x, y] = LoadTile(tileType, x, y);
                }
            }

            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exit == InvalidPosition)
                throw new NotSupportedException("A level must have an exit.");

        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);
                
                // Floating platform
               // case '-':
               //     return LoadTile("Platform", TileCollision.Platform);

                // Platform block
                case '~':
                    return LoadLavaTile("Spikes", TileCollision.Passable);

                // Passable block
                case ':':
                    return LoadEmptyTile(x, y);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y);
                case '+':
                    return LoadPositiveTile(x, y);
                case '-':
                    return LoadNegativeTile(x, y);

                // Impassable block
                case '#':
                    return LoadStandardTile( TileCollision.Impassable);
                case 'F':
                    return LoadFallingTile(x,y);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadLavaTile(string name, TileCollision collision)
        {
            return new Tile(Content.Load<Texture2D>("Tiles/" + name), collision);
        }

        
        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadStandardTile(TileCollision collision)
        {
            //int index = random.Next(variationCount);
            return LoadTile("VloerBlok", collision);
        }

        private Tile LoadEmptyTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            emptyTiles.Add(new EmptyTile(this, new Vector2(position.X, position.Y), new Point(x, y)));
            return new Tile(null, TileCollision.EmptyTile);
        }


        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            if (Player != null)
                throw new NotSupportedException("A level may only have one starting point.");

            start = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, start);

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            if (exit != InvalidPosition)
                throw new NotSupportedException("A level may only have one exit.");

            exit = GetBounds(x, y).Center;
            exitTile = new ExitTile(this,new Vector2(exit.X, exit.Y));
            exitTile.ExitPoint = exit;
            return new Tile(null, TileCollision.ExitTile);
        }

        private Tile LoadPositiveTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            charge.Add(new ChargeTile(this, new Vector2(position.X, position.Y),new Vector2(x,y),true));
            return new Tile(null, TileCollision.PositiveTile);
        }

        private Tile LoadNegativeTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            charge.Add(new ChargeTile(this, new Vector2(position.X, position.Y), new Vector2(x, y), false));
            return new Tile(null, TileCollision.NegativeTile);

        }

        private Tile LoadFallingTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            fallingTiles.Add(new FallingTile(this, new Vector2(position.X, position.Y), new Vector2(x, y)));
            return new Tile(null, TileCollision.DropTile);

        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
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

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if( ReachedExit)
            {
                
            }
            else
            {
                Player.Update(gameTime);

                UpdateBullets(gameTime);
                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled();

                exitTile.Update(gameTime);
                exit = exitTile.ExitPoint;
                UpdateCharges(gameTime);
                UpdateEmptyTiles(gameTime);
                UpdateFallingTiles(gameTime);
                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the gems.

                if (Player.IsAlive &&
                    Player.IsOnGround &&
                    (Player.BoundingRectangle.Contains(exit)))
                {
                    OnExitReached();
                }
            }

        }

 
        private void UpdateBullets(GameTime gameTime)
        {
            float posX = 0;
            int tileX = 0;
            int tileY = 0;
            bool droppedCharge = false;
            for (int i = 0; i < bullets.Count; i++ )
            {
                Bullet bult = bullets[i];
                if (!bult.Update(gameTime))
                {
                    bullets.Remove(bult);
                    break;
                }
                posX = bult.BoundingCircle.Center.X + bult.BoundingCircle.Radius / 2;
                tileX = (int)Math.Floor(posX / Tile.Width);
                tileY = (int)Math.Floor((bult.BoundingCircle.Center.Y + bult.BoundingCircle.Radius / 2) / Tile.Height);
                foreach (ChargeTile charg in charge)
                {

                    if (charg.TilePosition == new Vector2(tileX, tileY))
                    {
                        int x = (int)charg.TilePosition.X;
                        int y = (int)charg.TilePosition.Y;
                       
                        if (bult.Positive && !charg.Positive)
                        {
                            tiles[(int)charg.TilePosition.X, (int)charg.TilePosition.Y] = new Tile(null, TileCollision.Passable);
                            bool reachedFloor = false;
                            while (!reachedFloor)
                            {
                                int newY = y;
                                while (tiles[x, newY - 1].Collision == TileCollision.PositiveTile || tiles[x, newY - 1].Collision == TileCollision.NegativeTile || tiles[x, newY - 1].Collision == TileCollision.DropTile || tiles[x, newY - 1].Collision == TileCollision.EmptyTile || tiles[x, newY -1].Collision == TileCollision.ExitTile)
                                {
                                  //  if (tiles[x, newY - 1].Collision == TileCollision.EmptyTile)
                                  //      tiles[x, newY - 1] = LoadNegativeTile(x, newY - 1);
                                    tiles[x, newY] = tiles[x, newY - 1];
                                    if (tiles[x, newY].Collision == TileCollision.PositiveTile || tiles[x, newY].Collision == TileCollision.NegativeTile)
                                    {
                                        foreach (ChargeTile ch in charge)
                                            if (ch.TilePosition == new Vector2(x, newY - 1))
                                            {
                                                Vector2 pos = ch.Position;
                                                Vector2 newPos = ch.NewPosition;
                                                if (newPos != pos)
                                                    pos = newPos;
                                                pos.Y += ch.Size.Y;
                                                ch.NewPosition = pos;
                                                Vector2 tilePos = ch.TilePosition;
                                                tilePos.Y = newY;
                                                ch.TilePosition = tilePos;
                                            }
                                    }
                                    else if (tiles[x, newY].Collision == TileCollision.DropTile)
                                    {
                                        foreach (FallingTile ch in fallingTiles)
                                            if (ch.TilePosition == new Vector2(x, newY - 1))
                                            {
                                                Vector2 pos = ch.Position;
                                                Vector2 newPos = ch.NewPosition;
                                                if (newPos != pos)
                                                    pos = newPos;
                                                pos.Y += ch.Size.Y;
                                                ch.NewPosition = pos;
                                                Vector2 tilePos = ch.TilePosition;
                                                tilePos.Y = newY;
                                                ch.TilePosition = tilePos;
                                            }
                                    } else if (tiles[x, newY].Collision == TileCollision.ExitTile)
                                    {
                                        Vector2 pos = exitTile.Position;
                                        Vector2 newPos = exitTile.NewPosition;
                                        if (newPos != pos)
                                            pos = newPos;
                                        pos.Y += 48;
                                        exitTile.NewPosition = pos;
                                        exitTile.ExitPoint = GetBounds(x, newY).Center;
                                    } else if (tiles[x, newY].Collision == TileCollision.EmptyTile)
                                    {
                                        foreach (EmptyTile emptyTile in emptyTiles)
                                            if (emptyTile.TilePosition == new Point(x, newY - 1))
                                            {
                                                Vector2 pos = emptyTile.Position;
                                                Vector2 newPos = emptyTile.NewPosition;
                                                if (newPos != pos)
                                                    pos = newPos;
                                                pos.Y += emptyTile.Size.Y;
                                                emptyTile.NewPosition = pos;
                                                Point tilePos = emptyTile.TilePosition;
                                                tilePos.Y = newY;
                                                emptyTile.TilePosition = tilePos;
                                                droppedCharge = true;
                                            }
                                    }
                                    tiles[x, newY - 1] = new Tile(null, TileCollision.Passable);
                                    newY--;
                                }
                                if (y == 14)
                                    reachedFloor = true;
                                else if (tiles[x, y + 1].Collision == TileCollision.Passable)
                                {
                                    y++;

                                }
                                else
                                {
                                    reachedFloor = true;
                                }

                            }

                            charge.Remove(charg);
                        }
                        else if (!bult.Positive && charg.Positive)
                        {
                            tiles[(int)charg.TilePosition.X, (int)charg.TilePosition.Y] = new Tile(null, TileCollision.Passable);
                            bool reachedFloor = false;
                            while (!reachedFloor)
                            {
                                int newY = y;
                                while (tiles[x, newY - 1].Collision == TileCollision.PositiveTile || tiles[x, newY - 1].Collision == TileCollision.NegativeTile || tiles[x, newY - 1].Collision == TileCollision.DropTile || tiles[x, newY - 1].Collision == TileCollision.EmptyTile || tiles[x, newY -1].Collision == TileCollision.ExitTile)
                                {
                                    //if (tiles[x, newY - 1].Collision == TileCollision.EmptyTile)
                                    //    tiles[x, newY - 1] = LoadPositiveTile(x, newY - 1);
                                    tiles[x, newY] = tiles[x, newY - 1];
                                    if (tiles[x, newY].Collision == TileCollision.PositiveTile || tiles[x, newY].Collision == TileCollision.NegativeTile)
                                    {
                                        foreach (ChargeTile ch in charge)
                                            if (ch.TilePosition == new Vector2(x, newY - 1))
                                            {
                                                Vector2 pos = ch.Position;
                                                Vector2 newPos = ch.NewPosition;
                                                if (newPos != pos)
                                                    pos = newPos;
                                                pos.Y += ch.Size.Y;
                                                ch.NewPosition = pos;
                                                Vector2 tilePos = ch.TilePosition;
                                                tilePos.Y = newY;
                                                ch.TilePosition = tilePos;
                                            }
                                    }
                                    else if (tiles[x, newY].Collision == TileCollision.DropTile)
                                    {
                                        foreach (FallingTile ch in fallingTiles)
                                            if (ch.TilePosition == new Vector2(x, newY - 1))
                                            {
                                                Vector2 pos = ch.Position;
                                                Vector2 newPos = ch.NewPosition;
                                                if (newPos != pos)
                                                    pos = newPos;
                                                pos.Y += ch.Size.Y;
                                                ch.NewPosition = pos;
                                                Vector2 tilePos = ch.TilePosition;
                                                tilePos.Y = newY;
                                                ch.TilePosition = tilePos;
                                            }
                                    } else if (tiles[x, newY].Collision == TileCollision.ExitTile)
                                    {
                                        Vector2 pos = exitTile.Position;
                                        Vector2 newPos = exitTile.NewPosition;
                                        if (newPos != pos)
                                            pos = newPos;
                                        pos.Y += 48;
                                        exitTile.NewPosition = pos;
                                        exitTile.ExitPoint = GetBounds(x, newY).Center;
                                    }
                                    else if (tiles[x, newY].Collision == TileCollision.EmptyTile)
                                    {
                                        foreach (EmptyTile emptyTile in emptyTiles)
                                            if (emptyTile.TilePosition == new Point(x, newY - 1))
                                            {
                                                Vector2 pos = emptyTile.Position;
                                                Vector2 newPos = emptyTile.NewPosition;
                                                if (newPos != pos)
                                                    pos = newPos;
                                                pos.Y += emptyTile.Size.Y;
                                                emptyTile.NewPosition = pos;
                                                Point tilePos = emptyTile.TilePosition;
                                                tilePos.Y = newY;
                                                emptyTile.TilePosition = tilePos;
                                                droppedCharge = true;
                                            }

                                    }

                                    tiles[x, newY - 1] = new Tile(null, TileCollision.Passable);
                                    newY--;
                                }
                                if (y == 14)
                                    reachedFloor = true;
                                else if (tiles[x, y + 1].Collision == TileCollision.Passable)
                                    y++;
                                else
                                    reachedFloor = true;

                            }
                            charge.Remove(charg);
                        }


                        bullets.Remove(bult);
                        break;
                    }

                }

                if (GetCollision(tileX, tileY) == TileCollision.Impassable || GetCollision(tileX, tileY) == TileCollision.DropTile)
                {
                    bullets.Remove(bult);
                    break;
                }
                else if (GetCollision(tileX, tileY) == TileCollision.EmptyTile)
                {
                    if (bult.Positive && !droppedCharge)
                    {
                        tiles[tileX, tileY] = LoadPositiveTile(tileX, tileY);
                        foreach (EmptyTile emptyTile in emptyTiles)
                        {
                            if (emptyTile.TilePosition == new Point(tileX, tileY))
                            {
                                emptyTiles.Remove(emptyTile);
                                break;
                            }
                        }
                    }
                    else if (!droppedCharge)
                    {
                        tiles[tileX, tileY] = LoadNegativeTile(tileX, tileY);
                        foreach (EmptyTile emptyTile in emptyTiles)
                        {
                            if (emptyTile.TilePosition == new Point(tileX, tileY))
                            {
                                emptyTiles.Remove(emptyTile);
                                break;
                            }
                        }
                    }else
                    {
                        droppedCharge = false;
                    }
                    bullets.Remove(bult);
                    break;
                }
            }
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached()
        {
            Player.OnReachedExit();
            exitReachedSound.Play();
            reachedExit = true;
        }


        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateCharges(GameTime gameTime)
        {
            foreach (ChargeTile chargeTile in charge)
            {
                chargeTile.Update(gameTime);

            }
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEmptyTiles(GameTime gameTime)
        {
            foreach (EmptyTile emptyTile in emptyTiles)
            {
                emptyTile.Update(gameTime);

            }
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateFallingTiles(GameTime gameTime)
        {
            foreach (FallingTile fallingTile in fallingTiles)
                fallingTile.Update(gameTime);
        }

        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerKilled()
        {
            Player.OnKilled();
        }



        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            Player.Reset(start);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            
                spriteBatch.Draw(background, Vector2.Zero, Color.White);
                exitTile.Draw(gameTime, spriteBatch);
            DrawTiles(spriteBatch);

            

            foreach (Bullet bullet in bullets)
                bullet.Draw(gameTime, spriteBatch);

            foreach (ChargeTile chargeTile in charge)
                chargeTile.Draw(gameTime, spriteBatch);

            foreach (EmptyTile emptyTile in emptyTiles)
                emptyTile.Draw(gameTime, spriteBatch);

            foreach (FallingTile fallingTile in fallingTiles)
                fallingTile.Draw(gameTime, spriteBatch);
            

            Player.Draw(gameTime, spriteBatch);

        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // For each tile position
            for (int y = 0; y < Height; ++y)
            {
                for (int x = 0; x < Width; ++x)
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

        #endregion
    }
}
