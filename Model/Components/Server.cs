using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using WebService.Model.Database;

namespace WebService.Model.Components
{
    public class Server
    {
        private readonly Router _router;
        private readonly Semaphore _sem;
        private readonly SessionManager _sessionManager;
        private readonly RepositoryMock _repository;

        public Server(int initialConnections, int maxConnections, string connectionString)
        {
            _router = new Router();
            _sem = new Semaphore(initialConnections, maxConnections, "mySemaphore");
            _sessionManager = new SessionManager();
            _repository = new RepositoryMock(new CrudRepository(connectionString));
        }

        public void InitDb()
        {
            _repository.InitializeDatabase();
        }

        private List<IPAddress> GetLocalIPs()
        {
            var ipHostEntry = Dns.GetHostEntry(Dns.GetHostName());
            return ipHostEntry.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();
        }

        private HttpListener InitListener(List<IPAddress> localHostIPs)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost/");
            localHostIPs.ForEach(ip =>
            {
                System.Console.WriteLine("Listening on IP " + "http://" + ip + "/");
                listener.Prefixes.Add($"http://{ip}/");
            });

            return listener;
        }

        private async Task StartListenerConnection(HttpListener listener)
        {
            try
            {
                _sessionManager.ExpireOldSessions();
                var context = await listener.GetContextAsync();

                var responseString = _router.HandleRequest(context);
                var encoded = Encoding.UTF8.GetBytes(responseString);

                context.Response.ContentLength64 = encoded.Length;
                await context.Response.OutputStream.WriteAsync(encoded);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _sem.Release();
            }
        }

        private void RunServer(HttpListener listener)
        {
            while (true)
            {
                _sem.WaitOne();
                Task.Run(() => StartListenerConnection(listener));
            }
        }

        private void TaskStart(HttpListener listener)
        {
            listener.Start();
            Task.Run(() => RunServer(listener));
        }

        public void Start()
        {
            var localHostIPs = GetLocalIPs();
            var listener = InitListener(localHostIPs);

            _router.AddRoute("login", context =>
            {
                var response = context.Response;
                var method = context.Request.HttpMethod;
                if (method != "POST")
                {
                    return LoadStaticFile(context, "Website/login.html");
                }

                using var reader = new StreamReader(context.Request.InputStream);
                var json = reader.ReadToEnd();
                var userCredentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (userCredentials == null)
                {
                    throw new ArgumentNullException(nameof(userCredentials));
                }
                var username = userCredentials["username"];
                var password = userCredentials["password"];
                DateTime? latestLoginTime;

                if (_repository.AuthenticateUser(username, password, out latestLoginTime))
                {
                    var sessionId = _sessionManager.CreateSession();
                    _repository.CreateSession(sessionId, username);
                    response.Cookies.Add(new Cookie("session", sessionId)
                    {
                        HttpOnly = true,
                        Expires = DateTime.Now.AddMinutes(30)
                    });

                    _repository.UpdateLoginTime(username);
                    response.StatusCode = (int)HttpStatusCode.OK;

                    return JsonSerializer.Serialize(new { success = true });
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;

                    return JsonSerializer.Serialize(new { success = false, message = "Login failed." });
                }
            });

            _router.AddRoute("register", context =>
            {
                var response = context.Response;
                var method = context.Request.HttpMethod;
                if (method != "POST")
                {
                    return LoadStaticFile(context, "Website/register.html");
                }
                using var reader = new StreamReader(context.Request.InputStream);
                var json = reader.ReadToEnd();
                var userCredentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (userCredentials == null)
                {
                    return LoadStaticFile(context, "Website/register.html");
                }

                var username = userCredentials["username"];
                var password = userCredentials["password"];
                try
                {
                    _repository.RegisterUser(username, password);
                    response.StatusCode = (int)HttpStatusCode.OK;

                    return JsonSerializer.Serialize(new { status = "success" });
                }
                catch (Exception)
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;

                    return JsonSerializer.Serialize(new { status = "failed" });
                }
            });

            _router.AddRoute("log", context =>
            {
                var request = context.Request;
                var sessionId = request.Cookies["session"]?.Value;
                var loggedIn = !string.IsNullOrEmpty(sessionId) && _sessionManager.ValidateSession(sessionId) &&
                               _repository.ValidateSession(sessionId);
                string? username = null;
                if (loggedIn)
                {
                    if (sessionId != null)
                    {
                        username = _repository.GetUsernameBySessionId(sessionId);
                    }
                }

                var jsonResponse = JsonSerializer.Serialize(new { loggedIn, username });
                context.Response.ContentType = "application/json";

                return jsonResponse;
            });


            _router.AddRoute("logout", context =>
            {
                var response = context.Response;
                var sessionId = context.Request.Cookies["session"]?.Value;
                if (!string.IsNullOrEmpty(sessionId))
                {
                    _sessionManager.RemoveSession(sessionId);
                    _repository.DeleteSession(sessionId);
                }

                response.Cookies.Add(new Cookie("session", "")
                {
                    HttpOnly = true,
                    Expires = DateTime.Now.AddMinutes(-1)
                });

                response.StatusCode = (int)HttpStatusCode.OK;

                return LoadStaticFile(context, "Website/logout.html");
            });

            _router.AddRoute("", context => LoadStaticFile(context, "Website/index.html"));
            _router.AddRoute("index.html", context => LoadStaticFile(context, "Website/index.html"));
            _router.AddRoute("styles.css", context => LoadStaticFile(context, "Website/styles.css"));

            TaskStart(listener);
        }

        private static string LoadStaticFile(HttpListenerContext context, string filePath)
        {
            if (File.Exists(filePath))
            {
                var contentType = "text/html";
                if (filePath.EndsWith(".css")) contentType = "text/css";
                else if (filePath.EndsWith(".js")) contentType = "application/javascript";
                context.Response.ContentType = contentType;

                return File.ReadAllText(filePath);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

                return "<html><body><h1>404 Not Found</h1></body></html>";
            }
        }
    }
}