using System.Text.Json;
using ChainResource.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChainResource.Infrastructure.Storages;

public class FileSystemStorage<T> : IStorage<T>
{
    public bool CanWrite => true;

    private readonly string _filePath;
    
    private readonly TimeSpan _expiration;
    
    private readonly ILogger<FileSystemStorage<T>> _logger;
    
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    
    private readonly int _bufferSize;
    
    public FileSystemStorage(string filePath, TimeSpan expiration, int bufferSize, ILogger<FileSystemStorage<T>> logger)
    {
        _filePath = filePath;
        _expiration = expiration;
        _bufferSize = bufferSize;
        _logger = logger;
    }

    public async Task<(bool isValid, T? value)> TryGetValueAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.LogDebug("File cache miss: File not found at {Path}.", _filePath);
                
                return (false, default);
            }

            var fileInfo = new FileInfo(_filePath);
            var fileContentAge = DateTime.UtcNow - fileInfo.LastWriteTimeUtc;
            
            if (fileContentAge >= _expiration)
            {
                _logger.LogInformation("File cache expired (Age: {Age}s > Limit: {Limit}s).", 
                    Math.Round(fileContentAge.TotalSeconds, 1), _expiration.TotalSeconds);
                
                return (false, default);
            }

            await using var fileStream = new FileStream(
                _filePath, FileMode.Open, FileAccess.Read, FileShare.Read, _bufferSize, useAsync: true);
                
            var value = await JsonSerializer.DeserializeAsync<T>(fileStream);
            
            if (value != null)
            {
                _logger.LogDebug("Value successfully retrieved from File Storage.");
                
                return (true, value);
            }

            _logger.LogWarning("File Storage returned null after deserialization.");
            
            return (false, default);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to read or deserialize file cache from {Path}.", _filePath);
            
            return (false, default);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task SetValueAsync(T value)
    {
        await _fileLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                
                _logger.LogDebug("Created directory {Directory} for file cache.", directory);
            }

            await using var fileStream = new FileStream(
                _filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, useAsync: true);
            await JsonSerializer.SerializeAsync(fileStream, value);
            
            _logger.LogInformation("Successfully saved data to File Storage at {Path}.", _filePath);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to write file cache to {Path}.", _filePath);
        }
        finally
        {
            _fileLock.Release();
        }
    }
}