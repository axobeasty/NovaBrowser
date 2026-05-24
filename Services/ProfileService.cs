using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class ProfileService
{
    private const string ProfilesFile = "profiles.json";
    private const string ActiveProfileFile = "active-profile.txt";

    private readonly DataStoreService _store;
    private List<UserProfile> _profiles = [];
    private string _activeProfileId = "default";

    public ProfileService(DataStoreService store)
    {
        _store = store;
        Load();
    }

    public IReadOnlyList<UserProfile> Profiles => _profiles;

    public UserProfile ActiveProfile =>
        _profiles.FirstOrDefault(p => p.Id == _activeProfileId) ?? _profiles[0];

    public bool IsPrivateMode { get; private set; }

    public string ProfileRootDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        BrowserSettings.AppName,
        "Profiles",
        ActiveProfile.Id);

    public void ConfigureLaunchMode(bool isPrivate, string? profileId)
    {
        IsPrivateMode = isPrivate;
        if (!string.IsNullOrWhiteSpace(profileId) && _profiles.Any(p => p.Id == profileId))
        {
            _activeProfileId = profileId;
            _store.Save(ActiveProfileFile, _activeProfileId);
        }
    }

    public DataStoreService CreateProfileStore() =>
        new(Path.Combine(ProfileRootDirectory, "Data"));

    public string GetWebViewDataDirectory()
    {
        if (IsPrivateMode)
        {
            return Path.Combine(Path.GetTempPath(), "NovaBrowser-Private", Guid.NewGuid().ToString("N"));
        }

        return Path.Combine(ProfileRootDirectory, "WebView2");
    }

    public UserProfile CreateProfile(string name)
    {
        var profile = new UserProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
        };

        _profiles.Add(profile);
        SaveProfiles();
        return profile;
    }

    public void SwitchProfile(string profileId)
    {
        if (_profiles.All(p => p.Id != profileId))
        {
            return;
        }

        _activeProfileId = profileId;
        _store.Save(ActiveProfileFile, _activeProfileId);
    }

    public void DeleteProfile(string profileId)
    {
        if (_profiles.Count <= 1 || profileId == _activeProfileId)
        {
            return;
        }

        _profiles.RemoveAll(p => p.Id == profileId);
        SaveProfiles();

        var profileDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            BrowserSettings.AppName,
            "Profiles",
            profileId);

        if (Directory.Exists(profileDirectory))
        {
            try
            {
                Directory.Delete(profileDirectory, recursive: true);
            }
            catch
            {
                // Best effort.
            }
        }
    }

    private void Load()
    {
        _profiles = _store.Load(ProfilesFile, new List<UserProfile>
        {
            new() { Id = "default", Name = "Default" },
        });

        if (_profiles.Count == 0)
        {
            _profiles.Add(new UserProfile { Id = "default", Name = "Default" });
        }

        _activeProfileId = _store.Load(ActiveProfileFile, "default");
        if (_profiles.All(p => p.Id != _activeProfileId))
        {
            _activeProfileId = _profiles[0].Id;
        }
    }

    private void SaveProfiles() => _store.Save(ProfilesFile, _profiles);
}
