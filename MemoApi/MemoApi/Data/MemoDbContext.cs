using MemoApi.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoApi.Data;

public class MemoDbContext : DbContext
{
    public MemoDbContext(DbContextOptions<MemoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Memo> Memos => Set<Memo>();
}