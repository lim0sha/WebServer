namespace WebService.Model.Interfaces;

public interface IWebServer
{
    void Run(int initialConnections, int maxConnections);
}