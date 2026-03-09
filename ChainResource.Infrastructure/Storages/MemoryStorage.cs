using ChainResource.Core.Interfaces;
using ChainResource.Core.Models;

namespace ChainResource.Infrastructure.Storages;

public class MemoryStorage:IStorage<ExchangeRateList>
{
    public bool CanWrite { get; }
    
    public Task<(bool success, ExchangeRateList value)> TryGetValueAsync()
    {
        throw new NotImplementedException();
    }

    public Task SetValueAsync(ExchangeRateList value)
    {
        throw new NotImplementedException();
    }
}