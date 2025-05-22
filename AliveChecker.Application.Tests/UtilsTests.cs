using AliveChecker.Application.Utils;
using FluentAssertions;

namespace AliveChecker.Application.Tests;

public class UtilsTests
{
    readonly IHashService _sut = new HashService();
    
    [Fact]
    public void AuditHash_is_transformed_to_Hex_string()
    {
        // Arrange
        var audit
            = "eyJ0eXAiOiJKV1QiLCJraWQiOiJTa0xwTUlpTGpVeGFzbWkxYjhsdnJ2M1U4VFJNV3A2MUNmWVVibVBCWHN3IiwiYWxnIjoiUlMyNTYifQ.eyJhdWQiOiJodHRwczovL21vZGlwYS12YWwuYW5wci5pbnRlcm5vLml0L2dvdndheS9yZXN0L2luL01pbkludGVybm9Qb3J0YUFOUFIvQzAxOS1zZXJ2aXppb0FjY2VydGFtZW50b0VzaXN0ZW56YVZpdGEvdjEiLCJzdWIiOiIzZWI3Y2ViOS0wYTIyLTRlZGEtOTdkMC1lZjcxY2NiMmNmNDQiLCJuYmYiOjE3MDgwMzA4NzIsInB1cnBvc2VJZCI6IjAxMzZjMDI4LWM0ZDAtNGM1ZS04OWNhLTY2MzA5ODc1NjRiMiIsImlzcyI6IjNlYjdjZWI5LTBhMjItNGVkYS05N2QwLWVmNzFjY2IyY2Y0NCIsInVzZXJMb2NhdGlvbiI6IjI2LjIuMTIuMjMiLCJleHAiOjE3MDg2MzA4NzIsImRub25jZSI6IjEyMzQ1Njc4OTAxMjMiLCJ1c2VySUQiOiJDRlJHUFA3NlMxNkEwNDhOIiwiaWF0IjoxNzA4MDMwODcyLCJqdGkiOiIzNWRjNDZiMi1mZDRiLTRkZjAtYmJmMi05NzU2NWQyNDkyOWUiLCJMb0EiOiJMT0EzIn0.NzcgVZRRoZ09DplzrSrVW_B2krgO-bvblwSlvpHr--LE9M5gFO22DJF9RbM1R_z7fn0zV23l131Ni_5c0kMtU7yYohB-tjE_LjuKlF-D8M4CH9V0MEWb8nOkdZmBnD8wSWlhkrEfjgwrjuzqNgvdxIy3KuQKWdIg7FXaoExBqnJ2a3H0_LWHaK32P3J6fTWqAOJWQIpUJqMmJn3An97X36xBs_7wb6q0UNci6qVR2lpX3ah7gO2HCa7ATGJsWGou8mMtSG0Rvp7coNz_SZxekC8W-z2-zGx7cQkLd5zj4rdzFTLPyl1lOYwYJ-IC-Nksd5DTyGNDyOuYXyCmbhHRGg"; //java calculated hash
        var expected = "d5192b7966e1ce0fd744b9ed15db1348c12c5c5ef541a1234db2549f6cc631e5";

        //Act
        var actual = _sut.ToHexString(audit);
        
        //Assert

        expected.Should().Be(actual);
    }
    
    [Fact]
    public void The_request_Body_is_transformed_to_Base64String()
    {
        // Arrange
        var body = "{\r\n    \"idOperazioneClient\": \"1\",\n    \"criteriRicerca\": {\n        \"codiceFiscale\": \"TTRPNN00R51F839V\"\n    },\n    \"datiRichiesta\": {\n        \"dataRiferimentoRichiesta\": \"2024-02-12\",\n        \"motivoRichiesta\": \"Verifica esistenza in Vita\",\n        \"casoUso\": \"C019\"\n    }\n}"
            .ReplaceLineEndings("\r\n");

        //Act
        var actual = _sut.ToBase64String(body);
        //Assert
        var expected = "8hOt8qAkL0QRTGmWYI5RWNel9IXoBzuN8cXAx0rm904="; //java calculated digest

        actual.Should().Be(expected);
    }
}