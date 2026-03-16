using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Orchestration;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the Dashboard tab showing overall and per-principle scores.
    /// </summary>
    internal sealed class DashboardViewModel : ObservableObject
    {
        private int _overallScore;
        /// <summary>Gets or sets the overall compliance score (0–100).</summary>
        public int OverallScore
        {
            get => _overallScore;
            set => SetProperty(ref _overallScore, value);
        }

        private int _perceivableScore;
        /// <summary>Gets or sets the Perceivable principle score.</summary>
        public int PerceivableScore
        {
            get => _perceivableScore;
            set => SetProperty(ref _perceivableScore, value);
        }

        private int _operableScore;
        /// <summary>Gets or sets the Operable principle score.</summary>
        public int OperableScore
        {
            get => _operableScore;
            set => SetProperty(ref _operableScore, value);
        }

        private int _understandableScore;
        /// <summary>Gets or sets the Understandable principle score.</summary>
        public int UnderstandableScore
        {
            get => _understandableScore;
            set => SetProperty(ref _understandableScore, value);
        }

        private int _robustScore;
        /// <summary>Gets or sets the Robust principle score.</summary>
        public int RobustScore
        {
            get => _robustScore;
            set => SetProperty(ref _robustScore, value);
        }

        private int _failCount;
        /// <summary>Gets or sets the total Fail count.</summary>
        public int FailCount
        {
            get => _failCount;
            set => SetProperty(ref _failCount, value);
        }

        private int _warningCount;
        /// <summary>Gets or sets the total Warning count.</summary>
        public int WarningCount
        {
            get => _warningCount;
            set => SetProperty(ref _warningCount, value);
        }

        private int _manualReviewCount;
        /// <summary>Gets or sets the total ManualReview count.</summary>
        public int ManualReviewCount
        {
            get => _manualReviewCount;
            set => SetProperty(ref _manualReviewCount, value);
        }

        private int _passCount;
        /// <summary>Gets or sets the total Pass count.</summary>
        public int PassCount
        {
            get => _passCount;
            set => SetProperty(ref _passCount, value);
        }

        private string _targetName = string.Empty;
        /// <summary>Gets or sets the audited target name.</summary>
        public string TargetName
        {
            get => _targetName;
            set => SetProperty(ref _targetName, value);
        }

        private bool _hasResults;
        /// <summary>Gets or sets whether results are available to display.</summary>
        public bool HasResults
        {
            get => _hasResults;
            set => SetProperty(ref _hasResults, value);
        }

        private string _perceivableSummary = string.Empty;
        /// <summary>Gets or sets the Perceivable pass/total summary text.</summary>
        public string PerceivableSummary
        {
            get => _perceivableSummary;
            set => SetProperty(ref _perceivableSummary, value);
        }

        private string _operableSummary = string.Empty;
        /// <summary>Gets or sets the Operable pass/total summary text.</summary>
        public string OperableSummary
        {
            get => _operableSummary;
            set => SetProperty(ref _operableSummary, value);
        }

        private string _understandableSummary = string.Empty;
        /// <summary>Gets or sets the Understandable pass/total summary text.</summary>
        public string UnderstandableSummary
        {
            get => _understandableSummary;
            set => SetProperty(ref _understandableSummary, value);
        }

        private string _robustSummary = string.Empty;
        /// <summary>Gets or sets the Robust pass/total summary text.</summary>
        public string RobustSummary
        {
            get => _robustSummary;
            set => SetProperty(ref _robustSummary, value);
        }

        private string _auditSummaryText = string.Empty;
        /// <summary>Gets or sets the overall audit summary text for display or export.</summary>
        public string AuditSummaryText
        {
            get => _auditSummaryText;
            set => SetProperty(ref _auditSummaryText, value);
        }

        /// <summary>
        /// Updates the dashboard from an audit result.
        /// </summary>
        public void UpdateFromResult(AuditResult result)
        {
            if (result?.Score is null) return;

            TargetName = result.Target?.Name ?? "Unknown";
            OverallScore = result.Score.OverallScore;

            PerceivableScore = GetPrincipleScore(result.Score, WcagPrinciple.Perceivable);
            OperableScore = GetPrincipleScore(result.Score, WcagPrinciple.Operable);
            UnderstandableScore = GetPrincipleScore(result.Score, WcagPrinciple.Understandable);
            RobustScore = GetPrincipleScore(result.Score, WcagPrinciple.Robust);

            PerceivableSummary = GetPrincipleSummary(result.Score, WcagPrinciple.Perceivable);
            OperableSummary = GetPrincipleSummary(result.Score, WcagPrinciple.Operable);
            UnderstandableSummary = GetPrincipleSummary(result.Score, WcagPrinciple.Understandable);
            RobustSummary = GetPrincipleSummary(result.Score, WcagPrinciple.Robust);

            FailCount = result.Score.TotalFail;
            WarningCount = result.Score.TotalWarning;
            ManualReviewCount = result.Score.TotalManualReview;
            PassCount = result.Score.TotalPass;

            AuditSummaryText = $"Accessibility Audit — {TargetName}\n" +
                               $"Overall Score: {OverallScore}/100\n" +
                               $"Perceivable: {PerceivableScore}/100 ({PerceivableSummary})\n" +
                               $"Operable: {OperableScore}/100 ({OperableSummary})\n" +
                               $"Understandable: {UnderstandableScore}/100 ({UnderstandableSummary})\n" +
                               $"Robust: {RobustScore}/100 ({RobustSummary})\n" +
                               $"Findings: {FailCount} Fail, {WarningCount} Warning, {PassCount} Pass, {ManualReviewCount} Review";

            HasResults = true;
        }

        private static int GetPrincipleScore(ScoreCard score, WcagPrinciple principle)
        {
            return score.PrincipleScores.TryGetValue(principle, out var ps) ? ps.Score : 0;
        }

        private static string GetPrincipleSummary(ScoreCard score, WcagPrinciple principle)
        {
            if (!score.PrincipleScores.TryGetValue(principle, out var ps) || ps.Total == 0)
                return "no findings";
            return $"{ps.PassCount} pass / {ps.Total} total";
        }
    }
}
