using System.Diagnostics;
using Newtonsoft.Json;
using AtCoder;
using static System.Console;
using System.Threading.Tasks.Dataflow;

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

public class Program
{
    private Stopwatch _stopwatch;
    private TimeSpan _timeout;
    private int _n;
    private int _m;
    private int _t;
    private int _la;
    private int _lb;
    private List<int> _u;
    private List<int> _v;
    private List<List<int>> _g;
    private List<int> _order;
    private List<int> _x;
    private List<int> _y;
    private List<int> _a;
    private List<int> _b;
    private List<List<int>> _spanningTree;
    private int _centralNode;
    private Dictionary<(int From, int To), List<int>> _spanningTreeRoot;
    private List<string> _ans;

    public Stopwatch Stopwatch { get { return _stopwatch; } }
    public TimeSpan Timeout { get { return _timeout; } }

    public static void Main(string[] args)
    {
        new Program();
    }

    public Program()
    {
        _stopwatch = Stopwatch.StartNew();
        _timeout = TimeSpan.FromMilliseconds(2900);
        _u = new();
        _v = new();
        _order = new();
        _x = new();
        _y = new();
        _a = new();
        _b = new();
        _g = new();
        _spanningTree = new();
        _spanningTreeRoot = new();
        _ans = new();

        Init();
        MakeSpanningTree();
        SetCentralNode();
        SetSpanningTreeRoot();
        SetControlList();
        Solve();
    }

    public void Init()
    {
        var tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
        (_n, _m, _t, _la, _lb) = (tmp[0], tmp[1], tmp[2], tmp[3], tmp[4]);

        _g = new();
        for (int _ = 0; _ < _n; _++) _g.Add(new());

        for (int _ = 0; _ < _m; _++)
        {
            tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
            _u.Add(tmp[0]);
            _v.Add(tmp[1]);
            _g[tmp[0]].Add(tmp[1]);
            _g[tmp[1]].Add(tmp[0]);
        }
        for (int i = 0; i < _n; i++) _g[i].Sort();

        _order = ReadLine()!.Split().Select(int.Parse).ToList();

        for (int _ = 0; _ < _n; _++)
        {
            tmp = ReadLine()!.Split().Select(int.Parse).ToArray();
            _x.Add(tmp[0]);
            _y.Add(tmp[1]);
        }

        _b = new();
        for (int i = 0; i < _lb; i++) _b.Add(-1);
    }

    public void MakeSpanningTree()
    {
        _spanningTree = new();
        for (int _ = 0; _ < _n; _++) _spanningTree.Add(new());

        var dsu = new Dsu(_n);
        for (int i = 0; i < _m; i++)
        {
            if (!dsu.Same(_u[i], _v[i]))
            {
                dsu.Merge(_u[i], _v[i]);
                _spanningTree[_u[i]].Add(_v[i]);
                _spanningTree[_v[i]].Add(_u[i]);
            }
        }
    }

    public void SetCentralNode()
    {
        List<(int Id, int Count)> GetSubtree(int parent)
        {
            List<int> subtreeSize = new();
            for (int _ = 0; _ < _n; _++) subtreeSize.Add(1);

            void Dfs(int node, int parent)
            {
                foreach (int neighbor in _spanningTree[node])
                {
                    if (neighbor == parent) continue;
                    Dfs(neighbor, node);
                    subtreeSize[node] += subtreeSize[neighbor];
                }
            }
            Dfs(parent, -1);

            List<(int Id, int Count)> res = new();
            foreach (int neighbor in _spanningTree[parent])
            {
                res.Add((neighbor, subtreeSize[neighbor]));
            }
            return res;
        }

        (int Id, int Count) centralNode = (-1, int.MaxValue);
        for (int i = 0; i < _n; i++)
        {
            var subtree = GetSubtree(i);
            int maxCount = int.MinValue;
            foreach ((int id, int count) in subtree)
            {
                maxCount = Math.Max(maxCount, count);
            }

            if (maxCount < centralNode.Count)
            {
                centralNode = (i, maxCount);
            }
        }

        _centralNode = centralNode.Id;
    }

