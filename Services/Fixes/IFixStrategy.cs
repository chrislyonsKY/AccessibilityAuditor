using System.Threading;
using System.Threading.Tasks;
using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Services.Fixes
{
    /// <summary>
    /// Contract for all fix strategies — both deterministic and LLM-assisted.
    /// </summary>
    public interface IFixStrategy
    {
        /// <summary>
        /// Whether this strategy requires a configured LLM API key.
        /// Deterministic strategies return <c>false</c>.
        /// LLM strategies return <c>true</c>.
        /// </summary>
        bool RequiresApiKey { get; }

        /// <summary>
        /// Attempts to fix or suggest a fix for the given finding.
        /// Deterministic strategies apply the fix directly and return <see cref="FixStatus.Applied"/>.
        /// LLM strategies return <see cref="FixStatus.Suggested"/> with content for user review.
        /// Any Pro SDK / CIM access must be inside a <c>QueuedTask.Run()</c> call.
        /// </summary>
        Task<FixResult> ApplyFixAsync(Finding finding, CancellationToken ct);
    }
}
