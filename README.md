# AI-Based Email Manager System

A clean, modern email management system built on .NET 8 and Entity Framework Core that fetches emails, classifies them using a high-precision AI Classification Engine into 4 distinct categories, and displays them on an interactive dashboard.

---

## 🎯 Features

- **Native AI Classification Engine**: Built directly in C# (.NET 8) with zero external server or Python dependencies. Fast, deterministic, and 100% reliable.
- **4 Priority Categories**:
  - 🔥 **Most Important** - Urgent, critical, security alerts, and immediate actions required
  - ⭐ **Important** - Work, business proposals, meetings, invoices, and deadlines
  - 🙂 **Casual** - Personal conversations, social catch-ups, and friendly greetings
  - 🏷️ **Promotional** - Marketing discounts, newsletters, and sales promotions
- **Interactive Dashboard**: Real-time 4-panel dashboard displaying email summaries, detail preview modal, and on-demand AI reclassification.
- **Provider Flexibility**:
  - 🧪 **Mock / Test Emails** - Pre-built realistic email scenarios for instant demonstration and testing.
  - 📧 **Gmail** - Integrated Google Gmail API support with OAuth2 authentication.
  - 📨 **Outlook** - Microsoft Graph API support.
- **Database Storage**: SQL Server database with Entity Framework Core migrations.

---

## 📁 Clean Project Structure

```
EmailAssistant/
├── EmailAssistant/              # Presentation Layer (ASP.NET Core Web App & Razor Pages)
│   ├── Controllers/             # API Endpoints (EmailController)
│   ├── Pages/                   # Razor Pages (Index Dashboard)
│   ├── wwwroot/                 # Dashboard UI assets (CSS, JS)
│   ├── appsettings.json         # Application configuration & connection strings
│   └── Program.cs               # Web host & Dependency Injection
│
├── BLL/                         # Business Logic Layer
│   └── Services/                # Core Business Services
│       ├── AIClassificationService.cs   # Native High-Precision Classification Engine
│       ├── EmailProcessingService.cs   # Fetch, classify & store workflow orchestrator
│       ├── EmailFetcherService.cs      # Gmail / Outlook / Mock email fetcher
│       ├── GeminiAIService.cs          # Optional Google Gemini AI provider
│       └── MockEmailService.cs         # Coherent test email generator
│
├── DAL/                         # Data Access Layer
│   ├── Data/                    # DbContext (EmailDbContext)
│   ├── Migrations/              # EF Core Code-First Migrations
│   └── Repositories/            # Email & Category Repositories
│
├── Models/                      # Shared Models & Entities
│   ├── Entities/                # Database Entities (Email, Category)
│   ├── DTOs/                    # Data Transfer Objects
│   └── Enums/                   # Provider & category enumerations
│
└── EmailAssistant.sln           # Visual Studio Solution
```

---

## 🛠️ Technology Stack

- **Framework**: .NET 8.0 (C#)
- **Web**: ASP.NET Core Razor Pages & Web API
- **ORM**: Entity Framework Core 8.0 (Code-First)
- **Database**: Microsoft SQL Server / LocalDB
- **Frontend**: Bootstrap 5, jQuery, Font Awesome / Modern SVG Icons

---

## 🚀 Quick Start Guide

### 1. Database Configuration

In `EmailAssistant/appsettings.json`, ensure your SQL Server connection string is configured:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=EmailAssistantDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### 2. Apply Migrations

Run EF Core migrations from the solution root:

```bash
dotnet ef database update --project DAL\DAL.csproj --startup-project EmailAssistant\EmailAssistant.csproj
```

### 3. Run the Application

```bash
dotnet run --project EmailAssistant\EmailAssistant.csproj --launch-profile http
```

Open your browser at:
`http://localhost:5254`

### 4. Fetch and Classify Emails

1. Click **"Fetch Emails"** on the dashboard.
2. Select **"Test / Mock Emails"** (or Gmail/Outlook).
3. Click **"Fetch & Classify Now"**.
4. The system will process and classify the emails into the 4 category panels immediately.
