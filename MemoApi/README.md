# Memo Management App

ASP.NET Core Web API와 Blazor WebAssembly를 연동한 메모 관리 애플리케이션입니다.

## 프로젝트 구성

```text
MemoApi/
├─ MemoApi/       # ASP.NET Core Web API
├─ MemoBlazor/    # Blazor WebAssembly
└─ MemoApi.sln
```

## 기술 스택

- C#
- ASP.NET Core Web API
- Blazor WebAssembly
- Entity Framework Core
- SQLite
- Bootstrap

## 주요 기능

- 메모 등록, 조회, 수정, 삭제
- 입력값 검증
- 키워드 검색
- 카테고리 필터
- 정렬
- 페이징
- 삭제 확인
- 로딩 및 오류 상태 처리
- CORS 기반 API 연동

## 실행 주소

- API: https://localhost:7273
- Web: https://localhost:7157
- 실행 방법
- MemoApi 프로젝트 실행
- MemoBlazor 프로젝트 실행
- 브라우저에서 https://localhost:7157/memos 접속