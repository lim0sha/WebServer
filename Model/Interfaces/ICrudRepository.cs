namespace WebService.Model.Interfaces;

public interface ICrudRepository
{
    public void InitializeDatabase();

    public bool AuthenticateUser(string username, string password, out DateTime? latestLoginTime);

    public void RegisterUser(string username, string password);

    public void UpdateLoginTime(string username);

    public string? GetUsernameBySessionId(string sessionId);

    public void CreateSession(string sessionId, string username);

    public bool ValidateSession(string sessionId);

    public void DeleteSession(string sessionId);

    public void DeleteAllSessionsForUser(string username);
}