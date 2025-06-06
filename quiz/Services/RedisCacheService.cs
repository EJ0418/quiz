using StackExchange.Redis;

public class RedisCacheService
{
    private readonly IDatabase _redis;

    private readonly IConnectionMultiplexer _connection;

    public RedisCacheService(IConnectionMultiplexer connection)
    {
        _connection = connection;
        _redis = _connection.GetDatabase();
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _redis.StringGetAsync(key);
    }

    public async Task SetAsync(string key, string value)
    {
        await _redis.StringSetAsync(key, value,TimeSpan.FromSeconds(60));
    }

    public async Task RemoveAsync(string key)
    {
        await _redis.KeyDeleteAsync(key);
    }
}
