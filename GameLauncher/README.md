# GameLauncher

WPF 기반 게임 런처와 패치 시스템을 구현하고, 실제 Unity Windows 빌드와 연동한 프로젝트입니다.

단순 게임 실행 기능에서 시작해 버전 확인, Manifest 기반 파일 검사, SHA-256 무결성 검증, 변경 파일 다운로드, Unity 빌드 배포 자동화까지 구현했습니다.

---

## 프로젝트 구성

```text
GameLauncher
├─ GameLauncher.App
├─ GameLauncher.PatchServer
├─ GameLauncher.ManifestGenerator
├─ PatchServer
├─ UnityGame
├─ UnityGameBuild
└─ README.md
```

## GameLauncher.App

WPF + MVVM 기반 게임 런처입니다.

### 주요 기능:

- 게임 실행 파일 경로 설정
- 설정 JSON 저장 및 복원
- 현재 설치 버전 확인
- 서버 최신 버전 확인
- 업데이트 필요 여부 판단
- Manifest 기반 로컬 파일 검사
- 파일 존재 여부 / 크기 / SHA-256 비교
- 변경되거나 누락된 파일만 다운로드
- 다운로드 진행률 표시
- 패치 취소
- 임시 `.tmp` 파일을 이용한 안전한 파일 교체
- 패치 완료 후 로컬 버전 갱신
- Unity 게임 실행

GameLauncher.App 자체에 대한 세부 내용은 내부 README를 참고합니다.

## GameLauncher.PatchServer

ASP.NET Core Minimal API 기반의 로컬 테스트용 패치 서버입니다.

제공 엔드포인트:

``` text
GET /version.json
GET /manifest.json
GET /files/{filePath}
```

역할:

- 최신 게임 버전 정보 제공
- 최신 Manifest 제공
- 런처가 다운로드할 실제 게임 파일 제공

기본 테스트 주소:

```text
http://localhost:8080/
```

## PatchServer

패치 서버에서 실제로 제공할 데이터를 저장합니다.

```text 
PatchServer
├─ version.json
├─ manifest.json
└─ files
   ├─ UnityGame.exe
   ├─ UnityPlayer.dll
   ├─ UnityCrashHandler64.exe
   ├─ D3D12
   ├─ MonoBleedingEdge
   └─ UnityGame_Data
      └─ ...
```

각 항목의 역할:

- `version.json`
 - 서버의 최신 게임 버전
- `manifest.json`
 - 최신 버전을 구성하는 파일 목록
 - 파일 경로
 - 파일 크기
 - SHA-256 해시
- `files`
 - 런처가 실제로 다운로드할 최신 게임 파일

## Manifest 구조

예시:

```json
{
  "version": "1.7.0",
  "files": [
    {
      "path": "UnityPlayer.dll",
      "size": 123456,
      "hash": "ABCDEF..."
    }
  ]
}
```

런처는 Manifest 정보를 기준으로 로컬 파일과 서버 파일을 비교합니다.

## 패치 흐름

```text
Launcher 실행
    ↓
로컬 version.json 확인
    ↓
PatchServer/version.json 확인
    ↓
현재 버전 / 최신 버전 비교
    ↓
manifest.json 다운로드
    ↓
로컬 파일 검사
    ↓
파일 존재 여부 / 크기 / SHA-256 비교
    ↓
변경 또는 누락 파일 탐지
    ↓
필요한 파일만 다운로드
    ↓
.tmp 파일에 임시 저장
    ↓
다운로드 파일 SHA-256 검증
    ↓
기존 파일 교체
    ↓
로컬 version.json 갱신
    ↓
Unity 게임 실행
```

## GameLauncher.ManifestGenerator

Unity 빌드 결과를 패치 서버에서 사용할 수 있도록 자동으로 준비하는 콘솔 도구입니다.

기능:

- Unity 빌드 폴더 전체 탐색
- PatchServer/files 자동 동기화
- 기존 배포 파일 제거 후 최신 빌드 복사
- 각 파일 크기 계산
- SHA-256 해시 계산
- `manifest.json` 자동 생성
- `version.json` 자동 생성
- 버전 번호 일괄 적용
- 불필요한 파일 자동 제외

제외 대상:

```text
version.json
manifest.json
*.bak
*.tmp
*BurstDebugInformation*
```

## ManifestGenerator 실행 방법

예시:

```bat
GameLauncher.ManifestGenerator.exe C:\CSharpStudy\GameLauncher\UnityGameBuild C:\CSharpStudy\GameLauncher\PatchServer 1.7.0
```

인자:

```text
1번째: Unity 빌드 폴더
2번째: PatchServer 출력 폴더
3번째: 배포 버전
```

실행 후 자동으로:

```text
UnityGameBuild
        ↓
PatchServer/files 동기화
        ↓
manifest.json 생성
        ↓
version.json 생성
```

처리됩니다.

## UnityGame

Unity 학습 및 실제 Launcher/Patcher 연동 검증을 위해 제작한 간단한 3D 게임입니다.

구현 내용:

- WASD 이동
- Rigidbody 기반 물리 이동
- 점프
- Ground 판정
- 카메라 추적
- 마우스 Yaw / Pitch 회전
- 카메라 기준 이동
- Input System 사용
- Player Prefab
- Coin 수집
- Coin 회전
- GameManager
- TMP 기반 UI
- 게임 시작 UI
- Game Clear
- Game Over
- Hazard
- 낙하 Game Over
- Restart
- Cursor Lock 제어

Unity Windows Build 결과를 실제 GameLauncher와 연동했습니다.

## 실제 패치 테스트

다음 시나리오를 실제 Unity 빌드 파일을 대상으로 검증했습니다.

## 누락 파일 복구

로컬 `UnityPlayer.dll`을 제거한 상태에서 패치를 실행했습니다.

결과:

```text
파일 누락 감지
→ 다운로드
→ SHA-256 검증
→ 파일 복구
→ Unity 게임 정상 실행
```

## 파일 변조 복구

`UnityGame_Data/globalgamemanagers` 파일 내용을 임의로 변경한 후 패치를 실행했습니다.

결과:

```text
파일 존재
→ SHA-256 불일치 감지
→ 정상 파일 다운로드
→ 기존 파일 교체
→ Unity 게임 정상 실행
```

## 실제 버전 업데이트

Unity 게임 내용을 수정하고 새로운 빌드를 만든 뒤 새 버전으로 배포했습니다.

예:

```text
현재 버전: 1.6.0
최신 버전: 1.7.0
```

패치 후:

```text
현재 버전: 1.7.0
최신 버전: 1.7.0
```

으로 갱신되고 변경된 Unity 게임이 정상 실행되는 것을 확인했습니다.

## 개발 환경

### GameLauncher
- C#
- .NET 9
- WPF
- MVVM
- HttpClient
- System.Text.Json
- SHA-256

### PatchServer
- ASP.NET Core
- Minimal API
- .NET 9

### Unity
- Unity 6.3 LTS
- Universal 3D / URP
- Input System
- TextMesh Pro

## 학습 목표

이 프로젝트를 통해 다음 내용을 학습했습니다.

- WPF MVVM 구조
- 외부 프로세스 실행
- JSON 기반 설정 관리
- HTTP 통신
- 비동기 다운로드
- CancellationToken
- 스트림 기반 파일 다운로드
- SHA-256 파일 무결성 검사
- 임시 파일을 이용한 안전한 파일 교체
- Manifest 기반 패치 구조
- 파일 서버 구성
- Unity Windows Build 구조
- Unity 게임과 외부 Launcher 연동
- 게임 배포 파일 자동화