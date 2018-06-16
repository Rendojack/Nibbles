using System;

namespace Nibbles
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Game game = new Game(200, 20, 50, 20, 40, 7, 1, '@', '#', 3, 15);
                game.Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
