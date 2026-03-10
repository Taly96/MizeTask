namespace ChainResource.Core.Interfaces;

public interface IChainResource<T>
{
    Task<T> GetValueAsync();
}