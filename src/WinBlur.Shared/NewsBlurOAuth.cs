using Microsoft.Security.Authentication.OAuth;
using Microsoft.UI;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Security.Credentials;
using Windows.Storage;

namespace WinBlur.Shared
{
    /// <summary>OAuth credentials shared by the foreground and background processes.</summary>
    public static class NewsBlurOAuth
    {
        // Register a public authorization-code client with NewsBlur. Do not put a client secret in the app.
        public static readonly string ClientId = "nBRKOtgkXO6Gt7QLvxtGrKxeTw2X9F70QWmr8Xx4";
        public static readonly string CallbackAuthority = "oauth-callback";
        public static readonly Uri CallbackUri = new Uri($"amittere-winblur://{CallbackAuthority}");
        public static readonly Uri AuthorizeUri = new Uri("https://www.newsblur.com/oauth/authorize");
        public static readonly Uri TokenUri = new Uri("https://www.newsblur.com/oauth/token");
        private const string Resource = "WinBlur.NewsBlurOAuth";
        private const string CredentialName = "tokens";
        private static volatile OAuthTokens cachedTokens;

        private sealed class OAuthTokens
        {
            public string AccessToken { get; set; }
            public string RefreshToken { get; set; }
            public DateTimeOffset ExpiresAt { get; set; }

            public OAuthTokens(TokenResponse response, string previousRefreshToken = null)
            {
                AccessToken = response.AccessToken;
                RefreshToken = string.IsNullOrEmpty(response.RefreshToken) ? previousRefreshToken : response.RefreshToken;
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn > 0 ? response.ExpiresIn : 3600);
            }
        }

        public static bool HasTokens => cachedTokens != null;

        private static OAuthTokens Load()
        {
            try
            {
                var credential = new PasswordVault().Retrieve(Resource, CredentialName);
                credential.RetrievePassword();
                return JsonSerializer.Deserialize<OAuthTokens>(credential.Password);
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is JsonException)
            {
                return null;
            }
        }

        private static void Save(OAuthTokens tokens)
        {
            new PasswordVault().Add(new PasswordCredential(Resource, CredentialName, JsonSerializer.Serialize(tokens)));
        }

        private static async Task<FileStream> AcquireLockAsync(CancellationToken cancellationToken)
        {
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "newsblur-oauth.lock");
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
        }

        private static bool IsValidTokenResponse(TokenResponse response)
        {
            return response != null &&
                   !string.IsNullOrEmpty(response.AccessToken) &&
                   string.Equals(response.TokenType, "Bearer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidToken(OAuthTokens tokens)
        {
            return tokens != null && !string.IsNullOrEmpty(tokens.AccessToken) &&
                   tokens.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(2);
        }

        public static async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (IsValidToken(cachedTokens))
            {
                return cachedTokens.AccessToken;
            }

            using (await AcquireLockAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Another process may have refreshed and rotated the saved token.
                cachedTokens = Load();

                if (IsValidToken(cachedTokens))
                {
                    return cachedTokens.AccessToken;
                }
                if (cachedTokens == null || string.IsNullOrEmpty(cachedTokens.RefreshToken))
                {
                    return null;
                }

                var request = TokenRequestParams.CreateForRefreshToken(cachedTokens.RefreshToken);
                request.ClientId = ClientId;

                var result = await OAuth2Manager.RequestTokenAsync(TokenUri, request).AsTask(cancellationToken);
                if (!IsValidTokenResponse(result.Response))
                {
                    throw new InvalidOperationException("NewsBlur token refresh failed. Please sign in again if the problem persists.");
                }

                cachedTokens = new OAuthTokens(result.Response, cachedTokens.RefreshToken);
                Save(cachedTokens);

                return cachedTokens.AccessToken;
            }
        }

        public static async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            using (await AcquireLockAsync(cancellationToken))
            {
                try
                {
                    var vault = new PasswordVault();
                    vault.Remove(vault.Retrieve(Resource, CredentialName));
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                    // No saved OAuth credential.
                }
                cachedTokens = null;
            }
        }

        public static async Task SignInAsync(WindowId parentWindowId, CancellationToken cancellationToken)
        {
            var request = AuthRequestParams.CreateForAuthorizationCodeRequest(ClientId, CallbackUri);
            request.Scope = "read write";

            var auth = await OAuth2Manager.RequestAuthWithParamsAsync(parentWindowId, AuthorizeUri, request).AsTask(cancellationToken);
            if (auth.Response == null)
            {
                throw new InvalidOperationException("NewsBlur authorization was canceled or failed. Please try again.");
            }

            var tokenRequest = TokenRequestParams.CreateForAuthorizationCodeRequest(auth.Response);
            var result = await OAuth2Manager.RequestTokenAsync(TokenUri, tokenRequest).AsTask(cancellationToken);
            if (!IsValidTokenResponse(result.Response))
            {
                throw new InvalidOperationException("NewsBlur could not exchange the authorization code. Check the client registration and try again.");
            }

            using (await AcquireLockAsync(cancellationToken))
            {
                var tokens = new OAuthTokens(result.Response, cachedTokens.RefreshToken);
                Save(tokens);
                cachedTokens = tokens;
            }
        }
    }
}
