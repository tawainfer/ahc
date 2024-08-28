using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static System.Console;

public static class Extensions
{
    private static Random r = new Random();

    public static void Shuffle<T>(this IList<T> v)
    {
        for (int i = v.Count - 1; i > 0; i--)
        {
            int j = r.Next(0, i + 1);
            var tmp = v[i];
            v[i] = v[j];
            v[j] = tmp;
        }
    }
}

public static class DeepCopy
{
    public static T Clone<T>(T obj)
    {
        ReadOnlySpan<byte> bytes = JsonSerializer.SerializeToUtf8Bytes<T>(obj);
        return JsonSerializer.Deserialize<T>(bytes)!;
    }
}

public static class SharedStopwatch
{
    private static Stopwatch _stopwatch = new Stopwatch();
    public static void Start() => _stopwatch.Start();
    public static void Stop() => _stopwatch.Stop();
    public static void Reset() => _stopwatch.Reset();
    public static long ElapsedMilliseconds() => _stopwatch.ElapsedMilliseconds;
}

public class Log
{
    private List<string> _log;
    private int _score = 0;

    public int Score { get { return _score; } }

    public Log()
    {
        _log = new();
    }

    public void Add(string s)
    {
        if (!s.StartsWith("s ") && !s.StartsWith("m "))
        {
            throw new Exception("ログに追加できるのは's 'または'm 'で始まる文字列のみです");
        }

        _log.Add(s);
        if (s.StartsWith("s ")) _score++;
    }

    public void Add(List<string> l)
    {
        foreach (string s in l) Add(s);
    }

    public void Add(Log log)
    {
        foreach (string s in log._log) Add(s);
    }

    public void Remove(int count = 1)
    {
        for (int _ = 0; _ < count; _++)
        {
            if (_log.Count <= 0) break;
            if (_log.Last().StartsWith("s ")) _score--;
            _log.RemoveAt(_log.Count - 1);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        foreach (string s in _log)
        {
            sb.Append(s);
            sb.Append("\n");
        }
        return sb.ToString();
    }
}

public class A
{
    private List<int> _a;
    private int _la;

    public int Count { get { return _a.Count; } }

    public A(int la)
    {
        _a = new();
        _la = la;
    }

    public void Add(int x)
    {
        if (_a.Count >= _la) throw new Exception("Aの長さが_laより大きくなりました");
        _a.Add(x);
    }

    public void AddRange(List<int> l)
    {
        foreach (int x in l) Add(x);
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        for (int i = 0; i < _la; i++)
        {
            sb.Append((i >= _a.Count ? 0 : _a[i]).ToString());
            if (i != _la - 1) sb.Append(" ");
        }

        return sb.ToString();
    }
}

public class Field
{
    private int _n;
    private int _m;
    private int _la;
    private int _lb;
    private List<int> _u;
    private List<int> _v;
    private List<int> _x;
    private List<int> _y;
    private List<List<int>> _graph;
    private List<List<int>> _area;
    private HashSet<int>[] _nodeToAreaId;
    private List<List<int>> _areaGraph;
    private Dictionary<(int AreaIdFrom, int AreaIdTo), (int NodeFrom, int NodeTo)> _port;
    private int _currentNode = 0;
    private Log _log;
    private Dictionary<(int Id, int NodeFrom, int NodeTo), (List<int> Path, int UpdateAreaCount)> _shortestPathInArea;
    private Dictionary<(int IdFrom, int IdTo), (List<int> Path, int UpdateAreaCount)> _shortestPathAreaToArea;
    private A _a;
    private int[] _b;
    private Dictionary<int, int> _areaIdToAIndex;
    private int _lastUsedAreaId = -1;
    private int _updateAreaCount = 0;

    public int Score { get { return _log.Score; } }
    public Log Log { get { return _log; } }
    public A A { get { return _a; } }

    public Field(int n, int m, int la, int lb, in List<int> u, in List<int> v, in List<int> x, in List<int> y)
    {
        _n = n;
        _m = m;
        _la = la;
        _lb = lb;
        _u = DeepCopy.Clone(u);
        _v = DeepCopy.Clone(v);
        _x = DeepCopy.Clone(x);
        _y = DeepCopy.Clone(y);
        _graph = new();
        _area = new();
        _nodeToAreaId = new HashSet<int>[_n];
        for (int i = 0; i < _n; i++) _nodeToAreaId[i] = new();
        _areaGraph = new();
        _port = new();
        _log = new();
        _shortestPathInArea = new();
        _shortestPathAreaToArea = new();
        _a = new(_la);
        _b = new int[_lb];
        Array.Fill(_b, -1);
        _areaIdToAIndex = new();

        MakeGraph();
        MakeArea();
        AddArea();
    }

