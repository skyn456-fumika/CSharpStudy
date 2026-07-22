using MemoApi.Dtos;
using MemoApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MemoApi.Controllers;

[ApiController]
[Route("api/memos")]
public class MemosController : ControllerBase
{
    private readonly MemoService memoService;

    public MemosController(MemoService memoService)
    {
        this.memoService = memoService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<MemoResponse>>> GetAll(
        [FromQuery] string? keyword = null,
        [FromQuery] string? category = null,
        [FromQuery] string? sort = "latest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1)
        {
            return BadRequest(new
            {
                message = "page는 1 이상이어야 합니다."
            });
        }

        if (pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "pageSize는 1 이상 100 이하여야 합니다."
            });
        }

        string[] allowedSorts =
        [
            "latest",
        "oldest",
        "title",
        "title-desc"
        ];

        if (!allowedSorts.Contains(
            sort ?? "",
            StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new
            {
                message = "sort는 latest, oldest, title, title-desc 중 하나여야 합니다."
            });
        }

        PagedResponse<MemoResponse> result =
            await memoService.GetAllAsync(
                keyword,
                category,
                sort,
                page,
                pageSize);

        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<MemoResponse>> GetById(long id)
    {
        MemoResponse? memo = await memoService.GetByIdAsync(id);

        if (memo == null)
        {
            return NotFound();
        }

        return Ok(memo);
    }

    [HttpPost]
    public async Task<ActionResult<MemoResponse>> Create(
        MemoCreateRequest request)
    {
        MemoResponse memo = await memoService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = memo.Id },
            memo);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<MemoResponse>> Update(
        long id,
        MemoUpdateRequest request)
    {
        MemoResponse? memo = await memoService.UpdateAsync(id, request);

        if (memo == null)
        {
            return NotFound();
        }

        return Ok(memo);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        bool deleted = await memoService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}