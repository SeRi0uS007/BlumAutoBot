using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlumBot;

public enum BlumPlatform
{
    iOS15,
    iOS16,
    Android,
    Windows,
    MacOS
}

public class Settings
{
    public uint MinScore { get; set; } = 170;
    public uint MaxScore { get; set; } = 250;
    public string AuthorizationToken { get; set; } = string.Empty;
    public BlumPlatform Platform { get; set; } = BlumPlatform.Windows;

    public static Settings? ReadSettings(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (!Path.Exists(path))
            return null;

        using (FileStream file = new(path, FileMode.Open, FileAccess.Read))
        {
            Settings? settings;
            try
            {
                settings = JsonSerializer.Deserialize(file, SourceGenerationContext.Default.Settings);
            }
            catch (JsonException)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(settings?.AuthorizationToken))
                return null;

            return settings;
        }
    }

    public static Settings[] ReadSettings()
    {
        var processPath = Environment.ProcessPath;
        var processFolder = Path.GetDirectoryName(processPath);
        if (string.IsNullOrEmpty(processFolder))
            return Array.Empty<Settings>();

        var settingsPath = Path.Combine(processFolder, "accounts");
        if (!Directory.Exists(settingsPath))
            return Array.Empty<Settings>();

        return Directory.GetFiles(settingsPath).Select(x => ReadSettings(x)).Where(x => x != null).ToArray()!;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(Settings))]
internal partial class SourceGenerationContext: JsonSerializerContext
{
}
