using Microsoft.EntityFrameworkCore;
using DAL.Data;
using Models.Entities;

namespace DAL.Repositories;

/// <summary>
/// Repository implementation for Email operations
/// </summary>
public class EmailRepository : IEmailRepository
{
    private readonly EmailDbContext _context;

    public EmailRepository(EmailDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Email>> GetAllEmailsAsync()
    {
        return await _context.Emails
            .Include(e => e.Category)
            .OrderByDescending(e => e.ReceivedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Email>> GetEmailsByCategoryAsync(int categoryId)
    {
        return await _context.Emails
            .Include(e => e.Category)
            .Where(e => e.CategoryId == categoryId)
            .OrderByDescending(e => e.ReceivedTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Email>> GetEmailsByCategoryNameAsync(string categoryName)
    {
        return await _context.Emails
            .Include(e => e.Category)
            .Where(e => e.Category.Name == categoryName)
            .OrderByDescending(e => e.ReceivedTime)
            .ToListAsync();
    }

    public async Task<Email?> GetEmailByIdAsync(int id)
    {
        return await _context.Emails
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Email?> GetEmailByExternalIdAsync(string externalEmailId)
    {
        return await _context.Emails
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.ExternalEmailId == externalEmailId);
    }

    public async Task<Email> AddEmailAsync(Email email)
    {
        email.CreatedAt = DateTime.UtcNow;
        _context.Emails.Add(email);
        await _context.SaveChangesAsync();
        return email;
    }

    public async Task<Email> UpdateEmailAsync(Email email)
    {
        email.UpdatedAt = DateTime.UtcNow;
        _context.Emails.Update(email);
        await _context.SaveChangesAsync();
        return email;
    }

    public async Task<bool> DeleteEmailAsync(int id)
    {
        var email = await _context.Emails.FindAsync(id);
        if (email == null)
            return false;

        _context.Emails.Remove(email);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetEmailCountByCategoryAsync(int categoryId)
    {
        return await _context.Emails
            .CountAsync(e => e.CategoryId == categoryId);
    }

    public async Task<bool> EmailExistsByExternalIdAsync(string externalEmailId)
    {
        return await _context.Emails
            .AnyAsync(e => e.ExternalEmailId == externalEmailId);
    }
}


