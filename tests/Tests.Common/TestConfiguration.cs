using Microsoft.Extensions.Configuration;

namespace RippleSync.Tests.Common;
public static class TestConfiguration
{
    public static IConfiguration Configuration
        => new ConfigurationBuilder()
                .AddInMemoryCollection(ConfigurationValues)
                .Build();

    private static readonly Dictionary<string, string?> ConfigurationValues = new()
    {
        //Fake keys
        {"Encryption:IntegrationAccessTokenKey", "b5aBzsd9Q9X9u6Zsx0V0uQUhll79dGr2q/6GsXzVk1I="},
        {"Encryption:IntegrationRefreshTokenKey", "+T0eum5Adn7pVisvTUUYURxDmEGeOcPqIdV1my0Eh2g="},
        {"Encryption:PostMessageKey", "QRSLjgSUvOJAg9E8HkZ0OEj9JOKeDFDVY9lY6zHCPHM="},
        {"Encryption:PostMediaKey", "0llXM00lgnBO1b0S7wPIcaqiBzPvsyqZMQcp8jUqKNs="},
        {"Encryption:UserEmailKey", "NjVCeYL2WrmvH2Z/SidkfLT9xC04vZZm/m6pWFUbvEc="},
        {"Encryption:UserTokenValueKey", "zH7GhHab9Zrlu+/jhHPPAsllcffjahz0YWm0/Xq5d+c="}
    };
}
