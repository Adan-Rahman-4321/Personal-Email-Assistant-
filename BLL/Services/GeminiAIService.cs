using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models.DTOs;

namespace BLL.Services;

/// <summary>
/// Service for AI-based email classification using Google Gemini API
/// </summary>
public class GeminiAIService : IGeminiAIService
{
    private readonly ILogger<GeminiAIService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _geminiApiUrl;

    public GeminiAIService(ILogger<GeminiAIService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Get Gemini API key from configuration
        _apiKey = configuration["GeminiAPI:ApiKey"] ?? "";
        
        // Use Gemini Flash 1.5 model for fast classification
        var model = configuration["GeminiAPI:Model"] ?? "gemini-1.5-flash";
        _geminiApiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_apiKey}";
        
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Please add ApiKey in appsettings.json under GeminiAPI:ApiKey");
        }
    }


    public async Task<EmailClassificationResponse> ClassifyEmailWithGeminiAsync(EmailClassificationRequest request)
    {
        _logger.LogInformation("Classifying email using Google Gemini AI...");

        try
        {
            var categoryName = await ClassifyEmailCategoryAsync(
                request.Subject,
                request.Body,
                request.SenderName,
                request.SenderEmail
            );

            return new EmailClassificationResponse
            {
                CategoryName = categoryName,
                ConfidenceScore = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying email with Gemini AI");
            // Return default category on error
            return new EmailClassificationResponse
            {
                CategoryName = "Casual"
            };
        }
    }

    public async Task<string> ClassifyEmailCategoryAsync(string subject, string body, string senderName, string senderEmail)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("Gemini API key not configured. Returning default category 'Casual'.");
            return "Casual";
        }

        try
        {
            _logger.LogInformation($"Classifying email with Gemini AI. Subject: {subject}");

            // Prepare prompt for Gemini - More specific and directive
            var bodyPreview = string.IsNullOrEmpty(body) ? "No body content" : body.Substring(0, Math.Min(1000, body.Length));
            
            var prompt = $@"You are an email classification assistant. Analyze the following email and classify it into EXACTLY ONE of these 4 categories:

CATEGORY DEFINITIONS:
1. ""Most Important"" - Urgent matters requiring immediate action:
   - Deadlines, expired notifications, overdue payments
   - Security alerts, password changes, account warnings
   - Critical work requests, urgent meetings
   - Emergency notifications, urgent deadlines

2. ""Important"" - Business or work-related emails requiring attention but not urgent:
   - Meeting invitations, schedule updates
   - Project updates, work assignments
   - Invoices, payment confirmations
   - Professional communications, business inquiries

3. ""Casual"" - Personal or casual conversations:
   - Friendly personal emails
   - Casual updates from friends/family
   - Social invitations, personal chats
   - Non-urgent personal communications

4. ""Promotional"" - Marketing and sales content:
   - Deals, discounts, offers
   - Newsletter subscriptions
   - Marketing campaigns, advertisements
   - Product announcements, sales pitches

EMAIL TO CLASSIFY:
Subject: {subject}
From: {senderName} ({senderEmail})
Body: {bodyPreview}

CRITICAL INSTRUCTIONS:
- Return ONLY the exact category name: ""Most Important"", ""Important"", ""Casual"", or ""Promotional""
- Do NOT add any explanations, reasoning, or additional text
- Do NOT use markdown formatting, quotes, or numbers
- Return exactly as written above (case-sensitive)

Category:";

            // Prepare request payload for Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3, // Lower temperature for more consistent classification
                    topK = 1,
                    topP = 0.8,
                    maxOutputTokens = 50
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation($"Calling Gemini API: {_geminiApiUrl.Replace(_apiKey, "***")}");

            // Call Gemini API
            var response = await _httpClient.PostAsync(_geminiApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Gemini API error: {response.StatusCode} - {responseContent}");
                return "Casual"; // Default on error
            }

            // Parse Gemini response
            var jsonDoc = JsonDocument.Parse(responseContent);
            
            // Extract the classification text from Gemini response
            var categoryText = "";
            if (jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) && 
                candidates.GetArrayLength() > 0)
            {
                var candidate = candidates[0];
                if (candidate.TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var part = parts[0];
                    if (part.TryGetProperty("text", out var text))
                    {
                        categoryText = text.GetString() ?? "";
                    }
                }
            }

            // Clean and parse the response - remove extra whitespace, newlines, quotes
            categoryText = categoryText.Trim();
            categoryText = categoryText.Replace("\"", "").Replace("'", ""); // Remove quotes
            categoryText = categoryText.Split('\n').FirstOrDefault()?.Trim() ?? categoryText; // Get first line only
            categoryText = categoryText.Split('.').FirstOrDefault()?.Trim() ?? categoryText; // Get before first period
            
            _logger.LogInformation($"Gemini AI raw response: '{categoryText}' (Subject: {subject})");

            // Define valid categories (case-insensitive matching)
            var validCategories = new[] { "Most Important", "Important", "Casual", "Promotional" };
            
            // Try exact match first (case-insensitive)
            foreach (var category in validCategories)
            {
                if (categoryText.Equals(category, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"✅ Email classified as: {category} (Subject: {subject})");
                    return category;
                }
            }

            // Try contains match (in case Gemini adds extra words)
            foreach (var category in validCategories)
            {
                if (categoryText.Contains(category, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation($"✅ Email classified as: {category} (Subject: {subject})");
                    return category;
                }
            }

            // Try partial matches for common variations
            var lowerText = categoryText.ToLowerInvariant();
            if (lowerText.Contains("most important") || lowerText.Contains("urgent") || lowerText.Contains("critical"))
            {
                _logger.LogInformation($"✅ Email classified as: Most Important (from keywords) (Subject: {subject})");
                return "Most Important";
            }
            if (lowerText.Contains("important") && !lowerText.Contains("most"))
            {
                _logger.LogInformation($"✅ Email classified as: Important (Subject: {subject})");
                return "Important";
            }
            if (lowerText.Contains("promotional") || lowerText.Contains("marketing") || lowerText.Contains("offer"))
            {
                _logger.LogInformation($"✅ Email classified as: Promotional (Subject: {subject})");
                return "Promotional";
            }
            if (lowerText.Contains("casual") || lowerText.Contains("personal"))
            {
                _logger.LogInformation($"✅ Email classified as: Casual (Subject: {subject})");
                return "Casual";
            }

            _logger.LogWarning($"❌ Could not parse category from Gemini response: '{categoryText}'. Using default 'Casual'. Response JSON: {responseContent.Substring(0, Math.Min(500, responseContent.Length))}");
            return "Casual";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calling Gemini API. Subject: {subject}");
            return "Casual"; // Default category on error
        }
    }
}

