using DAL.Repositories;
using Microsoft.Extensions.Logging;
using Models.DTOs;
using Models.Entities;

namespace BLL.Services;

/// <summary>
/// Main service for processing emails - orchestrates fetching, classification, and storage
/// </summary>
public class EmailProcessingService : IEmailProcessingService
{
    private readonly IEmailFetcherService _emailFetcherService;
    private readonly IAIClassificationService _aiClassificationService;
    private readonly IEmailRepository _emailRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ILogger<EmailProcessingService> _logger;

    public EmailProcessingService(
        IEmailFetcherService emailFetcherService,
        IAIClassificationService aiClassificationService,
        IEmailRepository emailRepository,
        ICategoryRepository categoryRepository,
        ILogger<EmailProcessingService> logger)
    {
        _emailFetcherService = emailFetcherService;
        _aiClassificationService = aiClassificationService;
        _emailRepository = emailRepository;
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Email>> FetchAndClassifyEmailsAsync(EmailFetchRequest request)
    {
        _logger.LogInformation($"Starting email fetch and classification process. Provider: {request.Provider}, Max: {request.MaxEmails}");

        try
        {
            // Step 1: Fetch emails from provider
            _logger.LogInformation($"Starting to fetch emails from {request.Provider}. Mock mode: {false}");
            var fetchedEmails = await _emailFetcherService.FetchEmailsAsync(request);
            _logger.LogInformation($"Fetched {fetchedEmails.Count()} emails from {request.Provider}. Processing for classification...");

            var processedEmails = new List<Email>();

            // Step 2: Classify and process each email
            foreach (var email in fetchedEmails)
            {
                try
                {
                    var processedEmail = await ProcessAndSaveEmailAsync(email);
                    processedEmails.Add(processedEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing email: {email.Subject}");
                }
            }

            _logger.LogInformation($"Successfully processed {processedEmails.Count} emails");
            return processedEmails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fetch and classify process");
            throw;
        }
    }

    public async Task<Email> ProcessAndSaveEmailAsync(Email email)
    {
        _logger.LogInformation($"Processing email: {email.Subject}");

        // Check if email already exists by external ID
        if (!string.IsNullOrEmpty(email.ExternalEmailId))
        {
            var existingEmail = await _emailRepository.GetEmailByExternalIdAsync(email.ExternalEmailId);
            if (existingEmail != null)
            {
                _logger.LogInformation($"Email already exists: {email.Subject}. Skipping.");
                return existingEmail;
            }
        }

        // Step 1: Classify email using AI
        var classificationRequest = new EmailClassificationRequest
        {
            Subject = email.Subject,
            Body = email.Body,
            SenderName = email.SenderName,
            SenderEmail = email.SenderEmail
        };

        var classificationResponse = await _aiClassificationService.ClassifyEmailAsync(classificationRequest);
        
        _logger.LogInformation($"Email classified as: '{classificationResponse.CategoryName}' for subject: {email.Subject}");

        // Step 2: Get category by name
        var category = await _categoryRepository.GetCategoryByNameAsync(classificationResponse.CategoryName);
        if (category == null)
        {
            _logger.LogWarning($"Category '{classificationResponse.CategoryName}' not found. Using default 'Casual'.");
            category = await _categoryRepository.GetCategoryByNameAsync("Casual");
            if (category == null)
            {
                throw new InvalidOperationException("Default category 'Casual' not found in database.");
            }
        }
        
        _logger.LogInformation($"Assigned category ID: {category.Id}, Name: {category.Name} for email: {email.Subject}");

        // Step 3: Assign category to email
        email.CategoryId = category.Id;
        email.EmailProvider = email.EmailProvider ?? "Unknown";

        // Step 4: Save email to database
        var savedEmail = await _emailRepository.AddEmailAsync(email);
        _logger.LogInformation($"Email saved and classified as: {category.Name}");

        return savedEmail;
    }

    public async Task<IEnumerable<EmailDTO>> GetEmailsByCategoryAsync(string categoryName)
    {
        var emails = await _emailRepository.GetEmailsByCategoryNameAsync(categoryName);

        return emails.Select(e => new EmailDTO
        {
            Id = e.Id,
            Subject = e.Subject,
            Body = e.Body,
            SenderName = e.SenderName,
            SenderEmail = e.SenderEmail,
            ReceivedTime = e.ReceivedTime,
            CategoryName = e.Category.Name,
            CategoryDisplayName = e.Category.DisplayName,
            CategoryColorCode = e.Category.ColorCode,
            EmailProvider = e.EmailProvider
        });
    }

    public async Task<IEnumerable<CategoryDTO>> GetCategoriesWithCountsAsync()
    {
        var categories = await _categoryRepository.GetAllCategoriesAsync();

        var categoryDTOs = new List<CategoryDTO>();

        foreach (var category in categories)
        {
            var count = await _emailRepository.GetEmailCountByCategoryAsync(category.Id);
            categoryDTOs.Add(new CategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                DisplayName = category.DisplayName,
                Description = category.Description,
                ColorCode = category.ColorCode,
                Priority = category.Priority,
                EmailCount = count
            });
        }

        return categoryDTOs.OrderBy(c => c.Priority);
    }

    // CRUD Operations
    public async Task<IEnumerable<EmailDTO>> GetAllEmailsAsync()
    {
        var emails = await _emailRepository.GetAllEmailsAsync();
        
        return emails.Select(e => new EmailDTO
        {
            Id = e.Id,
            Subject = e.Subject,
            Body = e.Body,
            SenderName = e.SenderName,
            SenderEmail = e.SenderEmail,
            ReceivedTime = e.ReceivedTime,
            CategoryName = e.Category?.Name ?? "Unknown",
            CategoryDisplayName = e.Category?.DisplayName ?? "Unknown",
            CategoryColorCode = e.Category?.ColorCode ?? "#6c757d",
            EmailProvider = e.EmailProvider
        }).OrderByDescending(e => e.ReceivedTime);
    }

    public async Task<EmailDTO?> GetEmailByIdAsync(int id)
    {
        var email = await _emailRepository.GetEmailByIdAsync(id);
        if (email == null)
        {
            return null;
        }

        return new EmailDTO
        {
            Id = email.Id,
            Subject = email.Subject,
            Body = email.Body,
            SenderName = email.SenderName,
            SenderEmail = email.SenderEmail,
            ReceivedTime = email.ReceivedTime,
            CategoryName = email.Category?.Name ?? "Unknown",
            CategoryDisplayName = email.Category?.DisplayName ?? "Unknown",
            CategoryColorCode = email.Category?.ColorCode ?? "#6c757d",
            EmailProvider = email.EmailProvider
        };
    }

    public async Task<EmailDTO?> UpdateEmailAsync(UpdateEmailRequest request)
    {
        var email = await _emailRepository.GetEmailByIdAsync(request.Id);
        if (email == null)
        {
            return null;
        }

        // Update email fields
        if (!string.IsNullOrEmpty(request.Subject))
        {
            email.Subject = request.Subject;
        }

        if (!string.IsNullOrEmpty(request.Body))
        {
            email.Body = request.Body;
        }

        if (request.CategoryId.HasValue)
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId.Value);
            if (category != null)
            {
                email.CategoryId = request.CategoryId.Value;
            }
        }

