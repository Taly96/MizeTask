using ChainResource.Core.Interfaces;
using ChainResource.Core.Models;

namespace ChainResource.Core;

public class ChainResource:IChainResource<ExchangeRateList>
{
    private readonly List<IStorage<ExchangeRateList>> _storageChain;
    
    public Task<ExchangeRateList> GetValueAsync()
    {
        throw new NotImplementedException();
    }
}