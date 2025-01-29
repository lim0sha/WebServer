using WebService.Model.Interfaces;

namespace WebService.Model.Database;

public class RepositoryMock
{
    private readonly ICrudRepository _crudRepository;
    
    public RepositoryMock(ICrudRepository crudRepository)
    {
        _crudRepository = crudRepository ?? throw new ArgumentNullException(nameof(crudRepository));
    }

    public void InitializeDatabase()
    {
        _crudRepository.InitializeDatabase();
    }

    public bool AuthenticateUser(string username, string password, out DateTime? latestLoginTime)
    {
        return _crudRepository.AuthenticateUser(username, password, out latestLoginTime);
    }

    public void RegisterUser(string username, string password)
    {
        _crudRepository.RegisterUser(username, password);
    }

    public void UpdateLoginTime(string username)
    {
        _crudRepository.UpdateLoginTime(username);
    }

    public string? GetUsernameBySessionId(string sessionId)
    {
        return _crudRepository.GetUsernameBySessionId(sessionId);
    }

    public void CreateSession(string sessionId, string username)
    {
        _crudRepository.CreateSession(sessionId, username);
    }

    public bool ValidateSession(string sessionId)
    {
        return _crudRepository.ValidateSession(sessionId);
    }

    public void DeleteSession(string sessionId)
    {
        _crudRepository.DeleteSession(sessionId);
    }

    public void DeleteAllSessionsForUser(string username)
    {
        _crudRepository.DeleteAllSessionsForUser(username);
    }
}