using System.Diagnostics;
using Newtonsoft.Json;
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
    private int _cp = 0;
    private int _epIdx = 0;

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
        Solve();
    }

    public void Solve()
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

        _a = new();
        for (int i = 0; i < _la; i++) _a.Add(i % _n);

        _b = new();
        for (int i = 0; i < _lb; i++) _b.Add(-1);

        WriteLine(string.Join(' ', _a));

        foreach (int ep in _order)
        {
            List<bool> seen = new();
            for (int _ = 0; _ < _n; _++) seen.Add(false);
            seen[_cp] = true;
            Queue<(int, List<int>)> q = new();
            q.Enqueue((_cp, new() { _cp, }));

            List<int> confirmedRoot = new();
            while (q.Count >= 1)
            {
                (int u, List<int> root) = q.Dequeue();
                if (u == ep)
                {
                    confirmedRoot = root;
                    break;
                }

                foreach (int v in _g[u])
                {
                    if (seen[v]) continue;
                    seen[v] = true;
                    List<int> newRoot = new();
                    newRoot.AddRange(root);
                    newRoot.Add(v);
                    q.Enqueue((v, newRoot));
                }
            }

            for (int i = 1; i < confirmedRoot.Count; i++)
            {
                WriteLine($"s {1} {_a.IndexOf(confirmedRoot[i])} {0}");
                WriteLine($"m {confirmedRoot[i]}");
                _cp = confirmedRoot[i];
            }
        }
    }
}
