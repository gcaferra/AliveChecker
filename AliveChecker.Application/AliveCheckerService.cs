using AliveChecker.Application.Auth;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Database;
using AliveChecker.Application.Database.Entities;
using AliveChecker.Application.Endpoints;
using AliveChecker.Application.Endpoints.Models;
using AliveChecker.Application.Files;
using AliveChecker.Application.Utils;
using Microsoft.Extensions.Logging;

namespace AliveChecker.Application;

public interface IAliveCheckerService
{
    Task<bool> Run(IFileService fileService, CancellationTokenSource cancellationToken);
}

public class AliveCheckerService(
    ITokenService tokenService,
    IDateProvider dateProvider,
    IAuthService authService,
    ICsvReadService csvReadService,
    ICsvWriteService csvWriteService,
    ICheckerRepository checkerRepository,
    ClientConfiguration configuration,
    IBodyCreationService bodyCreator,
    IAliveCheckerEndpoint aliveCheckerEndpoint,
    ILogger<AliveCheckerService> logger) : IAliveCheckerService
{
    ClientConfiguration Configuration { get; } = configuration;

    public async Task<bool> Run(IFileService fileService, CancellationTokenSource cancellationToken)
    {
        logger.LogInformation("Start importing configured file");

        logger.LogDebug("Initialize database");
        checkerRepository.InitializeDatabase(Configuration.InitializeDb);
        logger.LogDebug("Database initialized");

        logger.LogInformation("Importing Csv file {FilePath}", fileService.InputFilePath);
        if (!checkerRepository.FileIsImported(fileService.InputFilePath))
        {
            logger.LogDebug("Elaborating input file {FilePath}", fileService.InputFilePath);

            foreach (var peopleReadData in await csvReadService.ImportFile(fileService))
            {
                checkerRepository.Enqueue(peopleReadData.TaxId.Trim());
            }

            checkerRepository.FileCompleted(fileService.InputFilePath);
            logger.LogInformation("File import completed");
        }
        else
        {
            logger.LogInformation("File already imported");
        }

        logger.LogInformation("Authenticating...");
        var result = await authService.AuthenticateAssertion(Guid.NewGuid());
        if (!result.IsSuccess)
        {
            logger.LogWarning("Token not received. Exit");
            return false;
        }
        logger.LogInformation("Authenticated");

        logger.LogDebug("Token: {AccessToken}", result.Token.AccessToken);

        var queueCount = checkerRepository.QueueCount();

        logger.LogDebug("Record to process: {QueueCount}", queueCount);
        var operationCount = 0;

        await csvWriteService.OpenTargetAsync(fileService);

        logger.LogInformation("Checking statuses");
        foreach (var queueItem in checkerRepository.DeQueue())
        {
            if (cancellationToken.IsCancellationRequested)
                return false;
            
            var body = bodyCreator.CreateBody(queueItem.TaxId, operationCount++, dateProvider.UtcNow);
            logger.LogDebug("Body: {Body}", body);

            var signature = tokenService.GetSignature(body);

            logger.LogDebug("Signature: {Signature}", signature);
            var response = await aliveCheckerEndpoint.FetchPersonData(result.Token, result.AuditToken, signature, body);

            Person? person = null;
            switch (response)
            {
                case ServerErrorResponse errorResponse:
                    checkerRepository.NAck(queueItem.Id);
                    logger.LogError("Server Error: {TaxId} - Enqueue - {Status} ", queueItem.TaxId, errorResponse.ErrorDescription);
                    continue;
                case InternalServerResponse errorResponse:
                    checkerRepository.NAck(queueItem.Id);
                    logger.LogError("Exit application due to Internal Server Error: {Status}", errorResponse.ErrorDescription);
                    return false;
                case SuccessResponse successResponse:
                    logger.LogDebug("Answer:  {TaxId} - Success - {Status} ", queueItem.TaxId,
                        successResponse.StatusDescription);
                    person = successResponse.Map(queueItem.TaxId);
                    break;
                case NotFoundResponse notFoundResponse:
                    logger.LogDebug("Answer:  {TaxId} - NotFound - {Status} ", queueItem.TaxId,
                        notFoundResponse.StatusDescription);
                    person = notFoundResponse.Map(queueItem.TaxId);
                    break;
                case NotFoundByTokenExpiredResponse notFoundByTokenExpiredResponse:
                    checkerRepository.NAck(queueItem.Id);
                    logger.LogWarning("NotFound Error (Token Expired): {TaxId} - Enqueue- {Status} ", queueItem.TaxId, notFoundByTokenExpiredResponse.FullResponse);
                    continue;
                case UnauthorizedResponse unauthorizedResponse:
                    checkerRepository.NAck(queueItem.Id);
                    logger.LogWarning("Unauthorized Error (Token Expired): {TaxId} - Enqueue - {Status} ", queueItem.TaxId, unauthorizedResponse.FullResponse);
                    result = await authService.AuthenticateAssertion(Guid.NewGuid());
                    if (!result.IsSuccess)
                    {
                        logger.LogWarning("Token not renewed. Exiting");
                        return false;
                    }
                    continue;
                case RateLimitedResponse rateLimitedResponse:
                    logger.LogDebug("Rate Limited Error: {TaxId} - Enqueue - {Status} ", queueItem.TaxId,
                    rateLimitedResponse.ErrorDescription);
                    checkerRepository.NAck(queueItem.Id);
                    continue;
            }

            checkerRepository.Ack(queueItem.Id);
            checkerRepository.SavePerson(person!);
            await csvWriteService.ExportAsync(person!.Map());
        }

        logger.LogInformation("Elaboration Complete");

        return true;
    }
}