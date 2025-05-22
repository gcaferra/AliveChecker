using System.Diagnostics.CodeAnalysis;
using AliveChecker.Application.Auth;
using AliveChecker.Application.Auth.Models;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Database;
using AliveChecker.Application.Database.Entities;
using AliveChecker.Application.Endpoints;
using AliveChecker.Application.Endpoints.Models;
using AliveChecker.Application.Files;
using AliveChecker.Application.Files.Models;
using AliveChecker.Application.Utils;
using Microsoft.Extensions.Logging.Abstractions;


namespace AliveChecker.Application.Tests;

[SuppressMessage("Substitute creation", "NS2002:Constructor parameters count mismatch.")]
[SuppressMessage("Non-substitutable member", "NS1004:Argument matcher used with a non-virtual member of a class.")]
public class AliveCheckerServiceTests
{
    readonly IAliveCheckerService _sut;
    readonly ITokenService _tokenService;
    readonly IDateProvider _dateProvider;
    readonly IAuthService _authService;
    readonly ICsvReadService _csvReadService;
    readonly ICsvWriteService _csvWriteService;
    readonly ICheckerRepository _checkerRepository;
    readonly IBodyCreationService _bodyCreator;
    readonly IAliveCheckerEndpoint _checkerEndpoint;
    readonly IFileService _fileService;
    readonly CancellationTokenSource _cancellationToken;

    public AliveCheckerServiceTests()
    {
        _cancellationToken = new CancellationTokenSource();
        _tokenService = Substitute.For<ITokenService>();
        _dateProvider = Substitute.For<IDateProvider>();
        _authService = Substitute.For<IAuthService>();
        _csvReadService = Substitute.For<ICsvReadService>();
        _csvWriteService = Substitute.For<ICsvWriteService>();
        _checkerRepository = Substitute.For<ICheckerRepository>();
        _bodyCreator = Substitute.For<IBodyCreationService>();
        _checkerEndpoint = Substitute.For<IAliveCheckerEndpoint>();
        _fileService = Substitute.For<IFileService>();

        var configuration = new ClientConfiguration
        {
            KeyId = "keyId",
            CsvFilePath = "csvFilePath",
            ServiceUrl = "serviceUrl",
            UserId = "userId",
            AuthenticationUrl = "authenticationUrl",
            InitializeDb = false,
            ClientId = "clientId",
            Audience = "audience",
            SignatureAudience = "signatureAudience",
            PurposeId = "purposeId",
            PrivateKey = "privateKey",

        };

        _sut = new AliveCheckerService(_tokenService, _dateProvider, _authService, _csvReadService, _csvWriteService, _checkerRepository, configuration, _bodyCreator, _checkerEndpoint, NullLogger<AliveCheckerService>.Instance);
    }


    [Fact]
    void When_Run_is_called_it_Import_the_configured_file()
    {
        // Arrange

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _csvReadService.Received().ImportFile(_fileService);
    }

    [Fact]
    void When_A_file_is_already_imported_the_import_is_skipped()
    {
        // Arrange
        var pathToCsvfileCsv = "csvFile.csv";
        _fileService.InputFilePath.Returns(pathToCsvfileCsv);
        _checkerRepository.FileIsImported(pathToCsvfileCsv).Returns(true);
        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _csvReadService.DidNotReceive().ImportFile(_fileService);
    }

    [Fact]
    void ImportFile_returns_a_List_of_records_and_they_are_saved_to_a_Queue()
    {
        // Arrange
        const string taxId = "123456789";
        var peopleData = new List<PeopleReadData>() { new(){TaxId = taxId}};
        _csvReadService.ImportFile(_fileService).Returns(peopleData );
        
        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().Enqueue(taxId);
    }


    [Fact]
    void GetPersons_is_called_to_find_the_list_of_Persons_to_check()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        _dateProvider.UtcNow.Returns(currentTime);
        
