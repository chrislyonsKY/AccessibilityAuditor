using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AccessibilityAuditor.Services.LLM;

namespace AccessibilityAuditor.ViewModels
{
    /// <summary>
    /// ViewModel for the AI Settings tab in the AccessibilityAuditor dockpane.
    /// Manages API key entry, provider selection, connection testing, and key removal.
    /// The key value is never stored as a ViewModel property — only passed directly
    /// from the PasswordBox to <see cref="CredentialProvider.Store"/> on save.
    /// </summary>
    internal sealed partial class LLMSettingsViewModel : ObservableObject
    {
        private readonly CredentialProvider _credentials;
        private readonly ILLMProvider[] _providers;

        /// <summary>Initialises the ViewModel with required services.</summary>
        public LLMSettingsViewModel(
            CredentialProvider credentials,
            params ILLMProvider[] providers)
        {
            _credentials = credentials;
            _providers = providers;
            RefreshKeyStatus();
        }

        private LLMProviderType _selectedProvider = LLMProviderType.Anthropic;
        /// <summary>Currently selected provider.</summary>
        public LLMProviderType SelectedProvider
        {
            get => _selectedProvider;
            set
            {
                if (SetProperty(ref _selectedProvider, value))
                {
                    RefreshKeyStatus();
                    StatusMessage = string.Empty;
                }
            }
        }

        private bool _isKeyConfigured;
        /// <summary>
        /// Whether a key is configured for the currently selected provider.
        /// Bound to the status indicator and command CanExecute.
        /// </summary>
        public bool IsKeyConfigured
        {
            get => _isKeyConfigured;
            private set
            {
                if (SetProperty(ref _isKeyConfigured, value))
                {
                    TestConnectionCommand.NotifyCanExecuteChanged();
                    RemoveKeyCommand.NotifyCanExecuteChanged();
                }
            }
        }

        private string _statusMessage = string.Empty;
        /// <summary>Status message shown below the key entry area.</summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isTesting;
        /// <summary>Whether a test connection is in progress.</summary>
        public bool IsTesting
        {
            get => _isTesting;
            set => SetProperty(ref _isTesting, value);
        }

        /// <summary>
        /// Saves the provided API key to Windows Credential Manager.
        /// The key is passed directly from the PasswordBox — never stored as a property.
        /// </summary>
        public void SaveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                StatusMessage = "Please enter an API key.";
                return;
            }

            try
            {
                _credentials.Store(SelectedProvider, key.Trim());
                RefreshKeyStatus();
                StatusMessage = "Key saved.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to save key: {ex.Message}";
                Debug.WriteLine($"Credential store failed: {ex}");
            }
        }

        private RelayCommand? _testConnectionCommand;
        /// <summary>Tests the connection to the selected provider.</summary>
        public RelayCommand TestConnectionCommand => _testConnectionCommand ??= new RelayCommand(
            async () => await TestConnectionAsync(),
            () => IsKeyConfigured && !IsTesting);

        private async Task TestConnectionAsync()
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderType == SelectedProvider);
            if (provider is null)
            {
                StatusMessage = "Provider not available.";
                return;
            }

            IsTesting = true;
            StatusMessage = "Testing connection...";

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var response = await provider.CompleteAsync("Say OK.", cts.Token);
                StatusMessage = "Connection successful.";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Connection test timed out.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Connection failed: {ex.Message}";
                Debug.WriteLine($"Connection test failed: {ex}");
            }
            finally
            {
                IsTesting = false;
            }
        }

        private RelayCommand? _removeKeyCommand;
        /// <summary>Removes the stored key for the selected provider.</summary>
        public RelayCommand RemoveKeyCommand => _removeKeyCommand ??= new RelayCommand(
            RemoveKey,
            () => IsKeyConfigured);

        private void RemoveKey()
        {
            _credentials.Delete(SelectedProvider);
            RefreshKeyStatus();
            StatusMessage = "Key removed.";
        }

        private void RefreshKeyStatus()
        {
            IsKeyConfigured = _credentials.IsConfigured(SelectedProvider);
        }
    }
}
