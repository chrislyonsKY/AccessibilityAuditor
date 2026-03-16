using AccessibilityAuditor.Core.Models;

namespace AccessibilityAuditor.Core.Constants
{
    /// <summary>
    /// Static definitions for all in-scope WCAG 2.1 AA success criteria.
    /// </summary>
    public static class WcagCriteria
    {
        private const string BaseUrl = "https://www.w3.org/WAI/WCAG21/Understanding";

        // ?? Perceivable ??

        /// <summary>1.1.1 Non-text Content (Level A).</summary>
        public static readonly WcagCriterion NonTextContent = new(
            "1.1.1", "Non-text Content", WcagPrinciple.Perceivable, "A",
            "All non-text content has a text alternative that serves the equivalent purpose.",
            $"{BaseUrl}/non-text-content.html");

        /// <summary>1.3.1 Info and Relationships (Level A).</summary>
        public static readonly WcagCriterion InfoAndRelationships = new(
            "1.3.1", "Info and Relationships", WcagPrinciple.Perceivable, "A",
            "Information, structure, and relationships conveyed through presentation can be programmatically determined.",
            $"{BaseUrl}/info-and-relationships.html");

        /// <summary>1.3.2 Meaningful Sequence (Level A).</summary>
        public static readonly WcagCriterion MeaningfulSequence = new(
            "1.3.2", "Meaningful Sequence", WcagPrinciple.Perceivable, "A",
            "When the sequence in which content is presented affects its meaning, a correct reading sequence can be programmatically determined.",
            $"{BaseUrl}/meaningful-sequence.html");

        /// <summary>1.3.3 Sensory Characteristics (Level A).</summary>
        public static readonly WcagCriterion SensoryCharacteristics = new(
            "1.3.3", "Sensory Characteristics", WcagPrinciple.Perceivable, "A",
            "Instructions do not rely solely on sensory characteristics such as shape, color, size, or visual location.",
            $"{BaseUrl}/sensory-characteristics.html");

        /// <summary>1.4.1 Use of Color (Level A).</summary>
        public static readonly WcagCriterion UseOfColor = new(
            "1.4.1", "Use of Color", WcagPrinciple.Perceivable, "A",
            "Color is not used as the only visual means of conveying information.",
            $"{BaseUrl}/use-of-color.html");

        /// <summary>1.4.3 Contrast Minimum (Level AA).</summary>
        public static readonly WcagCriterion ContrastMinimum = new(
            "1.4.3", "Contrast (Minimum)", WcagPrinciple.Perceivable, "AA",
            "Text and images of text have a contrast ratio of at least 4.5:1 (3:1 for large text).",
            $"{BaseUrl}/contrast-minimum.html");

        /// <summary>1.4.5 Images of Text (Level AA).</summary>
        public static readonly WcagCriterion ImagesOfText = new(
            "1.4.5", "Images of Text", WcagPrinciple.Perceivable, "AA",
            "Text is used to convey information rather than images of text.",
            $"{BaseUrl}/images-of-text.html");

        /// <summary>1.4.11 Non-text Contrast (Level AA).</summary>
        public static readonly WcagCriterion NonTextContrast = new(
            "1.4.11", "Non-text Contrast", WcagPrinciple.Perceivable, "AA",
            "Visual presentation of UI components and graphical objects have a contrast ratio of at least 3:1.",
            $"{BaseUrl}/non-text-contrast.html");

        // ?? Operable ??

        /// <summary>2.4.2 Page Titled (Level A).</summary>
        public static readonly WcagCriterion PageTitled = new(
            "2.4.2", "Page Titled", WcagPrinciple.Operable, "A",
            "Pages have titles that describe topic or purpose.",
            $"{BaseUrl}/page-titled.html");

        /// <summary>2.4.6 Headings and Labels (Level AA).</summary>
        public static readonly WcagCriterion HeadingsAndLabels = new(
            "2.4.6", "Headings and Labels", WcagPrinciple.Operable, "AA",
            "Headings and labels describe topic or purpose.",
            $"{BaseUrl}/headings-and-labels.html");

        // ?? Understandable ??

        /// <summary>3.1.1 Language of Page (Level A).</summary>
        public static readonly WcagCriterion LanguageOfPage = new(
            "3.1.1", "Language of Page", WcagPrinciple.Understandable, "A",
            "The default human language of each page can be programmatically determined.",
            $"{BaseUrl}/language-of-page.html");

        /// <summary>3.1.2 Language of Parts (Level AA).</summary>
        public static readonly WcagCriterion LanguageOfParts = new(
            "3.1.2", "Language of Parts", WcagPrinciple.Understandable, "AA",
            "The human language of each passage or phrase can be programmatically determined.",
            $"{BaseUrl}/language-of-parts.html");

        /// <summary>3.3.2 Labels or Instructions (Level A).</summary>
        public static readonly WcagCriterion LabelsOrInstructions = new(
            "3.3.2", "Labels or Instructions", WcagPrinciple.Understandable, "A",
            "Labels or instructions are provided when content requires user input.",
            $"{BaseUrl}/labels-or-instructions.html");

        // ?? Robust ??

        /// <summary>4.1.1 Parsing (Level A).</summary>
        public static readonly WcagCriterion Parsing = new(
            "4.1.1", "Parsing", WcagPrinciple.Robust, "A",
            "Content can be reliably interpreted by user agents, including assistive technologies.",
            $"{BaseUrl}/parsing.html");

        /// <summary>4.1.2 Name, Role, Value (Level A).</summary>
        public static readonly WcagCriterion NameRoleValue = new(
            "4.1.2", "Name, Role, Value", WcagPrinciple.Robust, "A",
            "For all user interface components, the name and role can be programmatically determined.",
            $"{BaseUrl}/name-role-value.html");
    }
}
