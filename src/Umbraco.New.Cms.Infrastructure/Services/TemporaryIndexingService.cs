﻿using Umbraco.Cms.Core.Cache;

namespace Umbraco.New.Cms.Infrastructure.Services;

public class TemporaryIndexingService : ITemporaryIndexingService
{
    private const string TempKey = "temp_indexing_op_";
    private readonly IAppPolicyCache _runtimeCache;

    public TemporaryIndexingService(AppCaches runtimeCache) => _runtimeCache = runtimeCache.RuntimeCache;

    public void Set(string indexName)
    {
        var cacheKey = TempKey + indexName;

        // put temp val in cache which is used as a rudimentary way to know when the indexing is done
        _runtimeCache.Insert(cacheKey, () => "tempValue", TimeSpan.FromMinutes(5));
    }

    public void Clear(string? indexName)
    {
        var cacheKey = TempKey + indexName;
        _runtimeCache.Clear(cacheKey);
    }
}
