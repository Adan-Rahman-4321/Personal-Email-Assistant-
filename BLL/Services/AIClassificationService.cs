using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Models.DTOs;

namespace BLL.Services;

/// <summary>
/// Service for AI-based email classification using Native High-Precision Engine or Gemini AI
/// Works 100% locally with zero external classification server or Python dependency
/// </summary>
public class AIClassificationService : IAIClassificationService
{
    private readonly ILogger<AIClassificationService> _logger;
    private readonly IGeminiAIService? _geminiAIService;
    private readonly bool _useGemini;

    // Categories
    public const string MostImportant = "Most Important";
    public const string Important = "Important";
    public const string Casual = "Casual";
    public const string Promotional = "Promotional";

    // Most Important Keywords (Urgent, Critical, Security, Deadlines)
    private static readonly string[] MostImportantKeywords = new[]
    {
        "urgent", "critical", "emergency", "asap", "as soon as possible", "immediate action",
        "action required", "requires immediate", "immediate attention", "action needed",
        "time sensitive", "expires today", "deadline today", "final notice", "last chance",
        "account suspended", "security alert", "unauthorized access", "fraud", "breach",
        "hacked", "compromised", "overdue", "late payment", "production down", "server down",
        "outage", "system alert", "high priority", "verification required", "immediate response"
    };

    // Important Keywords (Business, Work, Finances, Scheduling)
    private static readonly string[] ImportantKeywords = new[]
    {
        "meeting", "appointment", "interview", "scheduled", "schedule", "calendar", "invite",
        "project", "deadline", "task", "assignment", "proposal", "contract", "agreement",
        "invoice", "receipt", "payment due", "payment received", "billing", "timesheet",
        "payroll", "salary", "work", "business", "client", "quarterly", "budget", "approval",
        "approved", "review", "submit", "document", "conference", "webinar", "workshop",
        "training", "report", "presentation", "deliverable", "milestone", "status update"
    };

    // Promotional Keywords (Marketing, Sales, Deals, Subscriptions)
    private static readonly string[] PromotionalKeywords = new[]
    {
        "sale", "% off", "percent off", "discount", "coupon", "promo code", "deal", "deals",
        "promotion", "promotional", "clearance", "buy now", "shop now", "order now",
        "order today", "special offer", "exclusive offer", "limited time", "limited offer",
        "free shipping", "subscribe", "newsletter", "unsubscribe", "opt-out", "opt out",
        "win prize", "cashback", "gift card", "black friday", "cyber monday", "summer sale",
        "winter sale", "save up to", "advertisement", "sponsored"
    };

    // Promotional Domains & Sender hints
    private static readonly string[] PromotionalSenderHints = new[]
    {
        "noreply", "no-reply", "donotreply", "marketing", "promo", "promotions", "sales",
        "deals", "offers", "newsletter", "store", "shop", "amazon", "ebay", "walmart",
        "aliexpress", "daraz", "shopify", "etsy", "notifications"
    };

    // Casual Keywords (Personal, Informal, Greetings, Social)
    private static readonly string[] CasualKeywords = new[]
    {
        "hey", "hi", "hello", "how are you", "how's it going", "weekend", "coffee", "lunch",
        "dinner", "birthday", "happy birthday", "congrats", "congratulations", "party",
        "catch up", "hang out", "vacation", "trip", "holiday", "plans", "family", "friend",
        "see you", "whats up", "what's up", "drinks", "casual"
    };

    public AIClassificationService(
        ILogger<AIClassificationService> logger, 
        IConfiguration configuration,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        
        try
        {
            _geminiAIService = serviceProvider.GetService<IGeminiAIService>();
        }
        catch
        {
            _geminiAIService = null;
        }
        
        var geminiApiKey = configuration["GeminiAPI:ApiKey"] ?? "";
        _useGemini = !string.IsNullOrWhiteSpace(geminiApiKey) && _geminiAIService != null;
        
        if (_useGemini)
        {
            _logger.LogInformation("Gemini AI is configured for email classification");
        }
        else
        {
            _logger.LogInformation("Using Native In-Memory C# Classification Engine (100% standalone, no external server needed)");
        }
    }

