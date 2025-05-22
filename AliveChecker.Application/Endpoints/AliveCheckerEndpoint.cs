using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.RateLimiting;
using AliveChecker.Application.Auth.Models;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Endpoints.Models;
using AliveChecker.Application.Utils;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimiting;

namespace AliveChecker.Application.Endpoints;

public interface IAliveCheckerEndpoint
{
    Task<EndpointResponse> FetchPersonData(Token token, string audit, string signature, string bodyContent);
}

public class AliveCheckerEndpoint(ClientConfiguration configuration, ILogger<AliveCheckerEndpoint> logger, HttpClient client, IHashService hashService, IDateProvider dateProvider) : IAliveCheckerEndpoint
{
    readonly ResiliencePipeline _policy = new ResiliencePipelineBuilder()
            .AddRateLimiter(new FixedWindowRateLimiter(
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 49900,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0,
                    Window = TimeSpan.FromDays(1),
                    AutoReplenishment = true,

                })).Build();

    public async Task<EndpointResponse> FetchPersonData(Token token, string audit, string signature, string bodyContent)
    {
        var digest = hashService.ToBase64String(bodyContent);
        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.ServiceUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Headers.Add("Digest", $"SHA-256={digest}");
        request.Headers.Add("Agid-JWT-TrackingEvidence", audit);
        request.Headers.Add("Agid-JWT-Signature", signature);
        request.Content = new StringContent(bodyContent, new MediaTypeHeaderValue("application/json"));

        try
        {
            var response = await _policy.ExecuteAsync(async cancellationToken =>
                await client.SendAsync(request, cancellationToken));

            logger.LogDebug("Response: {@Response}", response);

            var fullResponse = await response.Content.ReadAsStringAsync();
            var checkDate = dateProvider.UtcNow.DateTime;
            var referenceDate = checkDate.AddDays(-1).Date;

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.BadRequest)
                    return new ServerErrorResponse(fullResponse);

                if (response.StatusCode == HttpStatusCode.InternalServerError)
                    return new InternalServerResponse(fullResponse);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                    return new UnauthorizedResponse(fullResponse, response.StatusCode.ToString());

                logger.LogInformation("Check Failed");
                logger.LogDebug("FullResponse: {Response}", fullResponse);
                var errorResponse =
                    JsonSerializer
                        .Deserialize<
                            ErrorResponse>(fullResponse); // await response.Content.ReadFromJsonAsync<ErrorResponse>();

                if (errorResponse?.Errors == null && string.IsNullOrEmpty(errorResponse?.IdOperazioneAnpr))
                {
                    return new NotFoundByTokenExpiredResponse(checkDate, fullResponse, "NotFound/TokenExpired");
                }

                if (errorResponse.Errors.Any(x =>
                        x.CodiceErroreAnomalia.Equals("EN122", StringComparison.InvariantCulture)))
                {
                    return new NotFoundResponse(checkDate,
                        referenceDate,
                        fullResponse,
                        errorResponse?.IdOperazioneAnpr,
                        response.StatusCode.ToString());
                }
            }

            var personResponse = await response.Content.ReadFromJsonAsync<PersonResponse>() ??
                                 throw new InvalidOperationException();

            return new SuccessResponse(
                checkDate,
                referenceDate,
                personResponse.ListaSoggetti.datiSoggetto[0].infoSoggettoEnte.GetBool(EndpointConstants.IS_ALIVE),
                fullResponse,
                personResponse.IdOperazioneAnpr,
                response.StatusCode.ToString(),
                personResponse.ListaSoggetti.datiSoggetto[0].infoSoggettoEnte
                    .GetDateString(EndpointConstants.DEATH_DATE)
            );
        }
        catch (RateLimiterRejectedException e)
        {
            logger.LogError(e, "Rate limit reached");
            return new RateLimitedResponse(e.Message);
        }
        catch (HttpRequestException requestException)
        {
            logger.LogError(requestException, "Request exception");
            return new ServerErrorResponse(requestException.Message);
        }
    }
}

internal static class AliveCheckerEndpointExtensions
{
    internal static bool GetBool(this IEnumerable<InfoSoggettoEnte> infos, string id)
    {
        var info = infos.FirstOrDefault(i => i.id == id);
        if (info != null)
        {
            return info.valore == "S";
        }

        return false;
    }
    internal static string GetDateString(this IEnumerable<InfoSoggettoEnte> infos, string id)
    {
        var info = infos.FirstOrDefault(i => i.id == id);
        if (info != null)
            return info.valoreData ?? string.Empty;
        return string.Empty;
    }
}

internal abstract class EndpointConstants
{
    internal const string IS_ALIVE = "1003";
    internal const string DEATH_DATE = "1015";
}