using System.Collections.Generic;
using System.Linq;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Core.Rules;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.Tests.Core;

/// <summary>
/// Unit tests for <see cref="RuleRegistry"/> covering registration,
/// filtering, and duplicate prevention.
/// </summary>
public sealed class RuleRegistryTests
{
    [Fact]
    public void Register_AddsRule()
    {
        var registry = new RuleRegistry();
        var rule = new FakeRule("RULE_1", AuditTargetType.Map);

        registry.Register(rule);

        Assert.Equal(1, registry.Count);
        Assert.Contains(rule, registry.AllRules);
    }

    [Fact]
    public void Register_MultipleRules_AllPresent()
    {
        var registry = new RuleRegistry();
        registry.Register(new FakeRule("RULE_1", AuditTargetType.Map));
        registry.Register(new FakeRule("RULE_2", AuditTargetType.Layout));
        registry.Register(new FakeRule("RULE_3", AuditTargetType.WebMap));

        Assert.Equal(3, registry.Count);
    }

    [Fact]
    public void Register_Null_Throws()
    {
        var registry = new RuleRegistry();
        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void Register_DuplicateRuleId_Throws()
    {
        var registry = new RuleRegistry();
        registry.Register(new FakeRule("RULE_1", AuditTargetType.Map));

        Assert.Throws<InvalidOperationException>(() =>
            registry.Register(new FakeRule("RULE_1", AuditTargetType.Layout)));
    }

    [Fact]
    public void GetApplicableRules_FiltersCorrectly()
    {
        var registry = new RuleRegistry();
        registry.Register(new FakeRule("MAP_RULE", AuditTargetType.Map));
        registry.Register(new FakeRule("LAYOUT_RULE", AuditTargetType.Layout));
        registry.Register(new FakeRule("BOTH_RULE", AuditTargetType.Map, AuditTargetType.Layout));

        var mapRules = registry.GetApplicableRules(AuditTargetType.Map);
        var layoutRules = registry.GetApplicableRules(AuditTargetType.Layout);
        var webMapRules = registry.GetApplicableRules(AuditTargetType.WebMap);

        Assert.Equal(2, mapRules.Count);    // MAP_RULE + BOTH_RULE
        Assert.Equal(2, layoutRules.Count); // LAYOUT_RULE + BOTH_RULE
        Assert.Empty(webMapRules);
    }

    [Fact]
    public void GetApplicableRules_EmptyRegistry_ReturnsEmpty()
    {
        var registry = new RuleRegistry();

        var rules = registry.GetApplicableRules(AuditTargetType.Map);

        Assert.Empty(rules);
    }

    [Fact]
    public void GetOfflineRules_FiltersNetworkRules()
    {
        var registry = new RuleRegistry();
        registry.Register(new FakeRule("OFFLINE", AuditTargetType.Map, requiresNetwork: false));
        registry.Register(new FakeRule("ONLINE", AuditTargetType.Map, requiresNetwork: true));

        var offlineRules = registry.GetOfflineRules();

        Assert.Single(offlineRules);
        Assert.Equal("OFFLINE", offlineRules[0].RuleId);
    }

    [Fact]
    public void AllRules_ReturnsReadOnlyList()
    {
        var registry = new RuleRegistry();
        registry.Register(new FakeRule("RULE_1", AuditTargetType.Map));

        var rules = registry.AllRules;

        Assert.IsAssignableFrom<IReadOnlyList<IComplianceRule>>(rules);
    }

    #region Helpers

    /// <summary>
    /// Minimal fake rule for testing the registry without ArcGIS SDK dependencies.
    /// </summary>
    private sealed class FakeRule : IComplianceRule
    {
        public FakeRule(string ruleId, params AuditTargetType[] targets)
        {
            RuleId = ruleId;
            ApplicableTargets = targets;
        }

        public FakeRule(string ruleId, AuditTargetType target, bool requiresNetwork)
        {
            RuleId = ruleId;
            ApplicableTargets = new[] { target };
            RequiresNetwork = requiresNetwork;
        }

        public string RuleId { get; }
        public WcagCriterion Criterion => new("0.0.0", "Test", WcagPrinciple.Perceivable, "A", "Test", "http://test");
        public string Description => "Test rule";
        public AuditTargetType[] ApplicableTargets { get; }
        public bool RequiresNetwork { get; }

        public Task<IReadOnlyList<Finding>> EvaluateAsync(
            AuditContext context,
            System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Finding>>(Array.Empty<Finding>());
        }
    }

    #endregion
}
