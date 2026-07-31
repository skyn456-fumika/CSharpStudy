using System.IO;
using System.Text.Json;
using DevDesk.App.Models;

namespace DevDesk.App.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public SettingsService()
    {
        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "DevDesk");

        Directory.CreateDirectory(dataDirectory);

        _settingsFilePath = Path.Combine(
            dataDirectory,
            "settings.json");
    }

    public async Task<AppSettingsModel> LoadAsync()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppSettingsModel();
        }

        try
        {
            await using var stream = new FileStream(
                _settingsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var settings =
                await JsonSerializer.DeserializeAsync<AppSettingsModel>(
                    stream,
                    _jsonOptions);

            return settings ?? new AppSettingsModel();
        }
        catch (JsonException)
        {
            return new AppSettingsModel();
        }
        catch (IOException)
        {
            return new AppSettingsModel();
        }
    }

    public async Task SaveAsync(AppSettingsModel settings)
    {
        var tempFilePath = _settingsFilePath + ".tmp";

        await using (var stream = new FileStream(
            tempFilePath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                settings,
                _jsonOptions);
        }

        File.Move(
            tempFilePath,
            _settingsFilePath,
            overwrite: true);
    }
}