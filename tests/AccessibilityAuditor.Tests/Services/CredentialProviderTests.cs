using AccessibilityAuditor.Services.LLM;

namespace AccessibilityAuditor.Tests.Services;

public class CredentialProviderTests
{
    /// <summary>
    /// In-memory fake — never touches the real Windows Credential Manager.
    /// Tests verify the interface contract, not the P/Invoke implementation.
    /// </summary>
    private sealed class FakeCredentialProvider : CredentialProvider
    {
        private readonly Dictionary<LLMProviderType, string> _store = new();

        public override void Store(LLMProviderType provider, string key)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            _store[provider] = key;
        }

        public override string? Retrieve(LLMProviderType provider) =>
            _store.TryGetValue(provider, out var key) ? key : null;

        public override bool IsConfigured(LLMProviderType provider) =>
            _store.ContainsKey(provider);

        public override void Delete(LLMProviderType provider) =>
            _store.Remove(provider);
    }

    private readonly FakeCredentialProvider _creds = new();

    #region Store and Retrieve

    [Fact]
    public void Store_ThenRetrieve_ReturnsKey()
    {
        _creds.Store(LLMProviderType.Anthropic, "sk-ant-test123");

        var retrieved = _creds.Retrieve(LLMProviderType.Anthropic);

        Assert.Equal("sk-ant-test123", retrieved);
    }

    [Fact]
    public void Retrieve_NothingStored_ReturnsNull()
    {
        var result = _creds.Retrieve(LLMProviderType.OpenAI);

        Assert.Null(result);
    }

    [Fact]
    public void Store_OverwritesPreviousKey()
    {
        _creds.Store(LLMProviderType.Anthropic, "key-1");
        _creds.Store(LLMProviderType.Anthropic, "key-2");

        Assert.Equal("key-2", _creds.Retrieve(LLMProviderType.Anthropic));
    }

    #endregion

    #region IsConfigured

    [Fact]
    public void IsConfigured_NotStored_ReturnsFalse()
    {
        Assert.False(_creds.IsConfigured(LLMProviderType.Anthropic));
    }

    [Fact]
    public void IsConfigured_Stored_ReturnsTrue()
    {
        _creds.Store(LLMProviderType.OpenAI, "sk-test");

        Assert.True(_creds.IsConfigured(LLMProviderType.OpenAI));
    }

    #endregion

    #region Delete

    [Fact]
    public void Delete_RemovesKey()
    {
        _creds.Store(LLMProviderType.Anthropic, "sk-test");
        Assert.True(_creds.IsConfigured(LLMProviderType.Anthropic));

        _creds.Delete(LLMProviderType.Anthropic);

        Assert.False(_creds.IsConfigured(LLMProviderType.Anthropic));
        Assert.Null(_creds.Retrieve(LLMProviderType.Anthropic));
    }

    [Fact]
    public void Delete_WhenNotStored_DoesNotThrow()
    {
        var ex = Record.Exception(() => _creds.Delete(LLMProviderType.OpenAI));

        Assert.Null(ex);
    }

    #endregion

    #region Provider Isolation

    [Fact]
    public void DifferentProviders_AreSeparate()
    {
        _creds.Store(LLMProviderType.Anthropic, "anthropic-key");
        _creds.Store(LLMProviderType.OpenAI, "openai-key");

        Assert.Equal("anthropic-key", _creds.Retrieve(LLMProviderType.Anthropic));
        Assert.Equal("openai-key", _creds.Retrieve(LLMProviderType.OpenAI));

        _creds.Delete(LLMProviderType.Anthropic);

        Assert.Null(_creds.Retrieve(LLMProviderType.Anthropic));
        Assert.Equal("openai-key", _creds.Retrieve(LLMProviderType.OpenAI));
    }

    #endregion
}
