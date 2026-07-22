# MemoApi

ASP.NET Core Web API와 Entity Framework Core를 사용한 메모 관리 API입니다.

## 기술 스택

- C#
- ASP.NET Core Web API
- Entity Framework Core
- SQLite
- Swagger / OpenAPI

## 주요 기능

- 메모 등록, 조회, 수정, 삭제
- DTO 기반 입력값 검증
- 키워드 검색
- 카테고리 필터
- 페이징
- 정렬
- EF Core Migration

## API

- GET /api/memos
- GET /api/memos/{id}
- POST /api/memos
- PUT /api/memos/{id}
- DELETE /api/memos/{id}

## 실행

```bash
dotnet restore
dotnet ef database update
dotnet run