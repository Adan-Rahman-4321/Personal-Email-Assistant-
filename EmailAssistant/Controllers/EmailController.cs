using Microsoft.AspNetCore.Mvc;
using BLL.Services;
using Models.DTOs;

namespace EmailAssistant.Controllers;

/// <summary>
/// API Controller for Email operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IEmailProcessingService _emailProcessingService;
    private readonly ILogger<EmailController> _logger;

    public EmailController(
        IEmailProcessingService emailProcessingService,
        ILogger<EmailController> logger)
    {
        _emailProcessingService = emailProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Get emails by category name
    /// </summary>
    [HttpGet("by-category/{categoryName}")]
    public async Task<ActionResult<IEnumerable<EmailDTO>>> GetEmailsByCategory(string categoryName)
    {
        try
        {
            var emails = await _emailProcessingService.GetEmailsByCategoryAsync(categoryName);
            return Ok(emails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting emails by category: {CategoryName}", categoryName);
            return StatusCode(500, new { error = "Error fetching emails" });
        }
    }

    /// <summary>
    /// Fetch and classify emails from provider
    /// </summary>
    [HttpPost("fetch")]
    public async Task<ActionResult> FetchAndClassifyEmails([FromBody] EmailFetchRequest request)
    {
        try
        {
            _logger.LogInformation("Fetching emails from {Provider}", request.Provider);

            var emails = await _emailProcessingService.FetchAndClassifyEmailsAsync(request);
            
            return Ok(new 
            { 
                message = "Emails fetched and classified successfully",
                count = emails.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching emails");
            return StatusCode(500, new { error = "Error fetching emails", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all categories with email counts
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<CategoryDTO>>> GetCategories()
    {
        try
        {
            var categories = await _emailProcessingService.GetCategoriesWithCountsAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, new { error = "Error fetching categories" });
        }
    }

    /// <summary>
    /// Get all emails
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EmailDTO>>> GetAllEmails()
    {
        try
        {
            var emails = await _emailProcessingService.GetAllEmailsAsync();
            return Ok(emails);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all emails");
            return StatusCode(500, new { error = "Error fetching emails" });
        }
    }

    /// <summary>
    /// Get email by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EmailDTO>> GetEmailById(int id)
    {
        try
        {
            var email = await _emailProcessingService.GetEmailByIdAsync(id);
            if (email == null)
            {
                return NotFound(new { error = "Email not found" });
            }
            return Ok(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting email by ID: {Id}", id);
            return StatusCode(500, new { error = "Error fetching email" });
        }
    }

    /// <summary>
    /// Update email (Change category, subject, etc.)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<EmailDTO>> UpdateEmail(int id, [FromBody] UpdateEmailRequest request)
    {
        try
        {
            if (id != request.Id)
            {
                return BadRequest(new { error = "Email ID mismatch" });
            }

            var email = await _emailProcessingService.UpdateEmailAsync(request);
            if (email == null)
            {
                return NotFound(new { error = "Email not found" });
            }

            return Ok(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating email: {Id}", id);
            return StatusCode(500, new { error = "Error updating email", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete email
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEmail(int id)
    {
        try
        {
            var result = await _emailProcessingService.DeleteEmailAsync(id);
            if (!result)
            {
                return NotFound(new { error = "Email not found" });
            }

            return Ok(new { message = "Email deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting email: {Id}", id);
            return StatusCode(500, new { error = "Error deleting email", details = ex.Message });
        }
    }

    /// <summary>
    /// Reclassify a specific email using AI
    /// </summary>
    [HttpPost("{id}/reclassify")]
    public async Task<ActionResult<EmailDTO>> ReclassifyEmail(int id)
    {
        try
        {
            var email = await _emailProcessingService.ReclassifyEmailAsync(id);
            if (email == null)
            {
                return NotFound(new { error = "Email not found" });
            }

            return Ok(email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reclassifying email: {Id}", id);
            return StatusCode(500, new { error = "Error reclassifying email", details = ex.Message });
        }
    }
}