        var token = new Token();
        var auditToken = "thisIsAnAuditToken";

        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().DeQueue();

    }

    
    [Fact]
    void Person_data_is_used_to_crate_a_Body_string()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        _dateProvider.UtcNow.Returns(currentTime);
        
        var token = new Token();

        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _bodyCreator.Received().CreateBody("taxId", Arg.Any<int>(), currentTime);
    }

    [Fact]
    void When_the_Authentication_fails_the_program_exits()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        _dateProvider.UtcNow.Returns(currentTime);

        _authService.AuthenticateAssertion(Guid.NewGuid()).Returns(new AuthenticationResult(Token.Empty, string.Empty));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _tokenService.DidNotReceive().GetSignature(Arg.Any<string>());

    }


    [Fact]
    void The_Digest_is_used_to_create_the_Signature()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        _dateProvider.UtcNow.Returns(currentTime);

        _authService.AuthenticateAssertion(Arg.Any<Guid>())
            .Returns(new AuthenticationResult(new Token() { AccessToken = "AccessToken" }, string.Empty, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});

        _bodyCreator.CreateBody("taxId", Arg.Any<int>(), currentTime).Returns(body);

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _tokenService.Received().GetSignature(body);
    }

    
    [Fact]
    void Body_is_used_to_fetch_data_from_the_Service_EndPoint()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";
        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});

        _bodyCreator.CreateBody("taxId", Arg.Any<int>(), currentTime).Returns(body);
        
        _tokenService.GetSignature(body).Returns(signature);

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerEndpoint.Received().FetchPersonData(token, auditToken, signature, body);
    }

    [Fact]
    void Fetched_person_data_is_used_to_update_the_database()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));

        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});
        _bodyCreator.CreateBody("taxId", Arg.Any<int>(), currentTime).Returns(body);
        
        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new SuccessResponse(DateTime.UtcNow, DateTime.UtcNow, true, string.Empty,string.Empty, string.Empty) );

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().SavePerson(Arg.Is<Person>( p => p.TaxId == "taxId"));
    }

    
    [Fact]
    void Fetched_person_data_is_used_to_write_a_csv_file()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";
        
        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);
        
        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new SuccessResponse(DateTime.UtcNow, DateTime.UtcNow, true, string.Empty,string.Empty, string.Empty));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _csvWriteService.Received(1).ExportAsync(Arg.Is<PeopleWriteData>( wd => wd.TaxId == "taxId"));
    }

    [Fact]
    void Fetched_data_is_saved_to_database_while_processed()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";
        
        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId1"}, new QueueItem {TaxId = "taxId2"}});

        _bodyCreator.CreateBody(Arg.Any<string>(), Arg.Any<int>(), currentTime).Returns(body);
        
        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new SuccessResponse(DateTime.UtcNow, DateTime.UtcNow, true, string.Empty,string.Empty, string.Empty));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received(2).SavePerson(Arg.Any<Person>());
    }

    [Fact]
    void Fetched_data_is_saved_to_the_output_file_while_processed()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";

        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});

        _bodyCreator.CreateBody("taxId", Arg.Any<int>(), currentTime).Returns(body);
        
        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new SuccessResponse(DateTime.UtcNow, DateTime.UtcNow, true, string.Empty,string.Empty, string.Empty));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _csvWriteService.Received(1).ExportAsync(Arg.Any<PeopleWriteData>());
    }

    [Fact]
    void The_FileWriter_initialize_the_output_file()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";

        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {new QueueItem {TaxId = "taxId"}});

        _bodyCreator.CreateBody("taxId", Arg.Any<int>(), currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new SuccessResponse(DateTime.UtcNow, DateTime.UtcNow, true, string.Empty,string.Empty, string.Empty));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _csvWriteService.Received(1).OpenTargetAsync(_fileService);
    }

    [Fact]
    void if_the_server_returns_OK_the_QueueItem_is_AckAcknowledged()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";
        var queueItem = new QueueItem() {TaxId = "taxId"};

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new []{queueItem});

        _bodyCreator.CreateBody("taxId", Arg.Any<int>(), currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new SuccessResponse(DateTime.UtcNow, DateTime.UtcNow, true, string.Empty,string.Empty, string.Empty));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().Ack(queueItem.Id);
    }

    [Fact]
    void if_the_endpoint_return_NotFound_the_data_is_tracked()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";
        
        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new List<QueueItem> {new() {TaxId = "taxId"}});
        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);
        
        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new NotFoundResponse(DateTime.UtcNow, DateTime.UtcNow, string.Empty,string.Empty, "Not Found"));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().SavePerson(Arg.Is<Person>(p => p.StatusDescription!.Contains("Not Found", StringComparison.CurrentCulture)));

    }

    [Fact]
    void at_the_end_of_the_process_the_file_Path_and_Name_are_saved()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new List<QueueItem> {new() {TaxId = "taxId"}});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new NotFoundResponse(DateTime.UtcNow, DateTime.UtcNow, string.Empty,string.Empty, "Not Found"));
        _fileService.InputFilePath.Returns("cvsFile.csv");
        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().FileCompleted("cvsFile.csv");

    }

    [Fact]
    void if_the_server_returns_NotFound_the_QueueItem_is_AckAcknowledged()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";
        var queueItem = new QueueItem() {TaxId = "taxId"};

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));        _checkerRepository.DeQueue().Returns(new []{queueItem});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new NotFoundResponse(DateTime.UtcNow, DateTime.UtcNow, string.Empty,string.Empty, "Not Found"));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().Ack(queueItem.Id);

    }

    [Fact]
    void if_the_server_returns_BadRequest_the_QueueItem_is_NotAckAcknowledged()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";
        var queueItem = new QueueItem() {TaxId = "taxId"};

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));        _checkerRepository.DeQueue().Returns(new [] {queueItem});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new ServerErrorResponse("Not Found"));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().NAck(queueItem.Id);

    }

    [Fact]
    void if_the_server_returns_NotFound_but_the_Token_is_Expired_the_QueueItem_is_NotAckAcknowledged()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";
        var queueItem = new QueueItem() {TaxId = "taxId"};

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {queueItem});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new NotFoundByTokenExpiredResponse(DateTime.Now,"FullResponse" , "NotFound/TokenExpired"));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received().NAck(queueItem.Id);

    }

    [Fact]
    void if_the_server_returns_TokenExpired_the_token_must_be_Renewed()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";
        var queueItem = new QueueItem() {TaxId = "taxId"};

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>()).Returns(new AuthenticationResult(token, auditToken, true));
        _checkerRepository.DeQueue().Returns(new [] {queueItem});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new UnauthorizedResponse("FullResponse" , "Unauthorized"));

        // Act
        _authService.ClearReceivedCalls();
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _authService.Received().AuthenticateAssertion(Arg.Any<Guid>());
        _checkerRepository.DidNotReceive().Ack(Arg.Any<Guid>());

    }

    [Fact]
    void if_the_server_returns_TokenExpired_and_the_token_is_not_renewed_the_function_exit()
    {
        // Arrange
        var currentTime = DateTimeOffset.Now;
        string body = "thisIsTheBody";

        var token = new Token(){ AccessToken = "AccessToken"};
        var signature = "Signature";
        var queueItem = new QueueItem() {TaxId = "taxId"};

        _dateProvider.UtcNow.Returns(currentTime);
        var auditToken = "thisIsAnAuditToken";
        _authService.AuthenticateAssertion(Arg.Any<Guid>())
            .Returns(new AuthenticationResult(token, auditToken, true),
                new AuthenticationResult(Token.Empty, string.Empty, false));
        _checkerRepository.DeQueue().Returns(new [] {queueItem});

        _bodyCreator.CreateBody("taxId", 0, currentTime).Returns(body);

        _tokenService.GetSignature(body).Returns(signature);
        _checkerEndpoint.FetchPersonData(token, auditToken, signature, body).Returns(new UnauthorizedResponse("FullResponse" , "Unauthorized"));

        // Act
        _sut.Run(_fileService, _cancellationToken);

        // Assert
        _checkerRepository.Received(1).DeQueue();

    }


}