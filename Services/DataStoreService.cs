using System.Text.Json;

namespace NovaBrowser.Services;

public sealed class DataStoreService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _rootDirectory;

    public DataStoreService(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
        Directory.CreateDirectory(_rootDirectory);
    }

    public string GetFilePath(string fileName) => Path.Combine(_rootDirectory, fileName);

    public T Load<T>(string fileName, T defaultValue)
    {
        var path = GetFilePath(fileName);
        if (!File.Exists(path))
        {
            return defaultValue;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public void Save<T>(string fileName, T value)
    {
        var path = GetFilePath(fileName);
        var json = JsonSerializer.Serialize(value, JsonOptions);
        File.WriteAllText(path, json);
    }

    public void Delete(string fileName)
    {
        var path = GetFilePath(fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
