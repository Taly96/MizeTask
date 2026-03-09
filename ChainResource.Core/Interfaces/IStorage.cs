namespace ChainResource.Core.Interfaces;

public interface IStorage<T>
{
    bool CanWrite { get; }

    Task<(bool success, T value)> TryGetValueAsync();
    
    Task SetValueAsync(T value);
}