using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccessibilityAuditor.Core.Models;
using AccessibilityAuditor.Services.LLM;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// Represents a single message in the AI chat conversation.
    /// </summary>
    internal sealed class ChatMessage : ObservableObject
    {
        public ChatMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }

        /// <summary>"user" or "assistant"</summary>
        public string Role { get; }

        private string _content;
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public bool IsUser => Role == "user";
        public bool IsAssistant => Role == "assistant";
    }

    /// <summary>
    /// ViewModel for the AI Chat tab. Provides a conversational interface
    /// for asking accessibility questions with audit context.
    /// Only functional when an API key is configured.
    /// </summary>
    internal sealed partial class AIChatViewModel : ObservableObject
    {
        private readonly CredentialProvider _credentials;
        private readonly ILLMProvider[] _providers;
        private readonly Func<System.Collections.Generic.IReadOnlyList<Finding>> _getCurrentFindings;
        private CancellationTokenSource? _cts;

        public AIChatViewModel(
            CredentialProvider credentials,
            ILLMProvider[] providers,
            Func<System.Collections.Generic.IReadOnlyList<Finding>> getCurrentFindings)
        {
            _credentials = credentials;
            _providers = providers;
            _getCurrentFindings = getCurrentFindings;
        }

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        private string _inputText = string.Empty;
        public string InputText
        {
            get => _inputText;
            set
            {
                if (SetProperty(ref _inputText, value))
                    SendCommand.NotifyCanExecuteChanged();
            }
        }

        private bool _isSending;
        public bool IsSending
        {
            get => _isSending;
            set
            {
                if (SetProperty(ref _isSending, value))
                {
                    SendCommand.NotifyCanExecuteChanged();
                    StopCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Whether any provider has a configured key.
        /// The chat is only usable when this is true.
        /// </summary>
        public bool IsAvailable =>
            _providers.Any(p => _credentials.IsConfigured(p.ProviderType));

        /// <summary>Refresh availability when key status changes.</summary>
        public void RefreshAvailability() => OnPropertyChanged(nameof(IsAvailable));

        private RelayCommand? _sendCommand;
        public RelayCommand SendCommand => _sendCommand ??= new RelayCommand(
            async () => await SendAsync(),
            () => !string.IsNullOrWhiteSpace(InputText) && !IsSending && IsAvailable);

        private RelayCommand? _stopCommand;
        public RelayCommand StopCommand => _stopCommand ??= new RelayCommand(
            () => _cts?.Cancel(),
            () => IsSending);

        private RelayCommand? _clearCommand;
        public RelayCommand ClearCommand => _clearCommand ??= new RelayCommand(
            () => Messages.Clear(),
            () => Messages.Count > 0);

        private async Task SendAsync()
        {
            var userText = InputText.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // Find active provider
            var provider = _providers.FirstOrDefault(p =>
                _credentials.IsConfigured(p.ProviderType));
            if (provider is null) return;

            // Add user message
            Messages.Add(new ChatMessage("user", userText));
            InputText = string.Empty;

            // Add placeholder for assistant response
            var assistantMsg = new ChatMessage("assistant", "Thinking...");
            Messages.Add(assistantMsg);

            IsSending = true;
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));

            try
            {
                var prompt = BuildPrompt(userText);
                var response = await provider.CompleteAsync(prompt, _cts.Token);
                assistantMsg.Content = response;
            }
            catch (OperationCanceledException)
            {
                assistantMsg.Content = "Request cancelled.";
            }
            catch (Exception ex)
            {
                assistantMsg.Content = $"Error: {ex.Message}";
                Debug.WriteLine($"AI chat error: {ex}");
            }
            finally
            {
                IsSending = false;
                _cts?.Dispose();
                _cts = null;
                ClearCommand.NotifyCanExecuteChanged();
            }
        }

        private string BuildPrompt(string userMessage)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an accessibility expert assistant for ArcGIS Pro maps and layouts.");
            sb.AppendLine("Answer questions about WCAG 2.1 AA compliance, accessibility best practices,");
            sb.AppendLine("and how to fix accessibility issues in GIS products.");
            sb.AppendLine();

            // Include audit context if available
            var findings = _getCurrentFindings();
            if (findings.Count > 0)
            {
                sb.AppendLine("=== Current Audit Findings ===");
                int shown = 0;
                foreach (var f in findings.Where(f =>
                    f.Severity is FindingSeverity.Fail or FindingSeverity.Warning))
                {
                    if (shown >= 20) // Cap context to avoid huge prompts
                    {
                        sb.AppendLine($"... and {findings.Count(ff => ff.Severity is FindingSeverity.Fail or FindingSeverity.Warning) - 20} more");
                        break;
                    }
                    sb.AppendLine($"- [{f.Severity}] {f.RuleId}: {f.Element} - {f.Detail}");
                    if (f.ForegroundColor is not null && f.BackgroundColor is not null)
                        sb.AppendLine($"  Colors: {f.ForegroundColor.Hex} on {f.BackgroundColor.Hex}, ratio {f.ContrastRatio:F1}:1");
                    shown++;
                }
                sb.AppendLine("=== End Findings ===");
                sb.AppendLine();
            }

            // Include conversation history (last 10 exchanges)
            var recentMessages = Messages.Skip(Math.Max(0, Messages.Count - 21))
                .Where(m => m.Content != "Thinking...");
            foreach (var msg in recentMessages)
            {
                sb.AppendLine(msg.IsUser ? $"User: {msg.Content}" : $"Assistant: {msg.Content}");
            }

            sb.AppendLine($"User: {userMessage}");
            sb.AppendLine("Assistant:");
            return sb.ToString();
        }
    }
}
