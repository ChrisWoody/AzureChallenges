using System.Collections.Concurrent;
using System.Text.Json;

namespace AzureChallenges.Data;

public class StateCache
{
    private readonly ConcurrentDictionary<string, State> _dict = new();

    private readonly StateStorageService _stateStorageService;

    public StateCache(StateStorageService stateStorageService)
    {
        _stateStorageService = stateStorageService;
    }

    public State Get(string key)
    {
        return _dict.GetOrAdd(key, key =>
        {
            var content = _stateStorageService.GetFile(key);
            return content == null ? new State() : JsonSerializer.Deserialize<State>(content);
        });
    }

    public async Task SetAsync(string key, State state)
    {
        using var mutex = new Mutex(true, key);
        mutex.WaitOne(TimeSpan.FromSeconds(30));
        _stateStorageService.SaveFile(key, JsonSerializer.SerializeToUtf8Bytes(state));
        _dict[key] = state;
        mutex.ReleaseMutex();
    }

    public async Task ClearKeyFromCache(string key)
    {
        _dict.TryRemove(key, out _);
    }
}