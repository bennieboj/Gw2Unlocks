namespace Gw2Unlocks.Wiki.Testing
{
    public class Gw2WikiSuccessResponseFake : IGw2WikiSource, IGw2WikiCache
    {
        public Task<AcquisitionGraph> GetAcquisitionGraph(IEnumerable<string> itemNames, AcquisitionGraph? existingGraph, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }


        public Task SaveAcquisitionGraphToCacheAsync(AcquisitionGraph data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}