# FinanceComplianceAgentBackEnd

Advanced .NET 8 Web API for AI-powered financial compliance and AML analysis.

## Overview

This project implements a backend service that analyzes financial transactions against compliance rules and detects suspicious patterns using an AI agent based on Microsoft Semantic Kernel.

It supports:
- SQL Server persistence via Entity Framework Core
- Dual AI provider configuration: Ollama and OpenAI
- Rule-based compliance evaluation
- REST API endpoints for transaction analysis
- Swagger integration for testing

## Tech Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Microsoft Semantic Kernel
- Swagger / OpenAPI
- Ollama / OpenAI integration

## Project Structure

- `FinanceCompliance.Api/` - main API project
- `FinanceCompliance.Api/Controllers/` - API controllers
- `FinanceCompliance.Api/Services/` - AI and business services
- `FinanceCompliance.Api/Data/` - EF Core DbContext and data models
- `FinanceCompliance.Api/Models/` - request/response models

## Features

- Load active compliance rules from SQL Server
- Read pending financial transactions
- Send transaction data and rules to the AI agent
- Evaluate anomalies, risk indicators, and rule violations
- Return a structured compliance assessment

## Configuration

Edit `FinanceCompliance.Api/appsettings.json` and configure your database and AI provider:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FinanceComplianceDb;User Id=sa;Password=YourStrong@Password123!;TrustServerCertificate=True;"
  },
  "AiSettings": {
    "Provider": "Ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "gemma2:2b"
    },
    "OpenAi": {
      "Endpoint": "https://api.openai.com/v1",
      "ApiKey": "YOUR_API_KEY",
      "ModelId": "gpt-4o-mini"
    }
  }
}
