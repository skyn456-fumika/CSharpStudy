# GameLauncher / Patcher

WPF와 .NET 9 기반으로 제작한 게임 런처 및 패처 학습 프로젝트입니다.

게임 실행 파일 관리부터 버전 확인, Manifest 기반 파일 비교, HTTP 다운로드, SHA-256 무결성 검증, 패치 취소 및 임시 파일 교체까지 실제 게임 런처의 기본적인 패치 흐름을 구현했습니다.

## 기술 스택

* C#
* .NET 9
* WPF
* MVVM
* ASP.NET Core Minimal API
* HttpClient
* System.Text.Json
* SHA-256

## 프로젝트 구조

```text
GameLauncher
├─ GameLauncher.App
│  ├─ Commands
│  ├─ Models
│  ├─ Services
│  ├─ ViewModels
│  ├─ Views
│  └─ Resources
│
└─ GameLauncher.PatchServer
```

### GameLauncher.App

WPF 기반 게임 런처 및 패처 클라이언트입니다.

### GameLauncher.PatchServer

버전 정보, Manifest 및 테스트용 패치 파일을 제공하는 ASP.NET Core Minimal API 서버입니다.

## 주요 기능

### 게임 실행 관리

* 게임 실행 파일 선택
* 선택한 실행 파일 경로 저장
* 런처 재실행 시 설정 자동 복원
* Process를 이용한 게임 실행

### 버전 관리

* 로컬 `version.json` 조회
* HTTP를 통한 서버 최신 버전 조회
* 현재 버전과 최신 버전 비교
* 업데이트 필요 여부 자동 판단
* 패치 성공 후 로컬 버전 파일 갱신

### Manifest 기반 패치

서버의 Manifest를 기준으로 로컬 게임 파일을 검사합니다.

검사 기준:

1. 파일 존재 여부
2. 파일 크기
3. SHA-256 Hash

필요한 파일만 업데이트 대상으로 선정합니다.

### 파일 다운로드

* HttpClient 기반 HTTP 다운로드
* 스트림 방식 다운로드
* 다운로드 진행률 표시
* 하위 디렉터리 자동 생성

### 무결성 검증

다운로드 후 SHA-256 Hash를 다시 계산하여 Manifest의 Hash와 비교합니다.

검증 실패 시 해당 파일을 적용하지 않고 패치를 실패 처리합니다.

### 안전한 파일 교체

패치 파일을 기존 파일에 직접 덮어쓰지 않고 `.tmp` 파일로 먼저 다운로드합니다.

```text
파일 다운로드
→ .tmp 생성
→ SHA-256 검증
→ 검증 성공
→ 실제 파일로 교체
```

패치 실패 또는 취소 시 임시 파일을 삭제하여 기존 정상 파일을 보호합니다.

### 패치 취소

CancellationToken을 이용하여 진행 중인 다운로드를 취소할 수 있습니다.

패치 중에는 다른 주요 기능을 비활성화하고 패치 취소 기능만 사용할 수 있도록 Command의 CanExecute 상태를 제어합니다.

### 자동 업데이트 확인

런처 실행 시 서버의 최신 버전을 자동으로 조회합니다.

업데이트가 필요한 경우 패치 기능을 활성화하고, 최신 버전일 경우 게임 실행 기능을 활성화합니다.

## 패치 흐름

```text
런처 실행
    ↓
로컬 버전 확인
    ↓
서버 최신 버전 조회
    ↓
업데이트 여부 판단
    ↓
Manifest 조회
    ↓
로컬 파일 검사
    ↓
업데이트 대상 계산
    ↓
.tmp 파일 다운로드
    ↓
SHA-256 검증
    ↓
실제 파일 교체
    ↓
version.json 갱신
    ↓
게임 실행
```

## 테스트

테스트용 게임 실행 파일로 이전 C# 프로젝트의 `TestGameServer.exe`를 사용했습니다.

PatchServer에서는 테스트용 패치 데이터를 제공하여 다음 상황을 확인했습니다.

* 최신 버전 / 구버전 비교
* 파일 누락
* 파일 크기 불일치
* 같은 크기에서 Hash 불일치
* 정상 패치
* 무결성 검사 실패
* 패치 취소
* `.tmp` 파일 정리
* PatchServer 연결 실패
* 런처 재실행 후 버전 유지

## 학습 내용

이 프로젝트를 통해 다음 내용을 학습했습니다.

* WPF와 MVVM 구조
* ICommand / RelayCommand
* CanExecute 기반 UI 상태 제어
* 비동기 프로그래밍과 async/await
* HttpClient 스트림 다운로드
* IProgress를 이용한 진행률 처리
* CancellationToken
* JSON 직렬화 / 역직렬화
* 파일 및 디렉터리 처리
* SHA-256 파일 무결성 검사
* 임시 파일을 이용한 안전한 패치 처리
* ASP.NET Core Minimal API 기반 테스트 서버

## 향후 확장 가능 기능

* 실제 Unity 게임 빌드 연동
* 여러 게임 파일로 구성된 실제 Manifest 생성
* 패치 서버 파일 자동 스캔
* 전체 다운로드 용량 및 속도 표시
* 남은 시간 계산
* 패치 재시도
* 압축 패치
* CDN / 실제 원격 서버 적용
* 런처 자체 업데이트
* 패치 노트 및 공지사항
* 게임 테마에 맞춘 UI 디자인
