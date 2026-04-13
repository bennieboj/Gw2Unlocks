using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki.Testing
{
    public class Gw2WikiSuccessResponseFake : IGw2WikiSource, IGw2WikiCache
    {
        public Task<Collection<string>> GetAllPages(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string?> GetSinglePage(string title, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SaveAllPagesToCacheAsync(Collection<string> data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<string> StreamAllPages(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StreamPagesToCacheAsync(IAsyncEnumerable<string> pages, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}