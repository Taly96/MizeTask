using ChainResource.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChainResource.Infrastructure.Storages;

public class MemoryStorage<T> : IStorage<T>
{
    public bool CanWrite => true;
    
    private T? _value;
    
    private DateTime? _lastUpdated;
    
    private readonly TimeSpan _expiration;
    
    private readonly ILogger<MemoryStorage<T>> _logger;
    
    private readonly object _lock = new();
    
    public MemoryStorage(ILogger<MemoryStorage<T>> logger, TimeSpan expiration)
    {
        _logger = logger;
        _expiration = expiration;
    }
    
    public Task<(bool isValid, T? value)> TryGetValueAsync()
    {
        lock (_lock)
        {
            if (_lastUpdated.HasValue)
            {
                var valueAge = DateTime.UtcNow - _lastUpdated.Value;
                
                if (valueAge <= _expiration)
                {
                    _logger.LogDebug("Memory cache hit. Value age: {Age}s", Math.Round(valueAge.TotalSeconds, 1));
                    
                    return Task.FromResult((true, _value));
                }

                _logger.LogInformation("Memory cache expired (Age: {Age}s > Limit: {Limit}s).", 
                    Math.Round(valueAge.TotalSeconds, 1), _expiration.TotalSeconds);
            }
            else
            {
                _logger.LogDebug("Memory cache is empty.");
            }
            
            return Task.FromResult((false, default(T)));
        }
    }

    public Task SetValueAsync(T value)
    {
        lock (_lock)
        {
            _value = value;
            _lastUpdated = DateTime.UtcNow;
            _logger.LogInformation("Memory cache updated. Expiration set for {Limit} minutes.", _expiration.TotalMinutes);
        }
        
        return Task.CompletedTask;
    }
}