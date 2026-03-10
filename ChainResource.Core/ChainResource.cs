using ChainResource.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace ChainResource.Core;

public class ChainResource<T> : IChainResource<T>
{
    private readonly IStorage<T>[] _storageChain;
    private readonly ILogger<ChainResource<T>> _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public ChainResource(IStorage<T>[] storageChain, ILogger<ChainResource<T>> logger)
    {
        if (storageChain == null || storageChain.Length == 0)
        {
            throw new ArgumentException("Storage chain cannot be empty.");
        }
        
        _storageChain = storageChain;
        _logger = logger;
    }

    public async Task<T> GetValueAsync()
    {
        var (isValid, value) = await _storageChain[0].TryGetValueAsync();
        
        if (isValid)
        {
            _logger.LogDebug("Fast-path hit on Primary Cache ({Layer}).", _storageChain[0].GetType().Name);
            
            return value!;
        }
        
        _logger.LogDebug("Primary cache miss. Acquiring lock to traverse chain...");
        await _refreshLock.WaitAsync();
        try
        {
            for (var storageChainIndex = 0; storageChainIndex < _storageChain.Length; storageChainIndex++)
            {
                var storage = _storageChain[storageChainIndex];
                var layerName = storage.GetType().Name;
                var (found, val) = await storage.TryGetValueAsync();
                
                if (found)
                {
                    _logger.LogInformation("Value successfully retrieved from {Layer}.", layerName);

                    for(var storageToUpdateIndex=storageChainIndex-1; storageToUpdateIndex>=0;storageToUpdateIndex--)
                    {
                        var storageUpdateTarget=_storageChain[storageToUpdateIndex];
                        _logger.LogDebug("Back-filling data into {TargetLayer}...", storageUpdateTarget.GetType().Name);
                        
                        if (storageUpdateTarget.CanWrite)
                        {
                            await storageUpdateTarget.SetValueAsync(val);
                        }
                    }
                    
                    return val!;
                }

                _logger.LogDebug("Cache miss on {Layer}.", layerName);
            }

            _logger.LogError("All storage layers failed to return a valid value.");
            throw new InvalidOperationException("No storage in the chain could provide a valid value.");
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}