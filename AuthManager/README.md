# AuthManager

ASP.NET Core Web API와 Blazor WebAssembly를 사용해 구현한 JWT 기반 회원 및 권한 관리 프로젝트입니다.

## 프로젝트 구성

```text
AuthManager
├─ AuthManager.Server
├─ AuthManager.Web
├─ AuthManager.sln
└─ README.md
```

## 기술 스택

### Server

- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- JWT Authentication
- ASP.NET Core Identity PasswordHasher

### Web

- Blazor WebAssembly
- HttpClient
- AuthenticationStateProvider
- LocalStorage
- Bootstrap

## 주요 기능

### 회원 기능

- 회원가입
- 로그인
- 내 정보 조회
- 닉네임 수정
- 비밀번호 변경
- 로그아웃

### JWT 인증

- Access Token 발급
- Refresh Token 발급
- Refresh Token 해시 저장
- Access Token 자동 재발급
- Refresh Token 회전
- 로그아웃 시 Refresh Token 폐기
- 정지 계정의 기존 JWT 즉시 차단

### 권한 관리

- USER / ADMIN 역할 구분
- 관리자 전용 페이지
- 사용자 역할 변경
- 사용자 정지 및 활성화
- 관리자 자신의 역할 및 상태 변경 방지

## 인증 처리 흐름

```text
로그인
→ Access Token 및 Refresh Token 발급
→ 브라우저 LocalStorage 저장
→ API 요청 시 Access Token 자동 첨부
→ Access Token 만료 시 Refresh Token으로 재발급
→ 기존 Refresh Token 폐기
→ 새 토큰 저장
→ 기존 API 요청 재시도
```

## Refresh Token 보안

Refresh Token 원본은 데이터베이스에 저장하지 않습니다.

```text
원본 Refresh Token
→ SHA-256 해시 생성
→ 해시값만 데이터베이스에 저장
```

재발급에 사용된 Refresh Token은 즉시 폐기되며, 새로운 Refresh Token이 발급됩니다.

## 실행 주소

```text
Server: https://localhost:7293
Web:    https://localhost:7232
```

## 실행 환경

- .NET 9 SDK
- Entity Framework Core CLI 도구
- HTTPS 개발 인증서

EF 도구 설치가 필요하다면:

```shell
dotnet tool install --global dotnet-ef
```

## 실행 방법

### Server

```shell
cd AuthManager.Server
dotnet run
```

### Web

```shell
cd AuthManager.Web
dotnet run
```

### 데이터베이스 마이그레이션

```shell
cd AuthManager.Server
dotnet ef database update
```

## 주요 API

| Method | URL                            | 설명                      |
| ------ | ------------------------------ | ----------------------- |
| POST   | `/api/auth/register`           | 회원가입                    |
| POST   | `/api/auth/login`              | 로그인                     |
| POST   | `/api/auth/refresh`            | 토큰 재발급                  |
| POST   | `/api/auth/logout`             | 로그아웃 및 Refresh Token 폐기 |
| GET    | `/api/users/me`                | 내 정보 조회                 |
| PATCH  | `/api/users/me/nickname`       | 닉네임 변경                  |
| PATCH  | `/api/users/me/password`       | 비밀번호 변경                 |
| GET    | `/api/admin/users`             | 사용자 목록                  |
| PATCH  | `/api/admin/users/{id}/role`   | 역할 변경                   |
| PATCH  | `/api/admin/users/{id}/status` | 계정 상태 변경                |

## 학습 내용

- JWT Access Token과 Refresh Token의 역할
- Refresh Token Rotation
- DelegatingHandler를 통한 인증 헤더 자동 적용
- 만료된 요청 자동 재발급 및 재시도
- Blazor AuthenticationStateProvider
- 역할 기반 인가
- 미들웨어를 통한 계정 상태 검사
- EF Core 관계 설정과 마이그레이션

## 보안 설정

개발 환경의 JWT Secret Key는 예제 값만 사용하며, 실제 운영 환경에서는 환경 변수 또는 Secret Manager를 통해 관리해야 합니다.

테스트 계정 정보가 포함된 SQLite 데이터베이스 파일은 저장소에 포함하지 않습니다.

### JWT Secret Key 설정

개발 환경에서는 .NET User Secrets를 사용합니다.

```shell
cd AuthManager.Server
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "개발용 JWT Secret Key"
```

`appsettings.json`에는 실제 비밀키를 저장하지 않습니다.