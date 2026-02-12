using Microsoft.Extensions.Caching.Memory;
using HomeCenter.Models;

namespace HomeCenter.Services;

public class CachedTestFileService : ITestFileService
{
    private const string CacheKey = "HomeCenter:TopicsSync";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    private readonly ITestFileService _inner;
    private readonly IMemoryCache _cache;

    public CachedTestFileService(TestFileService inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public void SyncTopicsFromFiles(bool force = false)
    {
        if (!force && _cache.TryGetValue(CacheKey, out _))
            return;

        _inner.SyncTopicsFromFiles(force);
        _cache.Set(CacheKey, true, CacheDuration);
    }

    public IReadOnlyList<QuestionModel> LoadQuestionsForTopic(Topic topic)
        => _inner.LoadQuestionsForTopic(topic);
}
