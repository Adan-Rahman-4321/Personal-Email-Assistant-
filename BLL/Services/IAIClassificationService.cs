using Models.DTOs;

namespace BLL.Services;

/// <summary>
/// Service interface for AI-based email classification
/// </summary>
public interface IAIClassificationService
{
    Task<EmailClassificationResponse> ClassifyEmailAsync(EmailClassificationRequest request);
    Task<string> ClassifyEmailCategoryAsync(string subject, string body, string senderName, string senderEmail);
}


