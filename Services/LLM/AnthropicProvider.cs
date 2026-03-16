using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AccessibilityAuditor.Services.LLM
{
    /// <summary>
    /// LLM provider implementation for the Anthropic Claude API.
    /// Posts to <c>https://api.anthropic.com/v1/messages</c>.
    /// API key is retrieved from <see cref="CredentialProvider"/> at call time — never cached.
    /// </summary>
    public sealed class AnthropicProvider : ILLMProvider
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private const string Model = "claude-sonnet-4-6";
        private const string ApiVersion = "2023-06-01";

        private readonly HttpClient _httpClient;
        private readonly CredentialProvider _credentials;

        /// <inheritdoc/>
        public LLMProviderType ProviderType => LLMProviderType.Anthropic;

        /// <inheritdoc/>
        public string DisplayName => "Anthropic (Claude)";

        /// <summary>Initialises the provider with injected dependencies.</summary>
        /// <param name="httpClient">Singleton HttpClient — do not instantiate inline.</param>
        /// <param name="credentials">Credential provider for key retrieval.</param>
        public AnthropicProvider(HttpClient httpClient, CredentialProvider credentials)
        {
            _httpClient = httpClient;
            _credentials = credentials;
        }

        /// <inheritdoc/>
        public async Task<string> CompleteAsync(string prompt, CancellationToken ct)
        {
            var key = _credentials.Retrieve(LLMProviderType.Anthropic)
                ?? throw new InvalidOperationException("Anthropic API key is not configured.");

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Add("x-api-key", key);
            request.Headers.Add("anthropic-version", ApiVersion);

            var body = new
            {
                model = Model,
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };
            request.Content = JsonContent.Create(body);

            Debug.WriteLine($"Sending completion request to {ProviderType}...");
            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }
}