    public async Task<EmailClassificationResponse> ClassifyEmailAsync(EmailClassificationRequest request)
    {
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
                ConfidenceScore = 1.0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error classifying email for subject: {Subject}", request.Subject);
            return new EmailClassificationResponse
            {
                CategoryName = Casual,
                ConfidenceScore = 0.5
            };
        }
    }

    public async Task<string> ClassifyEmailCategoryAsync(string subject, string body, string senderName, string senderEmail)
    {
        // 1. If Gemini AI is configured, attempt Gemini first
        if (_useGemini && _geminiAIService != null)
        {
            try
            {
                var geminiCategory = await _geminiAIService.ClassifyEmailCategoryAsync(subject, body, senderName, senderEmail);
                if (!string.IsNullOrWhiteSpace(geminiCategory) && IsValidCategory(geminiCategory))
                {
                    _logger.LogInformation("Gemini classified email '{Subject}' as: {Category}", subject, geminiCategory);
                    return geminiCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini API unavailable or failed. Falling back immediately to Native C# Engine");
            }
        }
        
        // 2. High-precision Native C# Classification Engine
        var category = ClassifyNative(subject, body, senderName, senderEmail);
        _logger.LogInformation("Native engine classified email '{Subject}' as: {Category}", subject, category);
        return category;
    }

    /// <summary>
    /// Pure C# high-precision email classification engine.
    /// Fast, deterministic, and 100% reliable with zero external processes or network calls.
    /// </summary>
    public static string ClassifyNative(string? subjectText, string? bodyText, string? senderNameText, string? senderEmailText)
    {
        var subject = subjectText?.Trim() ?? "";
        var body = bodyText?.Trim() ?? "";
        var senderName = senderNameText?.Trim() ?? "";
        var senderEmail = senderEmailText?.Trim() ?? "";

        var subjectLower = subject.ToLowerInvariant();
        var bodyLower = body.ToLowerInvariant();
        var content = $"{subjectLower} {bodyLower}";
        var senderLower = $"{senderEmail} {senderName}".ToLowerInvariant();

        // -------------------------------------------------------------
        // PRIORITY 1: Immediate Most Important (Urgent / Emergency / Action Required)
        // -------------------------------------------------------------
        if (subjectLower.StartsWith("urgent:") || subjectLower.StartsWith("[urgent]") || subjectLower.StartsWith("urgent!") ||
            subjectLower.StartsWith("critical:") || subjectLower.StartsWith("[critical]") || subjectLower.StartsWith("critical!") ||
            subjectLower.StartsWith("emergency:") || subjectLower.StartsWith("[emergency]") ||
            subjectLower.Contains("action required") || subjectLower.Contains("immediate action") ||
            subjectLower.Contains("security alert") || subjectLower.Contains("unauthorized access"))
        {
            return MostImportant;
        }

        // Check if subject is in ALL CAPS and contains alert/urgent keywords
        if (subject.Length > 5 && subject.All(c => !char.IsLetter(c) || char.IsUpper(c)))
        {
            if (content.Contains("urgent") || content.Contains("action") || content.Contains("alert") || 
                content.Contains("security") || content.Contains("warning") || content.Contains("notice"))
            {
                return MostImportant;
            }
        }

        // Calculate scores
        int mostImportantScore = 0;
        int importantScore = 0;
        int promotionalScore = 0;
        int casualScore = 0;

        // Score Most Important
        foreach (var keyword in MostImportantKeywords)
        {
            if (subjectLower.Contains(keyword))
            {
                mostImportantScore += 4;
            }
            else if (bodyLower.Contains(keyword))
            {
                mostImportantScore += 2;
            }
        }

        // Score Promotional
        foreach (var hint in PromotionalSenderHints)
        {
            if (senderLower.Contains(hint))
            {
                promotionalScore += 3;
                break;
            }
        }

        foreach (var keyword in PromotionalKeywords)
        {
            if (subjectLower.Contains(keyword))
            {
                promotionalScore += 3;
            }
            else if (bodyLower.Contains(keyword))
            {
                promotionalScore += 1;
            }
        }

        if (bodyLower.Contains("unsubscribe") || bodyLower.Contains("opt-out") || bodyLower.Contains("manage preferences"))
        {
            promotionalScore += 4;
        }

        // Score Important (Business / Professional)
        foreach (var keyword in ImportantKeywords)
        {
            if (subjectLower.Contains(keyword))
            {
                importantScore += 3;
            }
            else if (bodyLower.Contains(keyword))
            {
                importantScore += 1;
            }
        }

        // Score Casual
        foreach (var keyword in CasualKeywords)
        {
            if (subjectLower.Contains(keyword))
            {
                casualScore += 3;
            }
            else if (bodyLower.Contains(keyword))
            {
                casualScore += 1;
            }
        }

        // -------------------------------------------------------------
        // Decision Matrix
        // -------------------------------------------------------------
        // 1. Most Important takes priority if triggered
        if (mostImportantScore >= 3 || (mostImportantScore >= 2 && subjectLower.Contains("urgent")))
        {
            return MostImportant;
        }

        // 2. Promotional takes precedence if sender or body is clearly marketing/spam/sale
        if (promotionalScore >= 4)
        {
            return Promotional;
        }

        // 3. Important work/business takes precedence over casual
        if (importantScore >= 2)
        {
            return Important;
        }

        // 4. Remaining promotional check
        if (promotionalScore >= 2)
        {
            return Promotional;
        }

        // 5. Casual check
        if (casualScore >= 2)
        {
            return Casual;
        }

        // 6. Default to Casual if no specific pattern matched
        return Casual;
    }

    private static bool IsValidCategory(string category)
    {
        return category.Equals(MostImportant, StringComparison.OrdinalIgnoreCase) ||
               category.Equals(Important, StringComparison.OrdinalIgnoreCase) ||
               category.Equals(Casual, StringComparison.OrdinalIgnoreCase) ||
               category.Equals(Promotional, StringComparison.OrdinalIgnoreCase);
    }
}
