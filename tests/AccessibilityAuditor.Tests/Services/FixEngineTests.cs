using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.Fixes;
using AccessibilityAuditor.Services.LLM;

namespace AccessibilityAuditor.Tests.Services;

public class FixEngineTests
{
    #region Helpers

    private static Finding MakeFinding(
        string ruleId, FindingSeverity severity = FindingSeverity.Fail)
    {
        return new Finding
        {
            RuleId = ruleId,
            Severity = severity,
            Element = "Test Element",
            Detail = "Test detail",
            ForegroundColor = new ColorInfo(180, 180, 180),
            BackgroundColor = new ColorInfo(255, 255, 255)
        };
    }

    private sealed class FakeLLMProvider : ILLMProvider
    {
        public LLMProviderType ProviderType => LLMProviderType.Anthropic;
        public string DisplayName => "Fake";
        public Task<string> CompleteAsync(string prompt, CancellationToken ct) =>
            Task.FromResult("AI suggestion");
    }

    private sealed class FakeCredentialProvider : CredentialProvider
    {
        private readonly Dictionary<LLMProviderType, string> _keys = new();

        public override void Store(LLMProviderType provider, string key) =>
            _keys[provider] = key;
        public override string? Retrieve(LLMProviderType provider) =>
            _keys.TryGetValue(provider, out var k) ? k : null;
        public override bool IsConfigured(LLMProviderType provider) =>
            _keys.ContainsKey(provider);
        public override void Delete(LLMProviderType provider) =>
            _keys.Remove(provider);
    }

    private static FixEngine CreateEngine(bool withLLM = false)
    {
        var deterministic = new DeterministicFixStrategy();
        LLMFixStrategy? llm = null;

        if (withLLM)
        {
            var creds = new FakeCredentialProvider();
            creds.Store(LLMProviderType.Anthropic, "test-key");
            llm = new LLMFixStrategy(new FakeLLMProvider(), creds);
        }

        return new FixEngine(deterministic, llm);
    }

    #endregion

    #region ResolveStrategy

    [Fact]
    public void ResolveStrategy_ContrastFinding_ReturnsDeterministic()
    {
        var engine = CreateEngine();
        var finding = MakeFinding("WCAG_1_4_3_CONTRAST");

        var strategy = engine.ResolveStrategy(finding);

        Assert.NotNull(strategy);
        Assert.False(strategy!.RequiresApiKey);
    }

    [Fact]
    public void ResolveStrategy_AltTextFinding_ReturnsDeterministic()
    {
        var engine = CreateEngine();
        var finding = MakeFinding("WCAG_1_1_1_ALT_TEXT");

        var strategy = engine.ResolveStrategy(finding);

        Assert.NotNull(strategy);
        Assert.False(strategy!.RequiresApiKey);
    }

    [Fact]
    public void ResolveStrategy_UnsupportedRule_WithLLM_ReturnsLLMStrategy()
    {
        var engine = CreateEngine(withLLM: true);
        var finding = MakeFinding("WCAG_2_4_6_HEADINGS");

        var strategy = engine.ResolveStrategy(finding);

        Assert.NotNull(strategy);
        Assert.True(strategy!.RequiresApiKey);
    }

    [Fact]
    public void ResolveStrategy_UnsupportedRule_WithoutLLM_ReturnsNull()
    {
        var engine = CreateEngine(withLLM: false);
        var finding = MakeFinding("WCAG_2_4_6_HEADINGS");

        var strategy = engine.ResolveStrategy(finding);

        Assert.Null(strategy);
    }

    [Fact]
    public void ResolveStrategy_PassFinding_ReturnsNull()
    {
        var engine = CreateEngine(withLLM: true);
        var finding = MakeFinding("WCAG_1_4_3_CONTRAST", FindingSeverity.Pass);

        var strategy = engine.ResolveStrategy(finding);

        Assert.Null(strategy);
    }

    [Fact]
    public void ResolveStrategy_ManualReviewFinding_ReturnsNull()
    {
        var engine = CreateEngine();
        var finding = MakeFinding("WCAG_1_4_3_CONTRAST", FindingSeverity.ManualReview);

        var strategy = engine.ResolveStrategy(finding);

        Assert.Null(strategy);
    }

    [Fact]
    public void ResolveStrategy_WarningFinding_ReturnsStrategy()
    {
        var engine = CreateEngine();
        var finding = MakeFinding("WCAG_1_4_3_CONTRAST", FindingSeverity.Warning);

        var strategy = engine.ResolveStrategy(finding);

        Assert.NotNull(strategy);
    }

    [Fact]
    public void ResolveStrategy_PrefersDeterministicOverLLM()
    {
        var engine = CreateEngine(withLLM: true);
        var finding = MakeFinding("WCAG_1_4_3_CONTRAST");

        var strategy = engine.ResolveStrategy(finding);

        Assert.NotNull(strategy);
        Assert.False(strategy!.RequiresApiKey);
    }

    #endregion

    #region ApplyAllDeterministicAsync

    [Fact]
    public async Task ApplyAllDeterministic_ProcessesSupportedFindings()
    {
        var engine = CreateEngine();
        var findings = new[]
        {
            MakeFinding("WCAG_1_4_3_CONTRAST"),
            MakeFinding("WCAG_1_1_1_ALT_TEXT"),
            MakeFinding("WCAG_2_4_6_HEADINGS") // unsupported — should be skipped
        };

        var results = await engine.ApplyAllDeterministicAsync(findings, CancellationToken.None);

        // Only contrast and alt text should be attempted
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task ApplyAllDeterministic_SkipsPassFindings()
    {
        var engine = CreateEngine();
        var findings = new[]
        {
            MakeFinding("WCAG_1_4_3_CONTRAST", FindingSeverity.Pass)
        };

        var results = await engine.ApplyAllDeterministicAsync(findings, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ApplyAllDeterministic_EmptyList_ReturnsEmpty()
    {
        var engine = CreateEngine();

        var results = await engine.ApplyAllDeterministicAsync(
            Array.Empty<Finding>(), CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ApplyAllDeterministic_ContinuesAfterIndividualFailure()
    {
        var engine = CreateEngine();
        var findings = new[]
        {
            // Missing colors — will fail
            new Finding
            {
                RuleId = "WCAG_1_4_3_CONTRAST",
                Severity = FindingSeverity.Fail,
                Element = "Test", Detail = "Test"
            },
            // Has colors — will succeed
            MakeFinding("WCAG_1_1_1_ALT_TEXT")
        };

        var results = await engine.ApplyAllDeterministicAsync(findings, CancellationToken.None);

        Assert.Equal(2, results.Count);
        // First should fail, second should succeed
        Assert.Equal(FixStatus.Failed, results[0].Result.Status);
        Assert.Equal(FixStatus.Suggested, results[1].Result.Status);
    }

    [Fact]
    public async Task ApplyAllDeterministic_RespectsCancellation()
    {
        var engine = CreateEngine();
        var findings = new[]
        {
            MakeFinding("WCAG_1_4_3_CONTRAST"),
            MakeFinding("WCAG_1_1_1_ALT_TEXT")
        };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => engine.ApplyAllDeterministicAsync(findings, cts.Token));
    }

    #endregion
}
