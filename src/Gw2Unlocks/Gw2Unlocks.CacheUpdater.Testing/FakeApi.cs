namespace Gw2Unlocks.CacheUpdater.Testing;

public static class FakeApi
{
    public static string GetItemJson(int id) => $@"{{ ""id"": {id}, ""name"": ""Item {id}"", ""type"": ""Mini"", ""value"": {id * 10} }}";
}

public static class FakeItemGenerator
{
    //public static void GenerateFakeCache(ItemCache cache, int count = 100)
    //{
    //    System.ArgumentNullException.ThrowIfNull(cache);
    //    for (int i = 1; i <= count; i++)
    //        cache.SaveItem(i, FakeApi.GetItemJson(i));
    //}
}