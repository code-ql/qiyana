using System.Collections.Generic;
using qiyana.Models.MatchData;

namespace qiyana.Services;

public class GameDetailCache
{
    private const int Capacity = 568;

    private readonly LinkedList<long> _list = new();
    private readonly Dictionary<long, (LinkedListNode<long> Node, GameDetail Value)> _map = new();

    public GameDetail? Get(long gameId)
    {
        if (!_map.TryGetValue(gameId, out var entry))
            return null;

        _list.Remove(entry.Node);
        _list.AddFirst(entry.Node);
        return entry.Value;
    }

    public void Clear()
    {
        _list.Clear();
        _map.Clear();
    }

    public void Set(long gameId, GameDetail detail)
    {
        if (_map.TryGetValue(gameId, out var entry))
        {
            _list.Remove(entry.Node);
            _map.Remove(gameId);
        }

        if (_map.Count >= Capacity)
        {
            var last = _list.Last!;
            _map.Remove(last.Value);
            _list.RemoveLast();
        }

        var node = _list.AddFirst(gameId);
        _map[gameId] = (node, detail);
    }
}
