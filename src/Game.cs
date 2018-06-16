using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Nibbles
{
    class Game
    {
        private readonly int DefaultYPos;
        private readonly int DefaultXPos;
        private readonly int DefaultHealth;
        private readonly int DefaultDelay;

        private readonly char SnakeSymbol;
        private readonly char WallSymbol;

        private readonly int ScoreToPassLevel;
        private readonly int DelaySubtrahend;
        private readonly int MinDelay;

        private char[,] Map;
        private Level CurrentLevel;

        private int Size;
        private int Health;
        private int Delay;

        private int YPos;
        private int XPos;

        // collectable (fruit)
        private int NumberYPos;
        private int NumberXPos;
        private int Number;

        private List<int> YTails;
        private List<int> XTails;

        private readonly Random Random;
        private readonly object SyncLock;

        private Directions MovingDirection;

        private int ReturnCode;
        // return codes
        // -------------------------------------------
        // -1 - main menu (default)
        //  0 - success (nothing interesting happened)
        //  1 - next level
        //  2 - HP lost
        //  3 - game over
        // -------------------------------------------

        public Game(int defaultDelay, int delaySubtrahend, int minDelay, int yMax, int xMax, int yStarting, int xStarting, char snakeSymbol, char wallSymbol, int startingHealth, int scoreToPassLevel)
        {
            if (yStarting > yMax || xStarting > xMax)
            {
                throw new Exception("Error: Starting position is out of range.");
            }
            else if (xStarting == 0 || xStarting == xMax - 1 || yStarting == 0 || yStarting == yMax - 1)
            {
                throw new Exception("Error: Starting position cannot be on map borders.");
            }
            else if (startingHealth <= 0)
            {
                throw new Exception("Error: Starting health cannot be less than 1.");
            }
            else if (defaultDelay <= MinDelay)
            {
                throw new Exception("Error: DefaultDelay must be greater than " + MinDelay + ".");
            }
            else if (scoreToPassLevel <= 1)
            {
                throw new Exception("Error: ScoreToPassLevel must be greater than 1.");
            }
            else if (delaySubtrahend >= defaultDelay)
            {
                throw new Exception("Error: DelaySubtrahend cannot be greater than defaultDelay.");
            }
            else if (minDelay <= 0)
            {
                throw new Exception("Error: MinDelay must be greater than 0.");
            }
            else
            {
                Map = new char[yMax, xMax];
                Size = 1;
                Health = startingHealth;

                DefaultYPos = yStarting;
                DefaultXPos = xStarting;
                DefaultHealth = startingHealth;
                DefaultDelay = defaultDelay;
                WallSymbol = wallSymbol;
                SnakeSymbol = snakeSymbol;
                ScoreToPassLevel = scoreToPassLevel;
                DelaySubtrahend = delaySubtrahend;
                MinDelay = minDelay;
                MovingDirection = Directions.none;
                ReturnCode = -1;// default

                Random = new Random();
                SyncLock = new object();

                YPos = yStarting;
                XPos = xStarting;
                FillMap(wallSymbol);

                YTails = new List<int>();
                XTails = new List<int>();

                CurrentLevel = Level.first;
                Delay = defaultDelay;

                PlaceNewNumber();
            }
        }

        public void Run()
        {
            Console.Clear();
            Console.CursorVisible = false;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("################################################################");
            Console.WriteLine("#                                                              #");
            Console.WriteLine("#      ####    ##  ##  ######   ######   ##     #####   #####  #");
            Console.WriteLine("#     ## ##   ##  ##  ##   ##  ##   ##  ##     ##     ##       #");
            Console.WriteLine("#    ##  ##  ##  ##  ## ###   ## ###   ##     #####    ##      #");
            Console.WriteLine("#   ##   ## ##  ##  ##   ##  ##   ##  ##     ##         ##     #");
            Console.WriteLine("#  ##    ####  ##  ######   ######   #####  #####  #####       #");
            Console.WriteLine("#                                                              #");
            
            if (ReturnCode == -1)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("################################################################");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                             Controls:                        #");
                Console.WriteLine("#                            Arrow keys                        #");
                Console.WriteLine("#                                or                            #");
                Console.WriteLine("#                               WASD                           #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                          Enter to start                      #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("################################################################");
            }
            else if (ReturnCode == 2)
            {
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("################################################################");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                             1 HP lost!                       #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                          Enter to respawn                    #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("################################################################");
            }
            else if (ReturnCode == 1)
            {
                Console.BackgroundColor = ConsoleColor.DarkRed;
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("################################################################");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                             GAME OVER!                       #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                    Enter to return to game menu              #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("################################################################");
            }
            else if (ReturnCode == 3)
            {
                Console.BackgroundColor = ConsoleColor.DarkGreen;
                Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine("################################################################");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                            NEXT LEVEL!                       #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("#                         Enter to continue                    #");
                Console.WriteLine("#                                                              #");
                Console.WriteLine("################################################################");
            }
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine("#                                                              #");
            Console.WriteLine("#                   https://github.com/Rendojack               #");
            Console.WriteLine("#                                                              #");
            Console.WriteLine("################################################################");

            do { }
            while (!Console.KeyAvailable && Console.ReadKey(true).Key != ConsoleKey.Enter);

            if (ReturnCode == 1)// game over
            {
                Reset();
                ReturnCode = -1;// default returnCode
                Run();
                return;
            }

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Update();

            do { }
            while (!Console.KeyAvailable);
            CheckPlayerInput(Console.ReadKey(true).Key);

            do
            {
                if (Console.KeyAvailable)
                    CheckPlayerInput(Console.ReadKey(true).Key);
                else
                {
                    ReturnCode = MoveSnake();
                }
            }
            while (ReturnCode == 0 || ReturnCode == -1);// while success or default
            MovingDirection = Directions.none;
            Run();
        }

        private void CheckPlayerInput(ConsoleKey keyPressed)
        {
            if(keyPressed == ConsoleKey.UpArrow || keyPressed == ConsoleKey.W)
            {
                if(MovingDirection != Directions.down)// prevent colliding itself
                    MovingDirection = Directions.up;
            }
            else if(keyPressed == ConsoleKey.DownArrow || keyPressed == ConsoleKey.S)
            {
                if(MovingDirection != Directions.up)
                    MovingDirection = Directions.down;
            }
            else if(keyPressed == ConsoleKey.RightArrow || keyPressed == ConsoleKey.D)
            {
                if(MovingDirection != Directions.left)
                    MovingDirection = Directions.right;
            }
            else if(keyPressed == ConsoleKey.LeftArrow || keyPressed == ConsoleKey.A)
            {
                if(MovingDirection != Directions.right)
                    MovingDirection = Directions.left;
            }
            else if(keyPressed == ConsoleKey.Escape)
            {
                Environment.Exit(1);
            }
        }

        private void Reset()
        {
            Size = 1;
            YPos = DefaultYPos;
            XPos = DefaultXPos;
            Health = DefaultHealth;
            CurrentLevel = Level.first;
            MovingDirection = Directions.none;
            Delay = DefaultDelay;

            YTails.Clear();
            XTails.Clear();

            FillMap(WallSymbol);
            PlaceNewNumber();
            Update();
        }

        private int MoveSnake()
        {
            try
            {
                if (Size > 1)
                {
                    YTails.Add(YPos);
                    XTails.Add(XPos);

                    if (YTails.Count >= Size && XTails.Count >= Size)
                    {
                        Map[YTails[0], XTails[0]] = ' ';
                        YTails.RemoveAt(0);
                        XTails.RemoveAt(0);
                    }
                }
                else if (Size == 1)
                {
                    Map[YPos, XPos] = ' ';
                }

                switch (MovingDirection)
                {
                    case Directions.right:
                        if (Map[YPos, XPos + 1] == ' ' || (YPos == NumberYPos && XPos + 1 == NumberXPos))
                        {
                            Map[YPos, XPos + 1] = SnakeSymbol;
                            XPos++;
                            break;
                        }
                        else
                            throw new Exception();
                    case Directions.left:
                        if (Map[YPos, XPos - 1] == ' ' || (YPos == NumberYPos && XPos - 1 == NumberXPos))
                        {
                            Map[YPos, XPos - 1] = SnakeSymbol;
                            XPos--;
                            break;
                        }
                        else
                            throw new Exception();
                    case Directions.up:
                        if (Map[YPos - 1, XPos] == ' ' || (YPos - 1 == NumberYPos && XPos == NumberXPos))
                        {
                            Map[YPos - 1, XPos] = SnakeSymbol;
                            YPos--;
                            break;
                        }
                        else
                            throw new Exception();
                    case Directions.down:
                        if (Map[YPos + 1, XPos] == ' ' || (YPos + 1 == NumberYPos && XPos == NumberXPos))
                        {
                            Map[YPos + 1, XPos] = SnakeSymbol;
                            YPos++;
                            break;
                        }
                        else
                            throw new Exception();
                    case Directions.none:
                        Map[YPos, XPos] = SnakeSymbol;
                        break;
                }
            }
            catch (Exception)// snake collided
            {
                if (Health > 1)// new spawn
                {
                    Health--;
                    YPos = DefaultYPos;
                    XPos = DefaultXPos;

                    FillMap(WallSymbol);
                    PlaceNewNumber();
                    
                    YTails.Clear();
                    XTails.Clear();
                    Update();

                    return 2;
                }
                else// dead
                {
                    Update();
                    Health = 0;
                    Update();

                    return 1;
                }
            }

            if (YPos == NumberYPos && XPos == NumberXPos)// fruit collected
            {
                Console.Beep();
                Size += Number;
                if (CurrentLevel == Level.second && (Delay - DelaySubtrahend) > MinDelay)
                {
                    Delay -= DelaySubtrahend;
                }

                if (Size >= ScoreToPassLevel && CurrentLevel == Level.first)
                {
                    YPos = DefaultYPos;
                    XPos = DefaultXPos;

                    CurrentLevel = Level.second;
                    FillMap(WallSymbol);
                    PlaceNewNumber();
                    YTails.Clear();
                    XTails.Clear();

                    return 3;// new level
                }
                PlaceNewNumber();
            }

            Update();
            Thread.Sleep(Delay);

            return 0;// success (nothing interesting happened)
        }

        private void FillMap(char borderSymbol)
        {
            if (CurrentLevel == Level.first)// clean map for level 1
            {
                for (int i = 0; i < Map.GetLength(0); i++)
                {
                    for (int j = 0; j < Map.GetLength(1); j++)
                    {
                        Map[i, j] = ' ';
                    }
                }
            }
            else if (CurrentLevel == Level.second)// map with obstacles for level 2
            {
                for (int i = 0; i < Map.GetLength(0); i++)
                {
                    for (int j = 0; j < Map.GetLength(1); j++)
                    {
                        if (i % 3 == 0 && j % 5 == 0)
                        {
                            Map[i, j] = borderSymbol;
                        }
                        else
                            Map[i, j] = ' ';
                    }
                }
            }

            for (int i = 0; i < Map.GetLength(0); i++)// place borders
            {
                for (int j = 0; j < Map.GetLength(1); j++)
                {
                    Map[0, j] = borderSymbol;
                    Map[Map.GetLength(0) - 1, j] = borderSymbol;
                }
                Map[i, 0] = borderSymbol;
                Map[i, Map.GetLength(1) - 1] = borderSymbol;
            }

            Map[DefaultYPos, DefaultXPos] = SnakeSymbol;
        }

        private void Update()
        {
            StringBuilder buffer = new StringBuilder();// to prevent output flickering

            buffer.Append("Health: " + Health + "\n");
            buffer.Append("Level: " + CurrentLevel + "\n\n");

            if (CurrentLevel == Level.first)
            {
                buffer.Append("Size: " + Size + "\n" + (ScoreToPassLevel - Size) + " to unlock next level\n");
            }
            else if (CurrentLevel == Level.second)
            {
                buffer.Append("Size: " + Size + "\n");
                buffer.Append("Desc: endless run with speed increase\n");
                buffer.Append("Speed: 1 step/" + Delay + "ms\n");
            }

            for (int i = 0; i < Map.GetLength(0); i++)
            {
                for (int j = 0; j < Map.GetLength(1); j++)
                {
                    buffer.Append(Map[i, j]);
                }
                buffer.Append("\n");
            }
            Console.CursorVisible = false;
            Console.Clear();
            Console.Write(buffer);
        }

        private void PlaceNewNumber()// new collectable (fruit)
        {
            int randomNumber = RandomNumber(1, 9);

            int randomXPos = 0;
            int randomYPos = 0;

            do
            {
                randomXPos = RandomNumber(1, Map.GetLength(0) - 1);
                randomYPos = RandomNumber(1, Map.GetLength(1) - 1);
            }
            while (Map[randomXPos, randomYPos] != ' ');

            char ch = '+';
            switch (randomNumber)
            {
                case 1:
                    ch = '1';
                    break;
                case 2:
                    ch = '2';
                    break;
                case 3:
                    ch = '3';
                    break;
                case 4:
                    ch = '4';
                    break;
                case 5:
                    ch = '5';
                    break;
                case 6:
                    ch = '6';
                    break;
                case 7:
                    ch = '7';
                    break;
                case 8:
                    ch = '8';
                    break;
                case 9:
                    ch = '9';
                    break;
            }
            Map[randomXPos, randomYPos] = ch;
            NumberYPos = randomXPos;
            NumberXPos = randomYPos;
            Number = randomNumber;
        }

        private int RandomNumber(int min, int max)
        {
            lock (SyncLock)
            {
                return Random.Next(min, max);
            }
        }
    }
}