    private void MakeGraph()
    {
        _graph = new();
        for (int _ = 0; _ < _n; _++) _graph.Add(new());

        for (int i = 0; i < _m; i++)
        {
            _graph[_u[i]].Add(_v[i]);
            _graph[_v[i]].Add(_u[i]);
        }
        for (int i = 0; i < _n; i++) _graph[i].Sort();
    }

    private void ConnectArea()
    {
        for (int id = 0; id < _area.Count; id++)
        {
            foreach (int cp in _area[id])
            {
                foreach (int ep in _graph[cp])
                {
                    foreach (int id1 in _nodeToAreaId[cp])
                    {
                        foreach (int id2 in _nodeToAreaId[ep])
                        {
                            _port[(id1, id2)] = (cp, ep);
                        }
                    }
                }
            }
        }
    }

    private void MakeAreaGraph()
    {
        _areaGraph = new();
        for (int _ = 0; _ < _area.Count; _++) _areaGraph.Add(new());

        for (int id1 = 0; id1 < _area.Count; id1++)
        {
            for (int id2 = id1 + 1; id2 < _area.Count; id2++)
            {
                if (_port.ContainsKey((id1, id2)))
                {
                    _areaGraph[id1].Add(id2);
                    _areaGraph[id2].Add(id1);
                }
            }
        }
    }

    private void MakeArea()
    {
        _area = new();

        void dfs(int node, List<int> path, ref HashSet<int> seen, ref HashSet<int> confirm)
        {
            foreach (int neighbor in _graph[node])
            {
                if (seen.Contains(neighbor)) continue;
                seen.Add(neighbor);
                var newPath = DeepCopy.Clone(path);
                newPath.Add(neighbor);
                dfs(neighbor, newPath, ref seen, ref confirm);
            }

            List<int> groupCandidates = new();
            while (path.Count >= 1)
            {
                if (confirm.Contains(path.Last())) break;
                confirm.Add(path.Last());
                groupCandidates.Add(path.Last());
                path.RemoveAt(path.Count - 1);
            }

            if (groupCandidates.Count >= 1)
            {
                int idx = 0;
                bool isContinue = true;
                do
                {
                    List<int> group;
                    if (idx + _lb - 1 < groupCandidates.Count)
                    {
                        group = groupCandidates.GetRange(idx, _lb);
                    }
                    else
                    {
                        idx = Math.Max(groupCandidates.Count - _lb, 0);
                        group = groupCandidates.GetRange(idx, Math.Min(groupCandidates.Count - idx, _lb));
                        isContinue = false;
                    }

                    _areaIdToAIndex[_area.Count] = _a.Count + idx;
                    foreach (int x in group)
                    {
                        _nodeToAreaId[x].Add(_area.Count);
                    }
                    _area.Add(group);

                    idx += _lb / 2;
                } while (isContinue);

                _a.AddRange(groupCandidates);
            }
        }

        int startNode = 0;
        HashSet<int> seen = new();
        HashSet<int> confirm = new();
        dfs(startNode, new() { startNode, }, ref seen, ref confirm);

        ConnectArea();
        MakeAreaGraph();
        _updateAreaCount++;
    }

