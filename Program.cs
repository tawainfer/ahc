using System.Diagnostics;
using Newtonsoft.Json;
using static System.Console;
using System.Text;

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
        string json = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<T>(json)!;
    }
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

    // public void Output()
    // {
    //     foreach (string s in _log)
    //     {
    //         WriteLine(s);
    //     }
    // }

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

public class Field
{
    private int _n;
    private int _m;
    private int _la;
    private int _lb;
    private List<int> _u;
    private List<int> _v;
    private List<List<int>> _graph;
    private List<List<int>> _area;
    private Dictionary<int, int> _nodeToAreaId;
    private List<List<int>> _areaGraph;
    private Dictionary<(int AreaIdFrom, int AreaIdTo), int> _port;
    private int _currentNode = 0;
    private Log _log;
    private Dictionary<(int NodeFrom, int NodeTo), List<int>> _shortestPathInArea;
    private Dictionary<(int AreaIdFrom, int AreaIdTo), List<int>> _shortestPathAreaToArea;
    private List<int> _a;
    private int[] _b;
    private Dictionary<int, int> _areaIdToAIndex;
    private Dictionary<int, int> _nodeToAIndex;
    private int _lastUsedAreaId = -1;

    public int Score { get { return _log.Score; } }
    public Log Log { get { return _log; } }

