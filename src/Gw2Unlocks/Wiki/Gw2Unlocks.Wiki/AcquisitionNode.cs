using System.Collections.Generic;

namespace Gw2Unlocks.Wiki;

public abstract record AcquisitionNode(string Title)
{
    private readonly List<AcquisitionNode> _next = [];

    public IReadOnlyCollection<AcquisitionNode> Next => _next;

    public void AddNext(AcquisitionNode node)
    {
        if (!_next.Contains(node))
            _next.Add(node);
    }
}
