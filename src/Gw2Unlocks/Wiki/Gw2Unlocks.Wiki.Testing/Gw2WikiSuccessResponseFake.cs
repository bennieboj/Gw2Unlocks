using System.Collections.ObjectModel;

namespace Gw2Unlocks.Wiki.Testing
{
    public class Gw2WikiSuccessResponseFake : IGw2WikiSource, IGw2WikiCache
    {
        public Task<ReadOnlyCollection<UnlockInfo>> GetAllUnlocks(ICollection<string> pageTitles, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public Task SaveUnlocksToCacheAsync(ReadOnlyCollection<UnlockInfo> data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}