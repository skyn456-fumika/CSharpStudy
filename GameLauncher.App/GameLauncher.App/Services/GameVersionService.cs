using System.IO;
using System.Text.Json;
using GameLauncher.App.Models;

namespace GameLauncher.App.Services
{
    public class GameVersionService
    {
        // 버전 조회
        public string GetVersion(string executablePath)
        {
            // 설치 유무 확인
            if (string.IsNullOrWhiteSpace(executablePath))
                return "미설치";

            string? gameDirectory = Path.GetDirectoryName(executablePath);

            // 경로 유무 확인
            if (string.IsNullOrWhiteSpace(gameDirectory))
                return "미설치";

            string versionPath = Path.Combine(gameDirectory, "version.json");

            // 버전 JSON 파일 존재 유무 확인
            if (!File.Exists(versionPath))
                return "알 수 없음";

            try
            {
                string json = File.ReadAllText(versionPath);

                GameVersionInfo? versionInfo =
                    JsonSerializer.Deserialize<GameVersionInfo>(json);

                // 버전 값 반환(값이 없다면 "알 수 없음" 반환
                return versionInfo?.Version ?? "알 수 없음";
            }
            catch
            {
                return "알 수 없음";
            }
        }

        // 버전 저장
        public void SaveVersion(string executablePath, string version)
        {
            //  경로 확인 후 빈 값이면 리턴
            if (string.IsNullOrWhiteSpace(executablePath))
                return;

            // 디렉토리 경로 조회
            string? gameDirectory = Path.GetDirectoryName(executablePath);

            // 디렉토리 경로 확ㅇ니 후 빈 값이면 리턴
            if (string.IsNullOrWhiteSpace(gameDirectory))
                return;

            // 버전 JSON 파일 경로
            string versionPath = Path.Combine(gameDirectory, "version.json");

            // 버전 정보 객체(인스턴스) 생성
            var versionInfo = new GameVersionInfo
            {
                Version = version
            };

            // 옵션 JSON 객체(인스턴스) 생성
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            // 버전 JSON 변환
            string json = JsonSerializer.Serialize(versionInfo, options);

            // 버전 JSON 저장
            File.WriteAllText(versionPath, json);
        }
    }
}