        email.UpdatedAt = DateTime.UtcNow;

        var updatedEmail = await _emailRepository.UpdateEmailAsync(email);
        
        // Reload with category
        var reloadedEmail1 = await _emailRepository.GetEmailByIdAsync(updatedEmail.Id);
        if (reloadedEmail1 == null)
        {
            throw new InvalidOperationException("Email was deleted after update");
        }

        return new EmailDTO
        {
            Id = reloadedEmail1.Id,
            Subject = reloadedEmail1.Subject,
            Body = reloadedEmail1.Body,
            SenderName = reloadedEmail1.SenderName,
            SenderEmail = reloadedEmail1.SenderEmail,
            ReceivedTime = reloadedEmail1.ReceivedTime,
            CategoryName = reloadedEmail1.Category?.Name ?? "Unknown",
            CategoryDisplayName = reloadedEmail1.Category?.DisplayName ?? "Unknown",
            CategoryColorCode = reloadedEmail1.Category?.ColorCode ?? "#6c757d",
            EmailProvider = reloadedEmail1.EmailProvider
        };
    }

    public async Task<bool> DeleteEmailAsync(int id)
    {
        return await _emailRepository.DeleteEmailAsync(id);
    }

    public async Task<EmailDTO?> ReclassifyEmailAsync(int id)
    {
        var email = await _emailRepository.GetEmailByIdAsync(id);
        if (email == null)
        {
            return null;
        }

        _logger.LogInformation($"Reclassifying email ID: {id}, Subject: {email.Subject}");

        // Classify email again using AI
        var classificationRequest = new EmailClassificationRequest
        {
            Subject = email.Subject,
            Body = email.Body,
            SenderName = email.SenderName,
            SenderEmail = email.SenderEmail
        };

        var classificationResponse = await _aiClassificationService.ClassifyEmailAsync(classificationRequest);
        
        _logger.LogInformation($"Email reclassified as: '{classificationResponse.CategoryName}' for subject: {email.Subject}");

        // Get category by name
        var category = await _categoryRepository.GetCategoryByNameAsync(classificationResponse.CategoryName);
        if (category == null)
        {
            _logger.LogWarning($"Category '{classificationResponse.CategoryName}' not found. Using default 'Casual'.");
            category = await _categoryRepository.GetCategoryByNameAsync("Casual");
            if (category == null)
            {
                throw new InvalidOperationException("Default category 'Casual' not found in database.");
            }
        }

        // Update category
        email.CategoryId = category.Id;
        email.UpdatedAt = DateTime.UtcNow;

        var updatedEmail = await _emailRepository.UpdateEmailAsync(email);
        
        // Reload with category
        var reloadedEmail2 = await _emailRepository.GetEmailByIdAsync(updatedEmail.Id);
        if (reloadedEmail2 == null)
        {
            throw new InvalidOperationException("Email was deleted after update");
        }

        return new EmailDTO
        {
            Id = reloadedEmail2.Id,
            Subject = reloadedEmail2.Subject,
            Body = reloadedEmail2.Body,
            SenderName = reloadedEmail2.SenderName,
            SenderEmail = reloadedEmail2.SenderEmail,
            ReceivedTime = reloadedEmail2.ReceivedTime,
            CategoryName = reloadedEmail2.Category?.Name ?? "Unknown",
            CategoryDisplayName = reloadedEmail2.Category?.DisplayName ?? "Unknown",
            CategoryColorCode = reloadedEmail2.Category?.ColorCode ?? "#6c757d",
            EmailProvider = reloadedEmail2.EmailProvider
        };
    }
}