    public void SetSpanningTreeRoot()
    {
        _spanningTreeRoot = new();
        for (int i = 0; i < _n; i++)
        {
            _spanningTreeRoot[(i, i)] = new() { i, };
        }

        void Dfs(int node, int parent, List<int> root)
        {
            foreach (int neighbor in _spanningTree[node])
            {
                if (neighbor == parent) continue;

                var newRoot = DeepCopy.Clone(root);
                newRoot.Add(neighbor);
                _spanningTreeRoot[(_centralNode, neighbor)] = DeepCopy.Clone(newRoot);

                var reverseRoot = DeepCopy.Clone(newRoot);
                reverseRoot.Reverse();
                _spanningTreeRoot[(neighbor, _centralNode)] = DeepCopy.Clone(reverseRoot);

                Dfs(neighbor, node, DeepCopy.Clone(newRoot));
            }
        }
        Dfs(_centralNode, -1, new() { _centralNode, });
    }

    public void SetControlList()
    {
        _a = new();

        void Dfs(int node, int parent)
        {
            _a.Add(node);
            foreach (int neighbor in _spanningTree[node])
            {
                if (neighbor == parent) continue;
                Dfs(neighbor, node);
            }
        }
        Dfs(_centralNode, -1);

        while (_a.Count < _la)
        {
            _a.Add(_centralNode);
        }
    }

    public List<int> GetShortestRoot(int start, int goal)
    {
        List<int> shortestRoot = new() { start, };

        List<bool> seen = new();
        for (int _ = 0; _ < _n; _++) seen.Add(false);
        seen[start] = true;

        Queue<(int, List<int>)> q = new();
        q.Enqueue((start, new() { start, }));

        while (q.Count >= 1)
        {
            (int currentNode, List<int> root) = q.Dequeue();
            if (currentNode == goal)
            {
                shortestRoot = root;
                break;
            }

            foreach (int v in _g[currentNode])
            {
                if (seen[v]) continue;
                seen[v] = true;
                var newRoot = DeepCopy.Clone(root);
                newRoot.Add(v);
                q.Enqueue((v, newRoot));
            }
        }

        return shortestRoot;
    }

    public (int SignCount, List<string> Log) MoveSimulation(
        List<int> root,
        in List<int> before_b,
        out List<int> after_b,
        bool isReverseControl = false
    )
    {
        List<string> log = new();
        after_b = DeepCopy.Clone(before_b);

        for (int i = 1; i < root.Count; i++)
        {
            int nextNode = root[i];

            if (!after_b.Contains(nextNode))
            {
                int sa = _a.IndexOf(nextNode);
                int ea = Math.Min(sa + _lb - 1, _la - 1);
                int l = ea - sa + 1;

                if (isReverseControl)
                {
                    ea = _a.IndexOf(nextNode);
                    sa = Math.Max(ea - _lb + 1, 0);
                    l = ea - sa + 1;
                }

                log.Add($"s {l} {sa} {0}");
                for (int j = 0; j < l; j++)
                {
                    after_b[j] = _a[sa + j];
                }
            }

            log.Add($"m {nextNode}");
        }

        int signCount = 0;
        foreach (string s in log)
        {
            if (s.StartsWith('s')) signCount++;
        }

        return (signCount, log);
    }


    public void Solve()
    {
        _ans.Add(string.Join(' ', _a));

        int currentNode = 0;
        int detourCount = 0;
        int shortcutCount = 0;

        foreach (int nextNode in _order)
        {
            List<int> detourB;
            (int detourSignCount1, List<string> detourLog1) = MoveSimulation(
                _spanningTreeRoot[(currentNode, _centralNode)], _b, out detourB, true
            );
            (int detourSignCount2, List<string> detourLog2) = MoveSimulation(
                _spanningTreeRoot[(_centralNode, nextNode)], detourB, out detourB
            );

            int detourSignCount = detourSignCount1 + detourSignCount2;
            List<string> detourLog = new();
            detourLog.AddRange(detourLog1);
            detourLog.AddRange(detourLog2);

            List<int> shortcutB;
            (int shortcutSignCount, List<string> shortcutLog) = MoveSimulation(
                GetShortestRoot(currentNode, nextNode), _b, out shortcutB
            );

            if (detourSignCount <= shortcutSignCount)
            {
                detourCount++;
                _ans.AddRange(detourLog);
                _b = detourB;
            }
            else
            {
                shortcutCount++;
                _ans.AddRange(shortcutLog);
                _b = shortcutB;
            }

            currentNode = nextNode;
        }

        foreach (string s in _ans)
        {
            WriteLine(s);
        }
    }
}
