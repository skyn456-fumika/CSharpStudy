using GameServerManager.App.Models;
using System.IO;
using System.Text.Json;

namespace GameServerManager.App.Services;

public class AppSettingsService
{
    private readonly string _settingsPath;

    private readonly JsonSerializerOptions _jsonOptions =
        new()
        {
            WriteIndented = true
        };

    public AppSettingsService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "GameServerManager");

        Directory.CreateDirectory(directory);

        _settingsPath =
            Path.Combine(
                directory,
                "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json =
                await File.ReadAllTextAsync(
                    _settingsPath);

            return JsonSerializer.Deserialize<AppSettings>(
                       json,
                       _jsonOptions)
                   ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(
        AppSettings settings)
    {
        var json =
            JsonSerializer.Serialize(
                settings,
                _jsonOptions);

        await File.WriteAllTextAsync(
            _settingsPath,
            json);
    }
}