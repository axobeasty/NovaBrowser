using NovaBrowser.Models;
using Windows.Security.Credentials;

namespace NovaBrowser.Services;

public sealed class PasswordService
{
    private const string VaultResourcePrefix = "NovaBrowser:";
    private readonly DataStoreService _store;
    private List<SavedPasswordEntry> _entries = [];

    public PasswordService(DataStoreService store)
    {
        _store = store;
        _entries = store.Load("passwords-index.json", new List<SavedPasswordEntry>());
    }

    public IReadOnlyList<SavedPasswordEntry> Entries => _entries;

    public void SavePassword(string site, string username, string password)
    {
        var resource = $"{VaultResourcePrefix}{Guid.NewGuid():N}";
        var vault = new PasswordVault();
        vault.Add(new PasswordCredential(resource, username, password));

        _entries.Add(new SavedPasswordEntry
        {
            Site = site,
            Username = username,
            ResourceKey = resource,
        });

        _store.Save("passwords-index.json", _entries);
    }

    public string? GetPassword(string resourceKey)
    {
        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(resourceKey, _entries.First(entry => entry.ResourceKey == resourceKey).Username);
            credential.RetrievePassword();
            return credential.Password;
        }
        catch
        {
            return null;
        }
    }

    public void DeletePassword(Guid id)
    {
        var entry = _entries.FirstOrDefault(item => item.Id == id);
        if (entry is null)
        {
            return;
        }

        try
        {
            var vault = new PasswordVault();
            var credential = vault.Retrieve(entry.ResourceKey, entry.Username);
            vault.Remove(credential);
        }
        catch
        {
            // Best effort.
        }

        _entries.Remove(entry);
        _store.Save("passwords-index.json", _entries);
    }
}
