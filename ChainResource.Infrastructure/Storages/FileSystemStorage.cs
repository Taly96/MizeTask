using ChainResource.Core.Interfaces;

namespace ChainResource.Infrastructure.Storages;

public class FileSystemStorage<T>:IStorage<T>
{
    public bool CanWrite => true;
    
    public Task<(bool isValid, T value)> TryGetValueAsync()
    {
        throw new NotImplementedException();
    }

    public Task SetValueAsync(T value)
    {
        throw new NotImplementedException();
    }
}