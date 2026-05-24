using System.Text.RegularExpressions;
using NovaBrowser.Models;

namespace NovaBrowser.Services;

public sealed class UserScriptService
{
    private const string ScriptsFile = "userscripts.json";

    private readonly DataStoreService _store;
    private List<UserScript> _scripts = [];

    public UserScriptService(DataStoreService store)
    {
        _store = store;
        _scripts = store.Load(ScriptsFile, new List<UserScript>());
    }

    public IReadOnlyList<UserScript> Scripts => _scripts;

    public UserScript AddScript(string name, string matchPattern, string script)
    {
        var entry = new UserScript
        {
            Name = name.Trim(),
            MatchPattern = string.IsNullOrWhiteSpace(matchPattern) ? "*://*/*" : matchPattern.Trim(),
            Script = script,
        };

        _scripts.Add(entry);
        Save();
        return entry;
    }

    public void RemoveScript(Guid id)
    {
        _scripts.RemoveAll(script => script.Id == id);
        Save();
    }

    public void ToggleScript(Guid id, bool enabled)
    {
        var script = _scripts.FirstOrDefault(item => item.Id == id);
        if (script is null)
        {
            return;
        }

        script.IsEnabled = enabled;
        Save();
    }

    public IEnumerable<UserScript> GetMatchingScripts(string url)
    {
        foreach (var script in _scripts.Where(script => script.IsEnabled))
        {
            if (MatchesPattern(url, script.MatchPattern))
            {
                yield return script;
            }
        }
    }

    public static bool MatchesPattern(string url, string pattern)
    {
        if (pattern == "*://*/*")
        {
            return true;
        }

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(url, regexPattern, RegexOptions.IgnoreCase);
    }

    private void Save() => _store.Save(ScriptsFile, _scripts);
}
