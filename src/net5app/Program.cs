
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

const string azureAdCommonDiscoveryEndpoint = "https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration";
OpenIdConnectConfiguration config = default(OpenIdConnectConfiguration);
List<string> validIssuers = new List<string>
                        {
                            "https://sts.windows.net/XXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXX/",
                            "https://sts.windows.net/XXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXX/"
                        };
const string audience = "api://XXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXX";
JsonSerializerOptions options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,  PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(route =>
                {
                    route.MapGet("/", async context => 
                    {   
                        try
                        {
                            var authHeader = $"{context.Request.Headers["Authorization"]}";
                            if (!string.IsNullOrWhiteSpace(authHeader))
                            {
                                string accessToken = authHeader.Substring(7);
                                if (config == null)
                                {
                                    var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(azureAdCommonDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
                                    config = await configManager.GetConfigurationAsync();
                                }
                                var claims = new JwtSecurityTokenHandler()
                                    .ValidateToken(accessToken,
                                    new TokenValidationParameters
                                    {
                                        ValidIssuers = validIssuers,
                                        ValidAudience = audience,
                                        ValidateAudience = true,
                                        ValidateIssuer = true,
                                        IssuerSigningKey = config.SigningKeys.FirstOrDefault(),
                                        ValidateLifetime = true
                                    }, out var jwt);
                                await context.Response.WriteAsJsonAsync<JwtSecurityToken>(jwt as JwtSecurityToken, options);
                                return;
                            }
                        }
                        catch { }
                        context.Response.StatusCode = 401;
                    });
                });
            });
        })
        .Build().Run();



