using ChainResource.Core.Interfaces;

namespace ChainResource.Infrastructure.Storages;

public class MemoryStorage<T>:IStorage<T>
{
    public bool CanWrite => true;

    private T _value;
    
    private readonly TimeSpan _expiration;
    
    private readonly object _lock = new();
    
    private DateTime? _lastUpdated;

    public MemoryStorage(TimeSpan expiration)
    {
        _expiration = expiration;
    }
    public Task<(bool isValid, T value)> TryGetValueAsync()
    {
        lock (_lock)
        {
            if (_lastUpdated.HasValue && (DateTime.UtcNow - _lastUpdated.Value) <= _expiration)
            {
                return Task.FromResult((true, _value));
            }
            
            return Task.FromResult((false, _value));
        }
    }

    public Task SetValueAsync(T value)
    {
        lock (_lock)
        {
            _value = value;
            _lastUpdated = DateTime.Now;
        }
        
        return Task.CompletedTask;
    }
}