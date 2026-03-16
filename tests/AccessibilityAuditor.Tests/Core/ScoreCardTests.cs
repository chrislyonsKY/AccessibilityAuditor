using System.Collections.Generic;
using AccessibilityAuditor.Core.Constants;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Tests.Core;

/// <summary>
/// Unit tests for <see cref="ScoreCard"/> verifying score calculation logic.
/// </summary>
public sealed class ScoreCardTests
{
    [Fact]
    public void Calculate_NullFindings_ReturnsZeroScore()
    {
        var card = ScoreCard.Calculate(null!);

        Assert.Equal(0, card.OverallScore);
    }

    [Fact]
    public void Calculate_EmptyFindings_ReturnsZeroScore()
    {
        var card = ScoreCard.Calculate(new List<Finding>());

        Assert.Equal(0, card.OverallScore);
        Assert.Equal(0, card.TotalPass);
        Assert.Equal(0, card.TotalFail);
    }

    [Fact]
    public void Calculate_AllPass_Returns100()
    {
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Operable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.Equal(100, card.OverallScore);
        Assert.Equal(3, card.TotalPass);
        Assert.Equal(0, card.TotalFail);
    }

    [Fact]
    public void Calculate_AllFail_ReturnsZero()
    {
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Fail, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Fail, WcagPrinciple.Operable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.Equal(0, card.OverallScore);
        Assert.Equal(0, card.TotalPass);
        Assert.Equal(2, card.TotalFail);
    }

    [Fact]
    public void Calculate_MixedSeverity_ScoresCorrectly()
    {
        // 1 pass + 1 fail in Perceivable ? score = (1 + 0) / (1 + 1) * 100 = 50
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Fail, WcagPrinciple.Perceivable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.True(card.PrincipleScores.ContainsKey(WcagPrinciple.Perceivable));
        Assert.Equal(50, card.PrincipleScores[WcagPrinciple.Perceivable].Score);
    }

    [Fact]
    public void Calculate_WarningsCountAsHalf()
    {
        // Score = (Pass + Warning*0.5) / Total * 100
        // 1 warning out of 2 total: (0 + 0.5) / 2 * 100 = 25
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Warning, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Fail, WcagPrinciple.Perceivable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.Equal(25, card.PrincipleScores[WcagPrinciple.Perceivable].Score);
    }

    [Fact]
    public void Calculate_ErrorsExcludedFromScoring()
    {
        // Error findings should not affect the score
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Error, WcagPrinciple.Perceivable),
        };

        var card = ScoreCard.Calculate(findings);

        // Error is excluded: 1 pass / 1 total = 100
        Assert.Equal(100, card.PrincipleScores[WcagPrinciple.Perceivable].Score);
        Assert.Equal(1, card.TotalError);
    }

    [Fact]
    public void Calculate_ManualReviewCountsInDenominator()
    {
        // 1 pass + 1 manual review: (1 + 0) / (1 + 1) * 100 = 50
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Operable),
            MakeFinding(FindingSeverity.ManualReview, WcagPrinciple.Operable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.Equal(50, card.PrincipleScores[WcagPrinciple.Operable].Score);
        Assert.Equal(1, card.TotalManualReview);
    }

    [Fact]
    public void Calculate_MultiplePrinciples_AveragesCorrectly()
    {
        var findings = new List<Finding>
        {
            // Perceivable: 1 pass / 1 = 100
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            // Operable: 0 pass / 1 = 0
            MakeFinding(FindingSeverity.Fail, WcagPrinciple.Operable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.Equal(100, card.PrincipleScores[WcagPrinciple.Perceivable].Score);
        Assert.Equal(0, card.PrincipleScores[WcagPrinciple.Operable].Score);
        // Overall = average of (100, 0) = 50
        Assert.Equal(50, card.OverallScore);
    }

    [Fact]
    public void Calculate_PrincipleWithNoFindings_HasZeroScore()
    {
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
        };

        var card = ScoreCard.Calculate(findings);

        // Robust has no findings — score should be 0
        Assert.Equal(0, card.PrincipleScores[WcagPrinciple.Robust].Score);
    }

    [Fact]
    public void Calculate_CountsAreCorrect()
    {
        var findings = new List<Finding>
        {
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Pass, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Warning, WcagPrinciple.Perceivable),
            MakeFinding(FindingSeverity.Fail, WcagPrinciple.Operable),
            MakeFinding(FindingSeverity.ManualReview, WcagPrinciple.Robust),
            MakeFinding(FindingSeverity.Error, WcagPrinciple.Perceivable),
        };

        var card = ScoreCard.Calculate(findings);

        Assert.Equal(2, card.TotalPass);
        Assert.Equal(1, card.TotalWarning);
        Assert.Equal(1, card.TotalFail);
        Assert.Equal(1, card.TotalManualReview);
        Assert.Equal(1, card.TotalError);
    }

    #region Helpers

    private static Finding MakeFinding(FindingSeverity severity, WcagPrinciple principle)
    {
        return new Finding
        {
            RuleId = "TEST_RULE",
            Criterion = new WcagCriterion(
                $"{(int)principle}.0.0",
                "Test Criterion",
                principle,
                "AA",
                "Test description",
                "http://test"),
            Severity = severity,
            Element = "Test Element",
            Detail = "Test detail"
        };
    }

    #endregion
}