    public Field(int n, int m, int la, int lb, in List<int> u, in List<int> v)
    {
        _n = n;
        _m = m;
        _la = la;
        _lb = lb;
        _u = DeepCopy.Clone(u);
        _v = DeepCopy.Clone(v);
        _graph = new();
        _area = new();
        _nodeToAreaId = new();
        _areaGraph = new();
        _port = new();
        _log = new();
        _shortestPathInArea = new();
        _shortestPathAreaToArea = new();
        _a = new();
        _b = new int[_lb];
        _areaIdToAIndex = new();
        _nodeToAIndex = new();

        MakeGraph();
        MakeArea();
        ConnectArea();
        MakeAreaGraph();
        MakeShortestPathInArea();
        MakeShortestPathAreaToArea();
        MakeSign();
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

    private void MakeArea()
    {
        _area = new();
        bool[] seen = new bool[_n];

        List<int> idx = new();
        for (int i = 0; i < _n; i++) idx.Add(i);
        idx.Shuffle();

        foreach (int i in idx)
        {
            if (seen[i]) continue;
            seen[i] = true;

            Queue<int> q = new();
            q.Enqueue(i);

            List<int> group = new() { i, };
            while (q.Count >= 1 && group.Count < _lb)
            {
                int cp = q.Dequeue();

                foreach (int ep in _graph[cp])
                {
                    if (group.Count + 1 > _lb) break;
                    if (seen[ep]) continue;
                    seen[ep] = true;
                    group.Add(ep);
                    q.Enqueue(ep);
                }
            }

            _area.Add(group);
            foreach (int x in group)
            {
                _nodeToAreaId[x] = _area.Count - 1;
            }
        }
    }

    private void ConnectArea()
    {
        for (int id = 0; id < _area.Count; id++)
        {
            foreach (int cp in _area[id])
            {
                foreach (int ep in _graph[cp])
                {
                    _port[(_nodeToAreaId[cp], _nodeToAreaId[ep])] = cp;
                    _port[(_nodeToAreaId[ep], _nodeToAreaId[cp])] = ep;
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

    private void MakeShortestPathInArea()
    {
        _shortestPathInArea = new();

        for (int id = 0; id < _area.Count; id++)
        {
            foreach (int sp in _area[id])
            {
                Dictionary<int, bool> seen = new();
                foreach (int tp in _area[id]) seen.Add(tp, false);
                seen[sp] = true;

                Queue<(int Cp, List<int> Path)> q = new();
                q.Enqueue((sp, new() { sp, }));

                while (q.Count >= 1)
                {
                    (int cp, List<int> path) = q.Dequeue();
                    _shortestPathInArea[(sp, cp)] = DeepCopy.Clone(path);

                    foreach (int ep in _graph[cp])
                    {
                        if (_nodeToAreaId[ep] != id) continue;
                        if (seen[ep]) continue;
                        seen[ep] = true;
                        var newPath = DeepCopy.Clone(path);
                        newPath.Add(ep);
                        q.Enqueue((ep, newPath));
                    }
                }
            }
        }
    }

    private void MakeShortestPathAreaToArea()
    {
        _shortestPathAreaToArea = new();

        for (int sp = 0; sp < _area.Count; sp++)
        {
            bool[] seen = new bool[_area.Count];
            seen[sp] = true;

            Queue<(int Cp, List<int> Path)> q = new();
            q.Enqueue((sp, new() { sp, }));

            while (q.Count >= 1)
            {
                (int cp, List<int> path) = q.Dequeue();
                _shortestPathAreaToArea[(sp, cp)] = DeepCopy.Clone(path);

                foreach (int ep in _areaGraph[cp])
                {
                    if (seen[ep]) continue;
                    seen[ep] = true;
                    var newPath = DeepCopy.Clone(path);
                    newPath.Add(ep);
                    q.Enqueue((ep, newPath));
                }
            }
        }
    }

    private void MakeSign()
    {
        _a = new();
        Array.Fill(_b, -1);

        for (int id = 0; id < _area.Count; id++)
        {
            _areaIdToAIndex[id] = _a.Count;
            foreach (int node in _area[id])
            {
                _nodeToAIndex[node] = _a.Count;
                _a.Add(node);
            }
        }

        while (_a.Count < _la) _a.Add(0);
    }

    private Log Sign(int areaId)
    {
        Log log = new();
        int l = _area[areaId].Count;
        int pa = _areaIdToAIndex[areaId];
        int pb = 0;

        _lastUsedAreaId = areaId;
        log.Add($"s {l} {pa} {pb}");
        return log;
    }

    public Log Move(int destinationNode, bool simulation = false, bool ignoreArea = false)
    {
        Log log = new();
        int currentNode = _currentNode;

        var areaToAreaPath = _shortestPathAreaToArea[(
            _nodeToAreaId[currentNode],
            _nodeToAreaId[destinationNode]
        )];
        for (int i = 0; i < areaToAreaPath.Count - 1; i++)
        {
            if (_lastUsedAreaId != areaToAreaPath[i])
            {
                log.Add(Sign(areaToAreaPath[i]));
            }

            if (i != 0)
            {
                log.Add($"m {_port[(areaToAreaPath[i], areaToAreaPath[i - 1])]}");
                currentNode = _port[(areaToAreaPath[i], areaToAreaPath[i - 1])];
            }

            var inAreaPath = _shortestPathInArea[(
                currentNode,
                _port[(areaToAreaPath[i], areaToAreaPath[i + 1])]
            )];
            for (int j = 1; j < inAreaPath.Count; j++)
            {
                int nextNode = inAreaPath[j];
                log.Add($"m {nextNode}");
            }
            currentNode = inAreaPath.Last();
        }

        if (_lastUsedAreaId != _nodeToAreaId[destinationNode])
        {
            log.Add(Sign(_nodeToAreaId[destinationNode]));
        }

        if (_nodeToAreaId[currentNode] != _nodeToAreaId[destinationNode])
        {
            log.Add($"m {_port[(_nodeToAreaId[destinationNode], _nodeToAreaId[currentNode])]}");
            currentNode = _port[(_nodeToAreaId[destinationNode], _nodeToAreaId[currentNode])];
        }

        var inAreaPath2 = _shortestPathInArea[(currentNode, destinationNode)];
        for (int i = 1; i < inAreaPath2.Count; i++)
        {
            int nextNode = inAreaPath2[i];
            log.Add($"m {nextNode}");
        }
        currentNode = inAreaPath2.Last();

        if (!simulation)
        {
            _log.Add(log);
            _currentNode = currentNode;
        }

        return log;
    }

    public void OutputLog()
    {
        WriteLine(string.Join(' ', _a));
        WriteLine(_log);
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

        int maxCountAreaToArea = int.MinValue;
        for (int i = 0; i < _area.Count; i++)
        {
            for (int j = 0; j < _area.Count; j++)
            {
                maxCountAreaToArea = Math.Max(_shortestPathAreaToArea[(i, j)].Count, maxCountAreaToArea);
            }
        }
        sb.Append($"maxCountAreaToArea: {maxCountAreaToArea}\n");

        int maxCountInArea = int.MinValue;
        for (int i = 0; i < _n; i++)
        {
            for (int j = 0; j < _n; j++)
            {
                if (!_shortestPathInArea.ContainsKey((i, j))) continue;
                maxCountInArea = Math.Max(_shortestPathInArea[(i, j)].Count, maxCountInArea);
            }
        }
        sb.Append($"maxCountInArea: {maxCountInArea}\n");

        return sb.ToString();
    }
}

public class Program
{
    private Stopwatch _stopwatch;
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

    public Stopwatch Stopwatch { get { return _stopwatch; } }

    public static void Main(string[] args)
    {
        new Program();
    }

    public Program()
    {
        _stopwatch = Stopwatch.StartNew();
        _u = new();
        _v = new();
        _order = new();
        _x = new();
        _y = new();

        Input();
        Solve();
    }
    private void Input(bool isOmit = true)
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

        if (!isOmit)
        {
            for (int _ = 0; _ < _n; _++)
            {
                tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
                _x.Add(tmp[0]);
                _y.Add(tmp[1]);
            }
        }
    }
    private void Solve()
    {

        int minScore = int.MaxValue;
        Field? minScoreField = null;
        while (_stopwatch.ElapsedMilliseconds <= 2500)
        {
            int score = 0;
            var field = new Field(_n, _m, _la, _lb, _u, _v);
            foreach (int destinationNode in _order)
            {
                score += field.Move(destinationNode).Score;
            }

            if (score < minScore)
            {
                // WriteLine($"{minScore} => {score}");
                minScore = score;
                minScoreField = field;
            }
        }

        minScoreField!.OutputLog();
    }
}