    private void AddArea()
    {
        int remainingCountA = _la - _n;
        int beforeChangeAreaCount = _area.Count;

        while (SharedStopwatch.ElapsedMilliseconds() <= 1750)
        {
            int id1 = new Random().Next(beforeChangeAreaCount);
            int id2 = new Random().Next(beforeChangeAreaCount);
            if (GetShortestPathAreaToArea(id1, id2).Count <= 4) continue;

            HashSet<int> seen = new() { _area[id1][0], };
            Queue<(int Cp, LinkedList<int> Path)> q = new();
            q.Enqueue((_area[id1][0], new(new[] { _area[id1][0], })));

            LinkedList<int> shortestPath = new();
            while (q.Count >= 1)
            {
                (int cp, LinkedList<int> path) = q.Dequeue();
                if (cp == _area[id2][0])
                {
                    shortestPath = path;
                    break;
                }

                foreach (int ep in _graph[cp])
                {
                    if (seen.Contains(ep)) continue;
                    seen.Add(ep);
                    var newPath = DeepCopy.Clone(path);
                    newPath.AddLast(ep);
                    q.Enqueue((ep, newPath));
                }
            }

            while (shortestPath.Count >= 2)
            {
                LinkedListNode<int> firstNode = shortestPath.First!;
                LinkedListNode<int> nextNode = firstNode.Next!;
                if (!_nodeToAreaId[nextNode.Value].Contains(id1)) break;
                shortestPath.RemoveFirst();
            }

            while (shortestPath.Count >= 2)
            {
                LinkedListNode<int> lastNode = shortestPath.Last!;
                LinkedListNode<int> previousNode = lastNode.Previous!;
                if (!_nodeToAreaId[previousNode.Value].Contains(id2)) break;
                shortestPath.RemoveLast();
            }

            while (shortestPath.Count > _lb)
            {
                shortestPath.RemoveLast();
            }

            if (shortestPath.Count > remainingCountA) continue;
            remainingCountA -= shortestPath.Count;

            _areaIdToAIndex[_area.Count] = _a.Count;
            foreach (int node in shortestPath)
            {
                _nodeToAreaId[node].Add(_area.Count);
            }

            List<int> shortestPathList = new(shortestPath);
            _area.Add(shortestPathList);
            _a.AddRange(shortestPathList);
        }

        ConnectArea();
        MakeAreaGraph();
        _updateAreaCount++;
    }

    private List<int> GetShortestPathInArea(int id, int nodeFrom, int nodeTo)
    {
        if (_shortestPathInArea.ContainsKey((id, nodeFrom, nodeTo))
            && _shortestPathInArea[(id, nodeFrom, nodeTo)].UpdateAreaCount == _updateAreaCount)
        {
            return DeepCopy.Clone(_shortestPathInArea[(id, nodeFrom, nodeTo)].Path);
        }

        if (!_nodeToAreaId[nodeFrom].Contains(id))
        {
            throw new Exception($"頂点{nodeFrom}はエリア{id}に属していません");
        }

        if (!_nodeToAreaId[nodeTo].Contains(id))
        {
            throw new Exception($"頂点{nodeTo}はエリア{id}に属していません");
        }

        HashSet<int> seen = new() { nodeFrom, };
        Queue<(int Node, List<int> Path)> q = new();
        q.Enqueue((nodeFrom, new() { nodeFrom, }));

        while (q.Count >= 1)
        {
            (int node, List<int> path) = q.Dequeue();

            if (!_shortestPathInArea.ContainsKey((id, nodeFrom, nodeTo))
                || _shortestPathInArea[(id, nodeFrom, nodeTo)].UpdateAreaCount != _updateAreaCount)
            {
                _shortestPathInArea[(id, nodeFrom, node)] = (
                    DeepCopy.Clone(path),
                    _updateAreaCount
                );
            }

            if (node == nodeTo) break;

            foreach (int neighbor in _graph[node])
            {
                if (!_nodeToAreaId[neighbor].Contains(id)) continue;
                if (seen.Contains(neighbor)) continue;
                seen.Add(neighbor);
                var newPath = DeepCopy.Clone(path);
                newPath.Add(neighbor);
                q.Enqueue((neighbor, newPath));
            }
        }

        return DeepCopy.Clone(_shortestPathInArea[(id, nodeFrom, nodeTo)].Path);
    }

    private List<int> GetShortestPathAreaToArea(int idFrom, int idTo)
    {
        if (_shortestPathAreaToArea.ContainsKey((idFrom, idTo))
            && _shortestPathAreaToArea[(idFrom, idTo)].UpdateAreaCount == _updateAreaCount)
        {
            return DeepCopy.Clone(_shortestPathAreaToArea[(idFrom, idTo)].Path);
        }

        HashSet<int> seen = new() { idFrom, };
        Queue<(int Node, List<int> Path)> q = new();
        q.Enqueue((idFrom, new() { idFrom, }));

        while (q.Count >= 1)
        {
            (int node, List<int> path) = q.Dequeue();
            _shortestPathAreaToArea[(idFrom, node)] = (DeepCopy.Clone(path), _updateAreaCount);

            if (!_shortestPathAreaToArea.ContainsKey((idFrom, node))
                || _shortestPathAreaToArea[(idFrom, node)].UpdateAreaCount != _updateAreaCount)
            {
                _shortestPathAreaToArea[(idFrom, node)] = (
                    DeepCopy.Clone(path),
                    _updateAreaCount
                );
            }

            if (node == idTo) break;

            foreach (int neighbor in _areaGraph[node])
            {
                if (seen.Contains(neighbor)) continue;
                seen.Add(neighbor);
                var newPath = DeepCopy.Clone(path);
                newPath.Add(neighbor);
                q.Enqueue((neighbor, newPath));
            }
        }

        return DeepCopy.Clone(_shortestPathAreaToArea[(idFrom, idTo)].Path);
    }

