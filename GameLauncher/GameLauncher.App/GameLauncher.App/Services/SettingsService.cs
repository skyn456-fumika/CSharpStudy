using System.IO;
using System.Text.Json;
using GameLauncher.App.Models;

namespace GameLauncher.App.Services
{
    public class SettingsService
    {
        private readonly string _settingsPath;

        public SettingsService()
        {
            // 세팅 JSON 저장 베이스 경로를 exe와 동일 위치로
            _settingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "launcher-settings.json");
        }

        public GameSettings Load()
        {
            // 세팅 JSON 파일 존재 여부 검사 후 없으면 빈 인스턴스 생성 후 반환
            if (!File.Exists(_settingsPath))
                return new GameSettings();

            // 존재하면 GameSettings으로 Deserialize 후 반환
            try
            {
                string json = File.ReadAllText(_settingsPath);

                return JsonSerializer.Deserialize<GameSettings>(json)
                    ?? new GameSettings();
            }
            catch
            {
                return new GameSettings();
            }
        }

        // GameSettins을 JSON로 변환 후 베이스 경로에 저장/변경
        public void Save(GameSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(settings, options);

            File.WriteAllText(_settingsPath, json);
        }
    }
}