namespace GameServer
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            PacketRegistry.touch();
            // GameLogic.start();
            GameServer server = new GameServer();
            server.start(100);

            while(true) {
                Console.Write("input : ");
                string? input = Console.ReadLine();
                if (int.TryParse(input, out int output) == false)
                    continue;

                switch (output) {
                    case 1: {
                        Console.WriteLine($"current session count : {GameServer.currentSessionCount}");
                        break;
                    }
                }

            }
        }
    }
}
