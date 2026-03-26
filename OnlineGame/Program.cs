namespace OnlineGame
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var server = new Server();

            await server.Start(5050);
        }
    }
}