    private Log Sign(int areaId, ref int lastUsedAreaId)
    {
        Log log = new();
        int l = _area[areaId].Count;
        int pa = _areaIdToAIndex[areaId];
        int pb = 0;

        lastUsedAreaId = areaId;
        log.Add($"s {l} {pa} {pb}");
        return log;
    }

    public Log Move(int destinationNode, bool simulation = false, bool ignoreArea = false)
    {
        int minScore = int.MaxValue;
        Log minScoreLog = new();
        int minScoreLastUsedAreaId = -1;

        foreach (int startAreaId in _nodeToAreaId[_currentNode])
        {
            foreach (int destinationAreaId in _nodeToAreaId[destinationNode])
            {
                Log log = new();
                int currentNode = _currentNode;
                int lastUsedAreaId = _lastUsedAreaId;

                var areaToAreaPath = GetShortestPathAreaToArea(startAreaId, destinationAreaId);
                for (int i = 0; i < areaToAreaPath.Count - 1; i++)
                {
                    if (lastUsedAreaId != areaToAreaPath[i])
                    {
                        log.Add(Sign(areaToAreaPath[i], ref lastUsedAreaId));
                    }

                    if (i != 0)
                    {
                        log.Add($"m {_port[(areaToAreaPath[i - 1], areaToAreaPath[i])].NodeTo}");
                        currentNode = _port[(areaToAreaPath[i - 1], areaToAreaPath[i])].NodeTo;
                    }

                    var inAreaPath = GetShortestPathInArea(
                        areaToAreaPath[i],
                        currentNode,
                        _port[(areaToAreaPath[i], areaToAreaPath[i + 1])].NodeFrom
                    );

                    for (int j = 1; j < inAreaPath.Count; j++)
                    {
                        int nextNode = inAreaPath[j];
                        log.Add($"m {nextNode}");
                    }
                    currentNode = inAreaPath.Last();
                }

                if (lastUsedAreaId != destinationAreaId)
                {
                    log.Add(Sign(destinationAreaId, ref lastUsedAreaId));
                }

                if (areaToAreaPath.Count >= 2)
                {
                    log.Add($"m {_port[(areaToAreaPath[areaToAreaPath.Count - 2], areaToAreaPath[areaToAreaPath.Count - 1])].NodeTo}");
                    currentNode = _port[(areaToAreaPath[areaToAreaPath.Count - 2], areaToAreaPath[areaToAreaPath.Count - 1])].NodeTo;
                }

                var inAreaPath2 = GetShortestPathInArea(
                    areaToAreaPath.Last(),
                    currentNode,
                    destinationNode
                );

                for (int i = 1; i < inAreaPath2.Count; i++)
                {
                    int nextNode = inAreaPath2[i];
                    log.Add($"m {nextNode}");
                }
                currentNode = inAreaPath2.Last();

                if (log.Score < minScore)
                {
                    minScore = log.Score;
                    minScoreLog = log;
                    minScoreLastUsedAreaId = lastUsedAreaId;
                }
            }
        }

        if (!simulation)
        {
            _log.Add(minScoreLog);
            _currentNode = destinationNode;
            _lastUsedAreaId = minScoreLastUsedAreaId;
        }

        return minScoreLog;
    }

    public Log Illumination()
    {
        Log log = new();
        for (int id = 0; id < _area.Count; id++)
        {
            int l = _area[id].Count;
            int pa = _areaIdToAIndex[id];
            int pb = 0;

            log.Add($"s {l} {pa} {pb}");
        }

        return log;
    }

