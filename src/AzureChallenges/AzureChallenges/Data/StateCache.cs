using System.Collections.Concurrent;

namespace AzureChallenges.Data;

public class StateCache
{
    private readonly ConcurrentDictionary<string, State> _dict = new();

    public StateCache()
    {

    }

    public State? Get(string key)
    {
        return _dict.TryGetValue(key, out var state) ? state : null;
    }

    public void Set(string key, State value)
    {
        _dict[key] = value;
    }
}