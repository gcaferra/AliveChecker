namespace AliveChecker.Application.Auth.Models;

public record AuthenticationResult(Token Token,string AuditToken,bool IsSuccess = false);