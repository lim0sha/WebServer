using WebService.Model.Interfaces;

namespace WebService.Console
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            const string connectionString = "User ID=postgres;Password=limosha;Host=localhost;Port=5432;Database=WebServiceDB";
            IWebServer webServer = new Model.WebServer(connectionString);
            if (webServer == null)
            {
                throw new ArgumentNullException(nameof(webServer));
            }
            webServer.Run(10, 20);
            System.Console.ReadLine();
        }
    }
}