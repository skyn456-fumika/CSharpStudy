using MemoApi.Data;
using MemoApi.Dtos;
using MemoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoApi.Services;

public class MemoService
{
    private readonly MemoDbContext dbContext;

    public MemoService(MemoDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<PagedResponse<MemoResponse>> GetAllAsync(
        string? keyword,
        string? category,
        string? sort,
        int page,
        int pageSize)
    {
        IQueryable<Memo> query = dbContext.Memos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            string trimmedKeyword = keyword.Trim();

            query = query.Where(memo =>
                memo.Title.Contains(trimmedKeyword) ||
                memo.Content.Contains(trimmedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            string trimmedCategory = category.Trim();

            query = query.Where(memo =>
                memo.Category == trimmedCategory);
        }

        int totalCount = await query.CountAsync();

        IQueryable<Memo> sortedQuery = sort?.ToLower() switch
        {
            "oldest" => query.OrderBy(memo => memo.Id),
            "title" => query.OrderBy(memo => memo.Title),
            "title-desc" => query.OrderByDescending(memo => memo.Title),
            _ => query.OrderByDescending(memo => memo.Id)
        };

        List<MemoResponse> items = await sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(memo => ToResponse(memo))
            .ToListAsync();

        int totalPages = (int)Math.Ceiling(
            totalCount / (double)pageSize);

        return new PagedResponse<MemoResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<MemoResponse?> GetByIdAsync(long id)
    {
        Memo? memo = await dbContext.Memos
            .FirstOrDefaultAsync(memo => memo.Id == id);

        return memo == null ? null : ToResponse(memo);
    }

    public async Task<MemoResponse> CreateAsync(MemoCreateRequest request)
    {
        ValidateRequest(
            request.Title,
            request.Content,
            request.Category);

        DateTime now = DateTime.UtcNow;

        Memo memo = new()
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Category = request.Category.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Memos.Add(memo);
        await dbContext.SaveChangesAsync();

        return ToResponse(memo);
    }

    public async Task<MemoResponse?> UpdateAsync(
        long id,
        MemoUpdateRequest request)
    {
        ValidateRequest(
            request.Title,
            request.Content,
            request.Category);

        Memo? memo = await dbContext.Memos
            .FirstOrDefaultAsync(memo => memo.Id == id);

        if (memo == null)
        {
            return null;
        }

        memo.Title = request.Title.Trim();
        memo.Content = request.Content.Trim();
        memo.Category = request.Category.Trim();
        memo.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();

        return ToResponse(memo);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        Memo? memo = await dbContext.Memos
            .FirstOrDefaultAsync(memo => memo.Id == id);

        if (memo == null)
        {
            return false;
        }

        dbContext.Memos.Remove(memo);
        await dbContext.SaveChangesAsync();

        return true;
    }

    private static MemoResponse ToResponse(Memo memo)
    {
        return new MemoResponse
        {
            Id = memo.Id,
            Title = memo.Title,
            Content = memo.Content,
            Category = memo.Category,
            CreatedAt = memo.CreatedAt,
            UpdatedAt = memo.UpdatedAt
        };
    }

    private static void ValidateRequest(
        string title,
        string content,
        string category)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("제목은 공백일 수 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("내용은 공백일 수 없습니다.");
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("카테고리는 공백일 수 없습니다.");
        }
    }
}