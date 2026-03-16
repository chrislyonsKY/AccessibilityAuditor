using System.Collections.Generic;
using System.Linq;

namespace AccessibilityAuditor.Core.Models
{
    /// <summary>
    /// Aggregated compliance scores for an audit result, broken down by WCAG principle.
    /// </summary>
    public sealed class ScoreCard
    {
        /// <summary>
        /// Gets the overall compliance score (0–100).
        /// </summary>
        public int OverallScore { get; private set; }

        /// <summary>
        /// Gets per-principle scores.
        /// </summary>
        public IReadOnlyDictionary<WcagPrinciple, PrincipleScore> PrincipleScores { get; private set; }
            = new Dictionary<WcagPrinciple, PrincipleScore>();

        /// <summary>
        /// Gets the total number of Pass findings.
        /// </summary>
        public int TotalPass { get; private set; }

        /// <summary>
        /// Gets the total number of Warning findings.
        /// </summary>
        public int TotalWarning { get; private set; }

        /// <summary>
        /// Gets the total number of Fail findings.
        /// </summary>
        public int TotalFail { get; private set; }

        /// <summary>
        /// Gets the total number of ManualReview findings.
        /// </summary>
        public int TotalManualReview { get; private set; }

        /// <summary>
        /// Gets the total number of Error findings.
        /// </summary>
        public int TotalError { get; private set; }

        /// <summary>
        /// Computes a <see cref="ScoreCard"/> from a collection of findings.
        /// </summary>
        /// <remarks>
        /// Score per principle = (Pass + Warning*0.5) / (Pass + Warning + Fail + ManualReview) * 100.
        /// Error findings are excluded from scoring. Overall score is the weighted average across principles.
        /// </remarks>
        public static ScoreCard Calculate(IReadOnlyList<Finding> findings)
        {
            var card = new ScoreCard();

            if (findings is null || findings.Count == 0)
            {
                card.PrincipleScores = new Dictionary<WcagPrinciple, PrincipleScore>();
                return card;
            }

            card.TotalPass = findings.Count(f => f.Severity == FindingSeverity.Pass);
            card.TotalWarning = findings.Count(f => f.Severity == FindingSeverity.Warning);
            card.TotalFail = findings.Count(f => f.Severity == FindingSeverity.Fail);
            card.TotalManualReview = findings.Count(f => f.Severity == FindingSeverity.ManualReview);
            card.TotalError = findings.Count(f => f.Severity == FindingSeverity.Error);

            var principleScores = new Dictionary<WcagPrinciple, PrincipleScore>();
            var principles = new[] { WcagPrinciple.Perceivable, WcagPrinciple.Operable, WcagPrinciple.Understandable, WcagPrinciple.Robust };

            foreach (var principle in principles)
            {
                var pFindings = findings
                    .Where(f => f.Criterion?.Principle == principle && f.Severity != FindingSeverity.Error)
                    .ToList();

                principleScores[principle] = PrincipleScore.Calculate(principle, pFindings);
            }

            card.PrincipleScores = principleScores;

            var scorable = principleScores.Values.Where(ps => ps.Total > 0).ToList();
            card.OverallScore = scorable.Count > 0
                ? (int)scorable.Average(ps => ps.Score)
                : 0;

            return card;
        }
    }

    /// <summary>
    /// Score data for a single WCAG principle.
    /// </summary>
    public sealed class PrincipleScore
    {
        /// <summary>Gets the WCAG principle.</summary>
        public WcagPrinciple Principle { get; init; }

        /// <summary>Gets the score (0–100).</summary>
        public int Score { get; init; }

        /// <summary>Gets the count of Pass findings.</summary>
        public int PassCount { get; init; }

        /// <summary>Gets the count of Warning findings.</summary>
        public int WarningCount { get; init; }

        /// <summary>Gets the count of Fail findings.</summary>
        public int FailCount { get; init; }

        /// <summary>Gets the count of ManualReview findings.</summary>
        public int ManualReviewCount { get; init; }

        /// <summary>Gets the total scorable findings.</summary>
        public int Total => PassCount + WarningCount + FailCount + ManualReviewCount;

        /// <summary>
        /// Calculates the principle score from the given findings.
        /// </summary>
        public static PrincipleScore Calculate(WcagPrinciple principle, IReadOnlyList<Finding> findings)
        {
            int pass = findings.Count(f => f.Severity == FindingSeverity.Pass);
            int warn = findings.Count(f => f.Severity == FindingSeverity.Warning);
            int fail = findings.Count(f => f.Severity == FindingSeverity.Fail);
            int manual = findings.Count(f => f.Severity == FindingSeverity.ManualReview);
            int total = pass + warn + fail + manual;

            int score = total > 0
                ? (int)((pass + warn * 0.5) / total * 100)
                : 0;

            return new PrincipleScore
            {
                Principle = principle,
                Score = score,
                PassCount = pass,
                WarningCount = warn,
                FailCount = fail,
                ManualReviewCount = manual
            };
        }
    }
}
