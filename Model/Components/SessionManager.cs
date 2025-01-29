namespace WebService.Model.Components
{
    public class SessionManager
    {
        private readonly Dictionary<string, DateTime> _sessions = new();
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(30);
        private readonly object _lock = new();

        public string CreateSession()
        {
            lock (_lock)
            {
                var sessionId = Guid.NewGuid().ToString();
                _sessions[sessionId] = DateTime.Now.Add(_sessionTimeout);
                return sessionId;
            }
        }

        public bool ValidateSession(string sessionId)
        {
            lock (_lock)
            {
                return _sessions.ContainsKey(sessionId) && _sessions[sessionId] > DateTime.Now;
            }
        }

        public void RemoveSession(string sessionId)
        {
            lock (_lock)
            {
                _sessions.Remove(sessionId);
            }
        }

        public void ExpireOldSessions()
        {
            lock (_lock)
            {
                var expired = _sessions
                    .Where(s => s.Value <= DateTime.Now)
                    .Select(s => s.Key)
                    .ToList();

                foreach (var sessionId in expired)
                {
                    _sessions.Remove(sessionId);
                }
            }
        }
    }
}