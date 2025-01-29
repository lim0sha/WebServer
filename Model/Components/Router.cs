using System.Net;

namespace WebService.Model.Components
{
    public class Router
    {
        private const string Error404 = "<html><body><h1>404 Not Found</h1></body></html>";
        private readonly Dictionary<string, Func<HttpListenerContext, string>> _routes = new();

        public void AddRoute(string path, Func<HttpListenerContext, string> handler)
        {
            _routes[path] = handler;
        }

        public string HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            if (request.Url == null)
            {
                return Error404;
            }
            var path = request.Url.AbsolutePath.TrimStart('/');
            return _routes.TryGetValue(path, out var value) ? value(context) : Error404;
        }
    }
}