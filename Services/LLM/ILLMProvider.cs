using System.Threading;
using System.Threading.Tasks;

namespace AccessibilityAuditor.Services.LLM
{
    /// <summary>
    /// Abstraction over LLM API providers.
    /// Implementations handle provider-specific auth headers and endpoint routing.
    /// The API key is never cached — retrieved from <see cref="CredentialProvider"/> at call time.
    /// </summary>
    public interface ILLMProvider
    {
        /// <summary>The provider type this implementation handles.</summary>
        LLMProviderType ProviderType { get; }

        /// <summary>Human-readable provider name for UI display.</summary>
        string DisplayName { get; }

        /// <summary>
        /// Sends a completion request to the provider and returns the response text.
        /// </summary>
        /// <param name="prompt">The prompt to complete.</param>
        /// <param name="ct">Cancellation token — must be honoured.</param>
        /// <returns>The model's response as a plain string.</returns>
        Task<string> CompleteAsync(string prompt, CancellationToken ct);
    }
}
