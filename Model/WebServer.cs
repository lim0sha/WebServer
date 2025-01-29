using WebService.Model.Components;
using WebService.Model.Interfaces;

namespace WebService.Model;

public class WebServer : IWebServer
{
    private readonly string _connectionString;

    public WebServer(string connectionString)
    {
        _connectionString = connectionString;
    }
    public void Run(int initialConnections, int maxConnections)
    {
        var server = new Server(initialConnections, maxConnections, _connectionString);
        server.InitDb();
        server.Start();
    }
}