ChainResource - Multi-Layered Exchange Rate Provider
A robust, thread-safe .NET console application designed to retrieve and cache exchange rate data across multiple storage layers.

## Project Overview
The application ensures high availability and performance by checking for data in order of speed and cost:

Memory Storage: Ultra-fast, volatile primary cache.

File System Storage: Persistent local backup (JSON) for offline availability.

Web Service Storage: The "Source of Truth" (Open Exchange Rates API).

## Key Features
Atomic Orchestration: Prevents "Cache Stampede" using SemaphoreSlim to ensure only one thread fetches data from slow/expensive sources.

Automatic Back-filling: When data is found in a lower layer (e.g., Web), it is automatically propagated back up to faster layers (File/Memory).

Resiliency: Gracefully handles API failures or missing files by traversing the remaining chain.

Strongly-Typed Configuration: Uses IOptions patterns and appsettings.json for environment-specific settings.

## Getting Started
Prerequisites
.NET 8.0 SDK

An API Key from Open Exchange Rates.

Configuration
Use the appsettings.json file in the ChainResource.Runner project root and modify it according to your needs.

Running the Application
dotnet run --project ChainResource.Runner

## Testing
The project includes a comprehensive suite of xUnit and Moq tests:

Unit Tests: Validating expiration logic and JSON serialization.

Integration Tests: Testing physical file I/O.

Concurrency Tests: Stress-testing the orchestrator to ensure only a single web call is made during simultaneous requests.

To run the tests:

Bash
dotnet test


