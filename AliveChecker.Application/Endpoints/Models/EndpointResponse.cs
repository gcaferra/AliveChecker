namespace AliveChecker.Application.Endpoints.Models;


public abstract record EndpointResponse(){}

public record SuccessResponse(
    DateTime? CheckDate,
    DateTime? ReferenceDate,
    bool? IsAlive,
    string? FullResponse,
    string? AnprOperationId,
    string? StatusDescription,
    string DeathDate = ""): EndpointResponse();

public record NotFoundResponse(
    DateTime? CheckDate,
    DateTime? ReferenceDate,
    string? FullResponse,
    string? AnprOperationId,
    string? StatusDescription) : EndpointResponse();

public record NotFoundByTokenExpiredResponse(
    DateTime? CheckDate,
    string? FullResponse,
    string? StatusDescription): EndpointResponse();

public record UnauthorizedResponse(
    string? FullResponse,
    string? StatusDescription): EndpointResponse();

public record ServerErrorResponse(string ErrorDescription): EndpointResponse();
public record InternalServerResponse(string ErrorDescription): EndpointResponse();

public record RateLimitedResponse(string ErrorDescription): EndpointResponse();