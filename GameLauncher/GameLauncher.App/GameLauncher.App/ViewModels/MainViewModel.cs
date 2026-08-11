using System.IO;
using System.Windows.Input;
using GameLauncher.App.Commands;
using GameLauncher.App.Models;
using GameLauncher.App.Services;
using Microsoft.Win32;

namespace GameLauncher.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Field(필드) ===================================================================================
        // Service(서비스)
        private readonly GameProcessService _gameProcessService;
        private readonly SettingsService _settingsService;
        private readonly GameVersionService _gameVersionService;
        private readonly PatchService _patchService;

        // Parameter
        private readonly GameSettings _settings;                                // 런처 세팅 정보

        private string _currentVersion = "알 수 없음";                          // 게임의 현재 버전
        private string _statusMessage = "게임을 실행할 준비가 되었습니다.";     // 게임 상태 메시지

        private string _latestVersion = "확인 전";                              // 실제 게임 최신 버전

        private PatchManifest? _currentManifest;                                // 현재 Manifest 정보
        private List<PatchFileInfo> _filesToUpdate = [];                        // 다운받아야 할 파일 정보 리스트
        private double _downloadProgress;                                       // 진행률

        private CancellationTokenSource? _patchCancellationTokenSource;         // 패치 취소 토큰
        private bool _isPatching;                                               // 패치 중 상태
        private bool _isUpdateRequired;                                         // 패치(업데이트) 필요 여부

        // Property(프로퍼티, Getter, Setter) ===================================================================================
        public string CurrentVersion
        {
            get => _currentVersion;
            set => SetProperty(ref _currentVersion, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string GameExecutablePath
        {
            get => _settings.GameExecutablePath;
            set
            {
                if (_settings.GameExecutablePath == value)
                    return;

                _settings.GameExecutablePath = value;
                OnPropertyChanged();
            }
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set => SetProperty(ref _latestVersion, value);
        }

        public double DownloadProgress
        {
            get => _downloadProgress;
            set => SetProperty(ref _downloadProgress, value);
        }

        public bool IsPatching
        {
            get => _isPatching;

            set
            {
                if (SetProperty(ref _isPatching, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsUpdateRequired
        {
            get => _isUpdateRequired;
            set
            {
                if (SetProperty(ref _isUpdateRequired, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        // Command ===================================================================================
        public ICommand PlayCommand { get; }
        public ICommand BrowseGameCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand CheckManifestCommand { get; }
        public ICommand PatchCommand { get; }
        public ICommand CancelPatchCommand { get; }

        // Constructor(생성자) ===================================================================================
        public MainViewModel()
        {
            _gameProcessService = new GameProcessService();
            _settingsService = new SettingsService();
            _gameVersionService = new GameVersionService();
            _patchService = new PatchService();

            _settings = _settingsService.Load();                                                    // 설정 JSON 확인 후 설정 정보 담기

            PlayCommand = new RelayCommand(
                _ => ExecutePlay(),
                _ => !IsPatching && !IsUpdateRequired);                                             // 게임 실행 기능
            BrowseGameCommand = new RelayCommand(
                _ => ExecuteBrowseGame(),
                _ => !IsPatching);                                                                  // 찾아보기 기능
            CheckUpdateCommand = new RelayCommand(
                async _ => await ExecuteCheckUpdateAsync(),
                _ => !IsPatching);                                                                  // 최신 버전 확인 기능
            CheckManifestCommand = new RelayCommand(
                async _ => await ExecuteCheckManifestAsync(),
                _ => !IsPatching);                                                                  // Manifest 조회 기능
            PatchCommand = new RelayCommand(
                async _ => await ExecutePatchAsync(),
                _ => !IsPatching && IsUpdateRequired);                                              // 패치 기능
            CancelPatchCommand = new RelayCommand(
                _ => ExecuteCancelPatch(),
                _ => IsPatching);                                                                   // 패치 취소 기능
        }

        // Method(메소드) ===================================================================================
        // 프로그램 실행 시 초기화 및 초기 실행
        public async Task InitializeAsync()
        {
            // 게임 실행 경로가 설정 되어있으면 버전 바로 검색
            if (!string.IsNullOrWhiteSpace(GameExecutablePath))
            {
                CurrentVersion = _gameVersionService.GetVersion(GameExecutablePath);                // 버전 체크(생성자에 있던걸 가져옴)
            }

            // 패치(업데이트) 검사
            await ExecuteCheckUpdateAsync();
        }

        // 게임 실행 파일 찾기
        private void ExecuteBrowseGame()
        {
            var dialog = new OpenFileDialog
            {
                Title = "게임 실행 파일 선택",
                Filter = "실행 파일 (*.exe)|*.exe",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                GameExecutablePath = dialog.FileName;

                _settingsService.Save(_settings);                                       // 설정 변경 사항 저장

                CurrentVersion = _gameVersionService.GetVersion(GameExecutablePath);    // 버전 체크

                StatusMessage = "게임 실행 파일을 선택하고 설정을 저장했습니다.";
            }
        }

        // 게임 실행
        private void ExecutePlay()
        {
            try
            {
                // 실행 후 실행 결과 반환
                var started =
                    _gameProcessService.StartGame(
                        GameExecutablePath,
                        _settings.GameArguments);

                StatusMessage = started
                    ? "게임을 실행했습니다."
                    : "게임 실행 파일을 찾을 수 없습니다.";
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"게임 실행 실패: {ex.Message}";
            }
        }

        // 업데이트 확인
        private async Task ExecuteCheckUpdateAsync()
        {
            try
            {
                StatusMessage = "업데이트 정보를 확인하고 있습니다.";

                // 최신 버전 파일 JSON 위치
                const string versionUrl = "http://localhost:8080/version.json";

                // 최신 버전 확인
                var remoteVersion = await _patchService.GetRemoteVersionAsync(versionUrl);

                // 버전 정보가 없다면 에러 상태 메시지 리턴
                if (remoteVersion == null)
                {
                    StatusMessage = "서버 버전 정보를 읽을 수 없습니다.";
                    return;
                }

                // 최신 버전 파라미터 갱신
                LatestVersion = remoteVersion.Version;

                // 버전 비교 후 상태 메시지 변경
                IsUpdateRequired = IsUpdateRequiredVersion(
                    CurrentVersion,
                    LatestVersion);

                if (IsUpdateRequired)
                {
                    StatusMessage =
                        $"업데이트가 필요합니다. ({CurrentVersion} → {LatestVersion})";
                }
                else
                {
                    StatusMessage = "최신 버전입니다.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"업데이트 확인 실패: {ex.Message}";
            }
        }

        // 버전 비교
        private bool IsUpdateRequiredVersion(
            string currentVersion,
            string latestVersion)
        {
            if (!Version.TryParse(currentVersion, out var current))
                return true;

            if (!Version.TryParse(latestVersion, out var latest))
                return false;

            return current < latest;
        }

        // Manifest 조회(업데이트 확인)
        private async Task ExecuteCheckManifestAsync()
        {
            try
            {
                StatusMessage = "패치 파일 목록을 확인하고 있습니다.";

                // 최신 Manifest 파일 JSON 위치
                const string manifestUrl =
                    "http://localhost:8080/manifest.json";

                // 최신 Manifest 확인
                var manifest =
                    await _patchService.GetManifestAsync(manifestUrl);

                // Manifest 정보가 없다면 에러 상태 메시지 리턴
                if (manifest == null)
                {
                    StatusMessage = "Manifest 정보를 읽을 수 없습니다.";
                    return;
                }

                _currentManifest = manifest;

                // 게임 실행 파일 값이 없으면(실행 파일 선택 안했을 경우)
                if (string.IsNullOrWhiteSpace(GameExecutablePath))
                {
                    StatusMessage = "게임 실행 파일을 먼저 선택해주세요.";
                    return;
                }

                // 게임 디렉토리 경로 확인
                string? gameDirectory =
                    Path.GetDirectoryName(GameExecutablePath);

                // 경로를 찾을 수 없는 경우
                if (string.IsNullOrWhiteSpace(gameDirectory))
                {
                    StatusMessage = "게임 폴더를 확인할 수 없습니다.";
                    return;
                }

                // 업데이트 필요한 파일 검사
                _filesToUpdate = _patchService.GetFilesToUpdate(gameDirectory, manifest);

                if (_filesToUpdate.Count == 0)  
                {
                    StatusMessage =
                        $"파일 검사 완료 - 업데이트할 파일이 없습니다. ({manifest.Files.Count}개 확인)";
                }
                else
                {
                    StatusMessage =
                        $"파일 검사 완료 - {_filesToUpdate.Count}개 파일 업데이트 필요";
                }
            }
            catch (Exception ex)
            {
                StatusMessage =
                    $"Manifest 확인 실패: {ex.Message}";
            }
        }

        // 실체 패치
        private async Task ExecutePatchAsync()
        {
            string? currentTemporaryPath = null;    // 패치 중 임시 .tmp 파일 경로

            try
            {
                if (string.IsNullOrWhiteSpace(GameExecutablePath))
                {
                    StatusMessage = "게임 실행 파일을 먼저 선택해주세요.";
                    return;
                }

                string? gameDirectory = Path.GetDirectoryName(GameExecutablePath);

                if (string.IsNullOrWhiteSpace(gameDirectory))
                {
                    StatusMessage = "게임 폴더를 확인할 수 없습니다.";
                    return;
                }

                _patchCancellationTokenSource = new CancellationTokenSource();

                IsPatching = true;
                DownloadProgress = 0;

                const string manifestUrl = "http://localhost:8080/manifest.json";

                StatusMessage = "패치 파일 목록을 확인하고 있습니다.";

                _currentManifest = await _patchService.GetManifestAsync(manifestUrl);

                if (_currentManifest == null)
                {
                    StatusMessage = "Manifest 정보를 읽을 수 없습니다.";
                    return;
                }

                _filesToUpdate = _patchService.GetFilesToUpdate(
                    gameDirectory,
                    _currentManifest);

                if (_filesToUpdate.Count == 0)
                {
                    StatusMessage = "업데이트할 파일이 없습니다.";

                    IsUpdateRequired = false;

                    return;
                }

                foreach (var patchFile in _filesToUpdate)
                {
                    StatusMessage = $"{patchFile.Path} 다운로드 중...";

                    string relativePath = patchFile.Path.Replace(
                        '/',
                        Path.DirectorySeparatorChar);
                    
                    // 다운로드 할 파일 경로 설정 및 tmp 파일 경로 설정
                    string destinationPath = Path.Combine(gameDirectory, relativePath);
                    currentTemporaryPath = destinationPath + ".tmp";

                    string downloadUrl = $"http://localhost:8080/files/{patchFile.Path}";

                    var progress = new Progress<double>(value =>
                    {
                        DownloadProgress = value;

                        StatusMessage =
                            $"{patchFile.Path} 다운로드 중... {value:F0}%";
                    });

                    // 다운로드는 .tmp 확장자로 진행(패치 도중 취소 시 원본 파일 훼손을 막기위함)
                    await _patchService.DownloadFileAsync(
                        downloadUrl,
                        currentTemporaryPath,
                        progress,
                        _patchCancellationTokenSource.Token);

                    // 해시 검증
                    bool isValid = _patchService.VerifyFileHash(
                        currentTemporaryPath,
                        patchFile.Hash);

                    if (!isValid)
                    {
                        File.Delete(currentTemporaryPath);

                        throw new InvalidOperationException(
                            $"{patchFile.Path} 파일 무결성 검사에 실패했습니다.");
                    }

                    // .tmp 확장자로 다운로드가 정상적으로 완료되면 .tmp 확장자 제거하여 원본을 교체
                    File.Move(
                        currentTemporaryPath,
                        destinationPath,
                        true);

                    currentTemporaryPath = null;
                }

                // 패치 완료
                _gameVersionService.SaveVersion(
                    GameExecutablePath,
                    _currentManifest.Version);

                CurrentVersion = _currentManifest.Version;
                LatestVersion = _currentManifest.Version;
                IsUpdateRequired = false;

                _filesToUpdate.Clear();

                DownloadProgress = 100;

                StatusMessage = $"패치가 완료되었습니다. 버전 {CurrentVersion}";
            }
            catch (OperationCanceledException)
            {
                if (!string.IsNullOrWhiteSpace(currentTemporaryPath)
                    && File.Exists(currentTemporaryPath))
                {
                    File.Delete(currentTemporaryPath);
                }

                StatusMessage = "패치가 취소되었습니다.";
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(currentTemporaryPath)
                    && File.Exists(currentTemporaryPath))
                {
                    File.Delete(currentTemporaryPath);
                }

                StatusMessage = $"패치 실패: {ex.Message}";
            }
            finally
            {
                IsPatching = false;

                _patchCancellationTokenSource?.Dispose();
                _patchCancellationTokenSource = null;
            }
        }

        // 패치 취소
        private void ExecuteCancelPatch()
        {
            if (_patchCancellationTokenSource == null)
            {
                StatusMessage = "현재 진행 중인 패치가 없습니다.";
                return;
            }

            StatusMessage = "패치 취소 요청 중...";
            _patchCancellationTokenSource.Cancel();
        }
    }

}