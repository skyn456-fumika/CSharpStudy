# DevDesk

Windows 환경에서 개발자가 자주 사용하는 작업을 한곳에서 관리할 수 있도록 만든 WPF 기반 데스크톱 관리 도구입니다.

프로세스 관리, HTTP·TCP 상태 검사, 주기적 서버 감시, 로그 파일 실시간 감시, 검사 이력 저장, JSON 설정 저장, 트레이 실행과 상태 알림 기능을 제공합니다.

## 주요 기능

### 프로세스 관리

- 실행 중인 프로세스 목록 조회
- 프로세스 이름 및 PID 검색
- PID, 메모리 사용량, 시작 시각 표시
- 실행 파일 선택 및 실행
- 선택한 프로세스 종료
- DevDesk 자기 자신 종료 방지

### HTTP 상태 검사

- URL 기반 HTTP 연결 검사
- 응답 상태 코드 표시
- 응답 시간 측정
- 타임아웃 및 오류 메시지 처리
- 검사 결과 SQLite 저장

### TCP 연결 검사

- 호스트와 포트 기반 TCP 연결 검사
- 연결 성공 여부와 응답 시간 표시
- 잘못된 호스트 및 포트 입력 검증
- 검사 결과 SQLite 저장

### 자동 서버 감시

- 설정한 주기마다 HTTP와 TCP 상태 자동 검사
- `PeriodicTimer`와 `CancellationToken` 기반 백그라운드 처리
- 정상, 장애, 복구 상태 변화 감지
- 장애 및 복구 시 Windows 트레이 알림
- 중복 상태 알림 방지

### 로그 파일 감시

- 로그 파일 선택 및 기존 내용 불러오기
- `FileSystemWatcher` 기반 실시간 추가 내용 감지
- 로그 검색
- 최근 로그 자동 스크롤
- 최대 표시 줄 수 제한
- 로그 감시 시작 및 중지

### 검사 이력

- HTTP 및 TCP 검사 이력 SQLite 저장
- 전체, HTTP, TCP 유형별 필터
- 최근 검사 결과 조회
- 선택한 이력 상세 확인
- 전체 이력 삭제

### 설정 저장

- HTTP URL
- TCP 호스트 및 포트
- 자동 감시 주기
- 로그 파일 경로
- 최대 로그 표시 줄 수
- 서버 장애 및 복구 알림 사용 여부

설정은 JSON 파일로 저장되며 애플리케이션을 다시 실행해도 복원됩니다.

### 트레이 기능

- 창 최소화 시 시스템 트레이로 이동
- 창 닫기 버튼 클릭 시 백그라운드 실행 유지
- 트레이 아이콘 더블 클릭 또는 메뉴를 통한 창 복원
- 트레이 메뉴를 통한 완전 종료
- DevDesk 전용 애플리케이션 아이콘 적용

## 기술 스택

- C#
- .NET 9
- WPF
- MVVM
- Microsoft.Data.Sqlite
- SQLite
- JSON
- FileSystemWatcher
- HttpClient
- TcpClient
- Windows Forms NotifyIcon

## 프로젝트 구조

```text
DevDesk
├─ DevDesk.sln
├─ README.md
└─ DevDesk.App
   ├─ Commands
   │  ├─ AsyncRelayCommand.cs
   │  └─ RelayCommand.cs
   ├─ Data
   │  └─ DevDeskDatabase.cs
   ├─ Models
   ├─ Resources
   │  └─ DevDesk.ico
   ├─ Services
   ├─ ViewModels
   │  ├─ MainViewModel.cs
   │  └─ ViewModelBase.cs
   ├─ Views
   ├─ App.xaml
   ├─ App.xaml.cs
   ├─ MainWindow.xaml
   ├─ MainWindow.xaml.cs
   └─ DevDesk.App.csproj
```

## 데이터 저장 위치

DevDesk는 사용자별 로컬 애플리케이션 데이터 폴더에 파일을 저장합니다.

```text
%LocalAppData%\DevDesk
```

생성되는 주요 파일:

```text
devdesk.db
settings.json
```

- `devdesk.db`: HTTP 및 TCP 검사 이력
- `settings.json`: 사용자가 저장한 애플리케이션 설정

## 실행 방법

### 요구 사항

- Windows 10 이상
- .NET 9 SDK
- Visual Studio 2022 또는 .NET CLI

### Visual Studio

1. `DevDesk.sln`을 엽니다.
2. `DevDesk.App`을 시작 프로젝트로 설정합니다.
3. `Ctrl + F5` 또는 `F5`로 실행합니다.

### .NET CLI

```powershell
cd C:\CSharpStudy\DevDesk
dotnet restore
dotnet build
dotnet run --project DevDesk.App
```

## 사용 방법

1. 프로세스 화면에서 현재 실행 중인 프로세스를 조회하거나 실행 및 종료합니다.
2. HTTP 화면에서 검사할 URL을 입력한 뒤 상태를 확인합니다.
3. TCP 화면에서 호스트와 포트를 입력한 뒤 연결 여부를 확인합니다.
4. 자동 감시 화면에서 검사 주기를 설정하고 서버 감시를 시작합니다.
5. 로그 화면에서 파일을 선택하고 실시간 감시를 시작합니다.
6. 이력 화면에서 저장된 검사 결과를 조회합니다.
7. 설정 화면에서 기본값과 알림 사용 여부를 저장합니다.
8. 창을 최소화하거나 닫으면 DevDesk가 시스템 트레이에서 계속 실행됩니다.

## 예외 처리

- 잘못된 URL 입력
- 잘못된 TCP 포트 입력
- 존재하지 않는 로그 파일
- HTTP 및 TCP 연결 시간 초과
- 이미 종료된 프로세스 접근
- DevDesk 자기 자신 종료 요청
- 로그 파일 변경 및 접근 오류
- 애플리케이션 종료 시 백그라운드 작업 정리

## 개발 과정에서 해결한 주요 문제

- WPF와 Windows Forms의 `Application`, `MessageBox`, `OpenFileDialog` 타입 충돌
- Avast가 `apphost.exe`를 격리해 발생한 `CreateAppHost` 접근 거부 오류
- 비동기 종료 시 자동 감시 및 로그 감시 작업 정리
- 로그 파일 추가 내용만 읽기 위한 파일 위치 추적
- 트레이 종료와 창 닫기 동작 분리
- 서버 상태 변화 시 중복 알림 방지
- SQLite 검사 이력과 대시보드 최근 이력 동기화
- 전용 실행 파일 아이콘을 트레이 아이콘으로 재사용

## 향후 개선 사항

- 여러 HTTP 및 TCP 대상 동시 관리
- 개별 서버별 감시 주기 설정
- 검사 결과 차트와 통계
- 로그 수준별 색상 표시
- 로그 정규식 검색
- CSV 및 JSON 이력 내보내기
- Windows 시작 시 자동 실행
- 테마 전환 및 다크 모드
- 설치 파일 배포

## 라이선스

학습 및 포트폴리오 목적으로 제작한 프로젝트입니다.
