using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AccessibilityAuditor.Services.LLM
{
    /// <summary>
    /// Reads and writes API keys to Windows Credential Manager via P/Invoke.
    /// This is the ONLY class permitted to access credentials directly.
    /// Keys must never be logged, cached in fields, or exposed outside this class
    /// except as a return value to the immediate caller.
    /// </summary>
    public class CredentialProvider
    {
        private const string TargetPrefix = "AccessibilityAuditor";
        private const uint CRED_TYPE_GENERIC = 1;
        private const uint CRED_PERSIST_LOCAL_MACHINE = 2;

        /// <summary>
        /// Stores an API key in Windows Credential Manager.
        /// Target name: <c>AccessibilityAuditor/{provider}</c>.
        /// </summary>
        /// <param name="provider">The provider the key belongs to.</param>
        /// <param name="key">The API key value. Never log this parameter.</param>
        public virtual void Store(LLMProviderType provider, string key)
        {
            var target = TargetName(provider);
            var keyBytes = Encoding.Unicode.GetBytes(key);

            var credential = new CREDENTIAL
            {
                Type = CRED_TYPE_GENERIC,
                TargetName = target,
                CredentialBlobSize = (uint)keyBytes.Length,
                CredentialBlob = Marshal.AllocHGlobal(keyBytes.Length),
                Persist = CRED_PERSIST_LOCAL_MACHINE,
                UserName = provider.ToString()
            };

            try
            {
                Marshal.Copy(keyBytes, 0, credential.CredentialBlob, keyBytes.Length);
                if (!CredWriteW(ref credential, 0))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new InvalidOperationException(
                        $"Failed to store credential for {provider} (Win32 error {error}).");
                }

                Debug.WriteLine($"Credential stored for provider {provider}.");
            }
            finally
            {
                Marshal.FreeHGlobal(credential.CredentialBlob);
            }
        }

        /// <summary>
        /// Retrieves an API key from Windows Credential Manager.
        /// Returns <c>null</c> if no key is configured for this provider.
        /// </summary>
        public virtual string? Retrieve(LLMProviderType provider)
        {
            var target = TargetName(provider);
            if (!CredReadW(target, CRED_TYPE_GENERIC, 0, out var credPtr))
                return null;

            try
            {
                var cred = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                if (cred.CredentialBlobSize == 0 || cred.CredentialBlob == IntPtr.Zero)
                    return null;

                return Marshal.PtrToStringUni(
                    cred.CredentialBlob,
                    (int)cred.CredentialBlobSize / sizeof(char));
            }
            finally
            {
                CredFree(credPtr);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if a key is configured for the given provider.
        /// Safe to call from the UI thread to determine button state.
        /// </summary>
        public virtual bool IsConfigured(LLMProviderType provider) =>
            Retrieve(provider) is not null;

        /// <summary>
        /// Removes the stored key for the given provider from Windows Credential Manager.
        /// </summary>
        public virtual void Delete(LLMProviderType provider)
        {
            var target = TargetName(provider);
            CredDeleteW(target, CRED_TYPE_GENERIC, 0);
            Debug.WriteLine($"Credential removed for provider {provider}.");
        }

        private static string TargetName(LLMProviderType provider) =>
            $"{TargetPrefix}/{provider}";

        #region Win32 P/Invoke

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public long LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWriteW(ref CREDENTIAL credential, uint flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredReadW(
            string target, uint type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDeleteW(string target, uint type, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr buffer);

        #endregion
    }
}
