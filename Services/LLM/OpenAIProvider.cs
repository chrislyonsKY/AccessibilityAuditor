using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AccessibilityAuditor.Services.LLM
{
    /// <summary>
    /// LLM provider implementation for the OpenAI API.
    /// Posts to <c>https://api.openai.com/v1/chat/completions</c>.
    /// API key is retrieved from <see cref="CredentialProvider"/> at call time — never cached.
    /// </summary>
    public sealed class OpenAIProvider : ILLMProvider
    {
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";
        private const string Model = "gpt-4o";

        private readonly HttpClient _httpClient;
        private readonly CredentialProvider _credentials;

        /// <inheritdoc/>
        public LLMProviderType ProviderType => LLMProviderType.OpenAI;

        /// <inheritdoc/>
        public string DisplayName => "OpenAI (GPT-4o)";

        /// <summary>Initialises the provider with injected dependencies.</summary>
        /// <param name="httpClient">Singleton HttpClient — do not instantiate inline.</param>
        /// <param name="credentials">Credential provider for key retrieval.</param>
        public OpenAIProvider(HttpClient httpClient, CredentialProvider credentials)
        {
            _httpClient = httpClient;
            _credentials = credentials;
        }

        /// <inheritdoc/>
        public async Task<string> CompleteAsync(string prompt, CancellationToken ct)
        {
            var key = _credentials.Retrieve(LLMProviderType.OpenAI)
                ?? throw new InvalidOperationException("OpenAI API key is not configured.");

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);

            var body = new
            {
                model = Model,
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
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
    }
}
