namespace ChainResource.Core.Interfaces;

public interface IStorage<T>
{
    bool CanWrite { get; }

    Task<(bool isValid, T value)> TryGetValueAsync();
    
    Task SetValueAsync(T value);
}