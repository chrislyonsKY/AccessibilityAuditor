using System.Net.Http;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.Fixes;
using AccessibilityAuditor.Services.LLM;

namespace AccessibilityAuditor.Tests.Services;

public class LLMFixStrategyTests
{
    #region Helpers

    private static Finding MakeFinding(string ruleId = "WCAG_1_4_3_CONTRAST")
    {
        return new Finding
        {
            RuleId = ruleId,
            Severity = FindingSeverity.Fail,
            Element = "Label class 'Cities'",
            Detail = "Contrast ratio below threshold",
            ForegroundColor = new ColorInfo(180, 180, 180),
            BackgroundColor = new ColorInfo(255, 255, 255),
            ContrastRatio = 2.2
        };
    }

    /// <summary>Fake LLM provider that returns a configurable response.</summary>
    private sealed class FakeLLMProvider : ILLMProvider
    {
        private readonly string? _response;
        private readonly Exception? _exception;
        private readonly TimeSpan _delay;

        public FakeLLMProvider(string response)
        {
            _response = response;
        }

        public FakeLLMProvider(Exception exception)
        {
            _exception = exception;
        }

        public FakeLLMProvider(TimeSpan delay)
        {
            _delay = delay;
            _response = "delayed";
        }

        public LLMProviderType ProviderType => LLMProviderType.Anthropic;
        public string DisplayName => "Fake Provider";

        public async Task<string> CompleteAsync(string prompt, CancellationToken ct)
        {
            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, ct);
            }

            if (_exception is not null)
                throw _exception;

            return _response!;
        }
    }

    /// <summary>Fake credential provider backed by an in-memory dictionary.</summary>
    private sealed class FakeCredentialProvider : CredentialProvider
    {
        private readonly Dictionary<LLMProviderType, string> _keys = new();

        public override void Store(LLMProviderType provider, string key) =>
            _keys[provider] = key;

        public override string? Retrieve(LLMProviderType provider) =>
            _keys.TryGetValue(provider, out var key) ? key : null;

        public override bool IsConfigured(LLMProviderType provider) =>
            _keys.ContainsKey(provider);

        public override void Delete(LLMProviderType provider) =>
            _keys.Remove(provider);
    }

    #endregion

    #region RequiresApiKey

    [Fact]
    public void RequiresApiKey_ReturnsTrue()
    {
        var creds = new FakeCredentialProvider();
        var provider = new FakeLLMProvider("ok");
        var strategy = new LLMFixStrategy(provider, creds);

        Assert.True(strategy.RequiresApiKey);
    }

    #endregion

    #region Successful Suggestion

    [Fact]
    public async Task ApplyFix_SuccessfulResponse_ReturnsSuggested()
    {
        var creds = new FakeCredentialProvider();
        creds.Store(LLMProviderType.Anthropic, "test-key");
        var provider = new FakeLLMProvider("Use a darker shade of gray (#333333).");
        var strategy = new LLMFixStrategy(provider, creds);
        var finding = MakeFinding();

        var result = await strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Suggested, result.Status);
        Assert.Contains("#333333", result.SuggestedContent);
        Assert.Contains("review", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region No Key Configured

    [Fact]
    public async Task ApplyFix_NoKeyConfigured_ReturnsFailed()
    {
        var creds = new FakeCredentialProvider();
        var provider = new FakeLLMProvider("should not be called");
        var strategy = new LLMFixStrategy(provider, creds);
        var finding = MakeFinding();

        var result = await strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Failed, result.Status);
        Assert.Contains("No API key", result.Summary);
    }

    #endregion

    #region Cancellation

    [Fact]
    public async Task ApplyFix_CancelledToken_ReturnsFailed()
    {
        var creds = new FakeCredentialProvider();
        creds.Store(LLMProviderType.Anthropic, "test-key");
        var provider = new FakeLLMProvider(TimeSpan.FromSeconds(10));
        var strategy = new LLMFixStrategy(provider, creds);
        var finding = MakeFinding();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var result = await strategy.ApplyFixAsync(finding, cts.Token);

        Assert.Equal(FixStatus.Failed, result.Status);
        Assert.Contains("cancelled", result.Summary, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Provider Error

    [Fact]
    public async Task ApplyFix_ProviderThrows_ReturnsFailed()
    {
        var creds = new FakeCredentialProvider();
        creds.Store(LLMProviderType.Anthropic, "test-key");
        var provider = new FakeLLMProvider(new HttpRequestException("Connection refused"));
        var strategy = new LLMFixStrategy(provider, creds);
        var finding = MakeFinding();

        var result = await strategy.ApplyFixAsync(finding, CancellationToken.None);

        Assert.Equal(FixStatus.Failed, result.Status);
        Assert.Contains("Connection refused", result.Summary);
    }

    #endregion
}