    public override string ToString()
    {
        StringBuilder sb = new();

        sb.Append("[_graph]\n");
        for (int i = 0; i < _graph.Count; i++)
        {
            sb.Append($"_graph[{i}]: ");
            sb.Append(string.Join(',', _graph[i]));
            sb.Append("\n");
        }

        sb.Append("[_area]\n");
        for (int i = 0; i < _area.Count; i++)
        {
            sb.Append($"_area[{i}]: ");
            sb.Append(string.Join(',', _area[i]));
            sb.Append("\n");
        }

        sb.Append("[_port]\n");
        for (int id1 = 0; id1 < _area.Count; id1++)
        {
            for (int id2 = 0; id2 < _area.Count; id2++)
            {
                if (!_port.ContainsKey((id1, id2))) continue;
                sb.Append($"_port[({id1}, {id2})]: {_port[(id1, id2)]}");
                sb.Append("\n");
            }
        }

        sb.Append("[_areaGraph]\n");
        for (int i = 0; i < _areaGraph.Count; i++)
        {
            sb.Append($"_areaGraph[{i}]: ");
            sb.Append(string.Join(',', _areaGraph[i]));
            sb.Append("\n");
        }

        // int maxCountAreaToArea = int.MinValue;
        // for (int i = 0; i < _area.Count; i++)
        // {
        //     for (int j = 0; j < _area.Count; j++)
        //     {
        //         maxCountAreaToArea = Math.Max(_shortestPathAreaToArea[(i, j)].Count, maxCountAreaToArea);
        //     }
        // }
        // sb.Append($"maxCountAreaToArea: {maxCountAreaToArea}\n");

        // int maxCountInArea = int.MinValue;
        // for (int id = 0; id < _area.Count; id++)
        // {
        //     for (int cp = 0; cp < _n; cp++)
        //     {
        //         for (int ep = 0; ep < _n; ep++)
        //         {
        //             if (!_shortestPathInArea[id].ContainsKey((cp, ep))) continue;
        //             maxCountInArea = Math.Max(_shortestPathInArea[id][(cp, ep)].Count, maxCountInArea);
        //         }
        //     }
        // }
        // sb.Append($"maxCountInArea: {maxCountInArea}\n");

        return sb.ToString();
    }
}

public class Program
{
    private int _n;
    private int _m;
    private int _t;
    private int _la;
    private int _lb;
    private List<int> _u;
    private List<int> _v;
    private List<int> _order;
    private List<int> _x;
    private List<int> _y;

    public static void Main(string[] args)
    {
        new Program();
    }

    public Program()
    {
        SharedStopwatch.Start();

        _u = new();
        _v = new();
        _order = new();
        _x = new();
        _y = new();

        Input();
        Solve();
    }

    private void Input()
    {
        var tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
        (_n, _m, _t, _la, _lb) = (tmp[0], tmp[1], tmp[2], tmp[3], tmp[4]);

        for (int _ = 0; _ < _m; _++)
        {
            tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
            _u.Add(tmp[0]);
            _v.Add(tmp[1]);
        }

        _order = ReadLine()!.Split().Select(int.Parse).ToList();

        for (int _ = 0; _ < _n; _++)
        {
            tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
            _x.Add(tmp[0]);
            _y.Add(tmp[1]);
        }
    }

    // private void Solve()
    // {
    //     int minScore = int.MaxValue;
    //     Field? minScoreField = null;
    //     while (SharedStopwatch.ElapsedMilliseconds() <= 2500)
    //     {
    //         List<int> scores = new();
    //         var field = new Field(_n, _m, _la, _lb, _u, _v, _x, _y);
    //         foreach (int destinationNode in _order)
    //         {
    //             scores.Add(field.Move(destinationNode).Score);
    //         }
    //         // WriteLine(string.Join(',', scores));

    //         int score = scores.Sum();
    //         if (scores.Sum() < minScore)
    //         {
    //             WriteLine($"{minScore} => {score}");
    //             minScore = score;
    //             minScoreField = field;
    //         }

    //         WriteLine($"{SharedStopwatch.ElapsedMilliseconds()}ms"); // debug
    //     }

    //     WriteLine(minScoreField!.A);
    //     WriteLine(minScoreField!.Log);
    // }

    private void Solve()
    {
        var field = new Field(_n, _m, _la, _lb, _u, _v, _x, _y);

        foreach (int destinationNode in _order)
        {
            field.Move(destinationNode);
        }

        WriteLine(field.A);
        WriteLine(field.Log);
        // WriteLine(field.Illumination());
    }
}
