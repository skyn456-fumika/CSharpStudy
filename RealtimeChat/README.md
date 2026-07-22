# RealtimeChat

ASP.NET Core SignalR와 Blazor WebAssembly를 사용해 만든 실시간 다중 채팅방 애플리케이션입니다.

## 프로젝트 구성

```text
RealtimeChat/
├─ RealtimeChat.Server/   # ASP.NET Core SignalR 서버
├─ RealtimeChat.Web/      # Blazor WebAssembly 클라이언트
├─ RealtimeChat.sln
└─ README.md
```

## 기술 스택

- C#
- .NET 9
- ASP.NET Core
- SignalR
- Blazor WebAssembly
- Entity Framework Core
- SQLite
- Bootstrap

## 주요 기능

- 실시간 채팅
- 다중 채팅방
- 연결을 유지한 상태에서 채팅방 이동
- 방별 접속자 목록
- 입장 및 퇴장 알림
- 같은 방 내 중복 닉네임 방지
- 귓속말
- Enter 키 메시지 전송
- 메시지 자동 스크롤
- 전송 중 중복 요청 방지
- 방별 최근 메시지 50개 조회
- SQLite 채팅 기록 저장
- 서버 재시작 후 이전 메시지 복원
- SignalR 자동 재연결 및 채팅방 재입장

## 채팅방

- 일반
- 게임
- 잡담

## 실행 주소

- Server: https://localhost:7178
- Web: https://localhost:7170

## 실행 방법

1. RealtimeChat.Server 프로젝트를 실행합니다.
2. RealtimeChat.Web 프로젝트를 실행합니다.
3. 브라우저에서 https://localhost:7170/chat에 접속합니다.
4. 닉네임과 채팅방을 선택한 뒤 채팅에 연결합니다.

## 데이터베이스

채팅 메시지는 SQLite에 저장됩니다.

```text
realtime-chat.db
```

새로운 환경에서 데이터베이스를 생성하려면 RealtimeChat.Server 폴더에서 다음 명령어를 실행합니다.

```shell
dotnet ef database update
```

마이그레이션 파일은 Git에 포함하며 SQLite 데이터베이스 파일은 제외합니다.

## 프로젝트 구조

### RealtimeChat.Server

- Hubs/ChatHub.cs
  - SignalR 연결 및 메시지 처리
  - 채팅방 그룹 관리
  - 접속자 관리
  - 귓속말 처리

- Data/ChatDbContext.cs
  - Entity Framework Core 데이터베이스 설정

- Entities/ChatMessageEntity.cs
  - 채팅 메시지 엔티티

- Models
  - 접속 사용자 및 메시지 전달 모델

### RealtimeChat.Web

- Pages/Chat.razor
  - 채팅 화면
  - SignalR 연결
  - 메시지 및 접속자 상태 관리

- Models
  - 채팅 메시지 클라이언트 모델

## 학습 내용

- SignalR Hub와 클라이언트 연결
- ConnectionId 기반 사용자 관리
- SignalR Group을 이용한 채팅방 분리
- Clients.All, Clients.Group, Clients.Client, Clients.Caller 사용
- SignalR 자동 재연결 처리
- Blazor WebAssembly와 JavaScript 상호 작용
- Entity Framework Core와 SQLite 연동
- 실시간 메시지와 영구 저장 데이터의 결합