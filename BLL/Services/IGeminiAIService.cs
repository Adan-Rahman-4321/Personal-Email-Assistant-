using Models.DTOs;

namespace BLL.Services;

/// <summary>
/// Service interface for Google Gemini AI classification
/// </summary>
public interface IGeminiAIService
{
    Task<EmailClassificationResponse> ClassifyEmailWithGeminiAsync(EmailClassificationRequest request);
    Task<string> ClassifyEmailCategoryAsync(string subject, string body, string senderName, string senderEmail);
}


