using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.LLM;

namespace AccessibilityAuditor.Services.Fixes
{
    /// <summary>
    /// Provides LLM-assisted fix suggestions for findings that benefit from AI reasoning.
    /// Requires a configured API key — check <see cref="RequiresApiKey"/> and
    /// <see cref="CredentialProvider.IsConfigured"/> before invoking.
    /// All suggestions go through user review; this strategy never auto-applies changes to CIM.
    /// </summary>
    public sealed class LLMFixStrategy : IFixStrategy
    {
        private readonly ILLMProvider _provider;
        private readonly CredentialProvider _credentials;

        /// <inheritdoc/>
        public bool RequiresApiKey => true;

        /// <summary>Initialises the strategy with required services.</summary>
        /// <param name="provider">The configured LLM provider.</param>
        /// <param name="credentials">Credential provider for key retrieval.</param>
        public LLMFixStrategy(ILLMProvider provider, CredentialProvider credentials)
        {
            _provider = provider;
            _credentials = credentials;
        }

        /// <inheritdoc/>
        public async Task<FixResult> ApplyFixAsync(Finding finding, CancellationToken ct)
        {
            if (!_credentials.IsConfigured(_provider.ProviderType))
                return new FixResult(FixStatus.Failed,
                    "No API key configured for this provider.");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                var prompt = BuildPrompt(finding);
                var suggestion = await _provider.CompleteAsync(prompt, cts.Token);

                return new FixResult(FixStatus.Suggested,
                    "AI suggestion ready for review.", suggestion);
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine($"LLM fix request cancelled for rule '{finding.RuleId}'.");
                return new FixResult(FixStatus.Failed, "Request was cancelled.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LLM fix failed for rule '{finding.RuleId}': {ex}");
                return new FixResult(FixStatus.Failed, $"AI fix failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the LLM prompt from the finding context.
        /// Sanitises any paths before inclusion — no server names or UNC paths in prompts.
        /// </summary>
        private static string BuildPrompt(Finding finding)
        {
            var element = SanitizePath(finding.Element);
            var detail = SanitizePath(finding.Detail);
            var remediation = finding.Remediation is not null
                ? SanitizePath(finding.Remediation)
                : "None provided.";

            var prompt =
                $"""
                You are an ArcGIS Pro accessibility expert. A WCAG 2.1 AA audit found an issue.

                Rule: {finding.RuleId}
                Criterion: {finding.Criterion?.Id} — {finding.Criterion?.Name}
                Element: {element}
                Detail: {detail}
                Current remediation guidance: {remediation}
                """;

            if (finding.ForegroundColor is not null && finding.BackgroundColor is not null)
            {
                prompt +=
                    $"""

                    Foreground color: {finding.ForegroundColor.Hex}
                    Background color: {finding.BackgroundColor.Hex}
                    Contrast ratio: {finding.ContrastRatio:F1}:1
                    """;
            }

            prompt +=
                """

                Provide a concise, actionable fix that can be applied in ArcGIS Pro.
                Focus on the specific CIM property or UI action needed.
                Do not include file paths or server names in your response.
                """;

            return prompt;
        }

        /// <summary>
        /// Removes UNC paths, server names, and user-identifying path segments.
        /// </summary>
        private static string SanitizePath(string input) =>
            Regex.Replace(input, @"\\\\[^\s\\]+\\[^\s]*|[A-Za-z]:\\Users\\[^\s\\]+", "[path]");
    }
}
