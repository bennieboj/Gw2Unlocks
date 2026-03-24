using System.Collections.ObjectModel;

namespace Gw2Unlocks.Wiki.Testing
{
    public class Gw2WikiSuccessResponseFake : IGw2WikiSource, IGw2WikiCache
    {
        public Task<ReadOnlyCollection<UnlockInfo>> GetAllUnlocks(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SaveToCacheAsync<T>(string fileName, ReadOnlyCollection<T> data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}