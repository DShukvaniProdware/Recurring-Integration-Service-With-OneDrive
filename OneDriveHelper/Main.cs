using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Prompt = Microsoft.Identity.Client.Prompt;

namespace OneDriveHelper
{
    public class OneDriveAuthenticationHelper
    {
        // The Client ID is used by the application to uniquely identify itself to the v2.0 authentication endpoint.

        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        private const string Instance = "https://login.microsoftonline.com/";
        public string[] Scopes = { "Files.ReadWrite.All" };

        public static IPublicClientApplication ClientApp;

        public string TokenForUser;
        public DateTimeOffset Expiration;

        private GraphServiceClient _graphClient;

        public OneDriveAuthenticationHelper(string clientId, string tenantId, string userId, string password)
        {
            ClientId = clientId;
            TenantId = tenantId;
            UserId = userId;
            Password = password;
        }

        // Get an access token for the given context and resourceId. An attempt is first made to 
        // acquire the token silently. If that fails, then we try to acquire the token by prompting the user.
        public async Task<GraphServiceClient> GetAuthenticatedClient()
        {
            if (_graphClient == null)
            {
                // Create Microsoft Graph client.
                try
                {
                    // Build a client application.
                    IPublicClientApplication publicClientApplication = PublicClientApplicationBuilder.Create(ClientId)
                        .WithAuthority($"{Instance}{TenantId}")
                        .WithDefaultRedirectUri()
                        .Build();

                    try
                    {
                        var password = new NetworkCredential(UserId, Password).SecurePassword;
                        await publicClientApplication.AcquireTokenByUsernamePassword(Scopes, UserId, password)
                            .ExecuteAsync();
                    }
                    catch (Exception)
                    {
                        try
                        {
                            var accounts = (await publicClientApplication.GetAccountsAsync()).ToArray();
                            
                            await publicClientApplication.AcquireTokenInteractive(Scopes)
                                .WithAccount(accounts.FirstOrDefault())
                                .WithPrompt(Prompt.SelectAccount)
                                .ExecuteAsync();
                        }
                        catch (MsalException msalex)
                        {
                            Debug.WriteLine($"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                        }
                    }

                    // Create an authentication provider by passing in a client application and graph scopes.
                    DeviceCodeProvider authProvider = new DeviceCodeProvider(publicClientApplication, Scopes);

                    ClientApp = publicClientApplication;

                    // Create a new instance of GraphServiceClient with the authentication provider.

                    // And this is the place where problem is being thrown, that it's unable to load NewtonSoft 6.0.0.0
                    // But most intriguing part is that, when this code is called from project "Scheduler", it works just fine
                    // But once it's called from the Project "Job.Import", it all goes wrong and error is thrown
                    // All of this led me to think that something is wrong with Quartz.net
                    _graphClient = new GraphServiceClient(authProvider);
                    return _graphClient;
                }

                catch (Exception ex)
                {
                    Debug.WriteLine("Could not create a graph client: " + ex.Message);
                }
            }

            return _graphClient;
        }

        /// <summary>
        /// Signs the user out of the service.
        /// </summary>
        public async Task SignOut()
        {
            foreach (var user in await ClientApp.GetAccountsAsync())
            {
                await ClientApp.RemoveAsync(user);
            }
            _graphClient = null;
            TokenForUser = null;

        }

    }
}
