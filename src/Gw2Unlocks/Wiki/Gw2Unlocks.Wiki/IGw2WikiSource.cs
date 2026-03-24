using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Gw2Unlocks.Wiki;

public abstract record AcquisitionNode(string Title)
{
    private readonly List<AcquisitionNode> _next = new();

    public IReadOnlyCollection<AcquisitionNode> Next => _next;

    public void AddNext(AcquisitionNode node)
    {
        if (!_next.Contains(node))
            _next.Add(node);
    }
}

public interface IGw2WikiSource
{
    Task<ReadOnlyCollection<UnlockInfo>> GetAllUnlocks(ICollection<string> pageTitles, CancellationToken cancellationToken);
}
