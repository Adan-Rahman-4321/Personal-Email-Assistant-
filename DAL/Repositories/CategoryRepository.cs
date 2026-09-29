using Microsoft.EntityFrameworkCore;
using DAL.Data;
using Models.Entities;

namespace DAL.Repositories;

/// <summary>
/// Repository implementation for Category operations
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly EmailDbContext _context;

    public CategoryRepository(EmailDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Priority)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<Category?> GetCategoryByNameAsync(string name)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == name);
    }

    public async Task<Category> AddCategoryAsync(Category category)
    {
        category.CreatedAt = DateTime.UtcNow;
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<Category> UpdateCategoryAsync(Category category)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
            return false;

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }
}


