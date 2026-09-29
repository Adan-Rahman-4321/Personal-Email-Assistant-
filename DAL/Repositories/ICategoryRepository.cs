using Models.Entities;

namespace DAL.Repositories;

/// <summary>
/// Repository interface for Category operations
/// </summary>
public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task<Category?> GetCategoryByNameAsync(string name);
    Task<Category> AddCategoryAsync(Category category);
    Task<Category> UpdateCategoryAsync(Category category);
    Task<bool> DeleteCategoryAsync(int id);
}


