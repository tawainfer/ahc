using System.Diagnostics;
using System.Text;

// using System.Text.Json;
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

// public static class DeepCopy
// {
//     public static T Clone<T>(T obj)
//     {
//         ReadOnlySpan<byte> bytes = JsonSerializer.SerializeToUtf8Bytes<T>(obj);
//         return JsonSerializer.Deserialize<T>(bytes)!;
//     }
// }

public static class SharedStopwatch
{
    private static Stopwatch _stopwatch = new Stopwatch();
    private static double _frequency = Stopwatch.Frequency;

    public static void Start() => _stopwatch.Start();
    public static void Stop() => _stopwatch.Stop();
    public static void Reset() => _stopwatch.Reset();
    public static long ElapsedMilliseconds() => _stopwatch.ElapsedMilliseconds;
    public static long ElapsedMicroseconds() => (long)(_stopwatch.ElapsedTicks / _frequency * 1_000_000);
}

// public struct Coord
// {
//     public int Y { get; set; }
//     public int X { get; set; }

//     public Coord(in int y = 0, in int x = 0)
//     {
//         Y = y;
//         X = x;
//     }
// }

public class Node
{
    // public Coord Coord { get; set; }
    public int Y { get; set; }
    public int X { get; set; }
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public bool IsFingertip { get; set; }
    public bool IsGrabbed { get; set; }

    public Node(int y, int x, int id, int? parentId, bool isFingertip, bool isGrabbed)
    {
        // Coord = coord;
        Y = y;
        X = x;
        Id = id;
        ParentId = parentId;
        IsFingertip = isFingertip;
        IsGrabbed = isGrabbed;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"id: {Id} ");
        sb.Append($"pid: {ParentId ?? -1} ");
        sb.Append($"({Y},{X}) ");
        sb.Append($"IsFingertip: {IsFingertip} ");
        sb.Append($"IsGrabbed: {IsGrabbed} ");
        return sb.ToString();
    }
}

public class RobotArm
{
    private int _n;
    private int _maxNodeCount;
    private HashSet<int>[] _graph;
    private HashSet<int>[] _subtree;
    public List<Node> Nodes { get; private set; }

    public RobotArm(int y, int x, int maxNodeCount, int n)
    {
        if (maxNodeCount < 1)
        {
            throw new Exception("maxNodeCountは1以上の値にしてください");
        }

        if (y < 0 || y >= n || x < 0 || x >= n)
        {
            throw new Exception("rootCoordの値が不正です");
        }

        _n = n;
        _maxNodeCount = maxNodeCount;
        _graph = new HashSet<int>[maxNodeCount];
        _subtree = new HashSet<int>[maxNodeCount];
        for (int i = 0; i < maxNodeCount; i++)
        {
            _graph[i] = new();
            _subtree[i] = new();
        }
        _subtree[0].Add(0);
        Nodes = new() { new Node(y, x, 0, null, true, false), };
    }

    public RobotArm DeepCopy()
    {
        RobotArm copy = new RobotArm(Nodes[0].Y, Nodes[0].X, _maxNodeCount, _n);

        foreach (var node in Nodes)
        {
            copy.Nodes.Add(new Node(node.Y, node.X, node.Id, node.ParentId, node.IsFingertip, node.IsGrabbed));
        }

        for (int i = 0; i < _graph.Length; i++)
        {
            copy._graph[i] = new HashSet<int>(_graph[i]);
        }

        for (int i = 0; i < _subtree.Length; i++)
        {
            copy._subtree[i] = new HashSet<int>(_subtree[i]);
        }

        return copy;
    }

    public void AddNode(int parentId, int length)
    {
        if (Nodes.Count >= _maxNodeCount)
        {
            throw new Exception("maxNodeCountを超えて頂点を追加することはできません");
        }

        if (parentId >= Nodes.Count)
        {
            throw new Exception($"親ノードとして指定した頂点{parentId}は存在しません");
        }

        if (length < 1 || length >= _n)
        {
            throw new Exception($"lengthの長さが不正です");
        }

        int id = Nodes.Count;
        Nodes.Add(new Node(
            Nodes[parentId].Y, Nodes[parentId].X + length, id, parentId, true, false
        ));
        Nodes[parentId].IsFingertip = false;
        _graph[id].Add(parentId);
        _graph[parentId].Add(id);

        Queue<int> q = new();
        q.Enqueue(id);
        while (q.Count >= 1)
        {
            int cp = q.Dequeue();
            _subtree[cp].Add(id);
            if (Nodes[cp].ParentId == null) break;
            q.Enqueue((int)Nodes[cp].ParentId!);
        }
    }

    public bool CanMove(int dy, int dx)
    {
        int ny = Nodes[0].Y + dy;
        int nx = Nodes[0].X + dx;
        return (0 <= ny && ny < _n && 0 <= nx && nx < _n);
    }

    public bool CanMove(char c)
    {
        switch (c)
        {
            case 'U':
                return CanMove(-1, 0);
            case 'D':
                return CanMove(1, 0);
            case 'L':
                return CanMove(0, -1);
            case 'R':
                return CanMove(0, 1);
            default:
                throw new Exception("不正な命令です");
        }
    }

    public void Move(int dy, int dx)
    {
        if (!CanMove(dy, dx))
        {
            throw new Exception("範囲外に移動しました");
        }

        foreach (var node in Nodes)
        {
            node.Y += dy;
            node.X += dx;
        }
    }

    public void Move(char c)
    {
        switch (c)
        {
            case 'U':
                Move(-1, 0);
                break;
            case 'D':
                Move(1, 0);
                break;
            case 'L':
                Move(0, -1);
                break;
            case 'R':
                Move(0, 1);
                break;
            default:
                throw new Exception("不正な命令です");
        }
    }

    private (int Ey, int Ex) RotatePoint(int py, int px, int cy, int cx, char c)
    {
        int ry = cy - py;
        int rx = cx - px;

        int dy, dx;
        switch (c)
        {
            case 'L':
                dy = -rx;
                dx = ry;
                break;
            case 'R':
                dy = rx;
                dx = -ry;
                break;
            default:
                throw new ArgumentException("不正な命令です");
        }

        int ey = py + dy;
        int ex = px + dx;
        return (ey, ex);
    }

    public void Rotate(int id, char c)
    {
        if (id < 1 || id >= Nodes.Count || Nodes[id].ParentId is null)
        {
            throw new ArgumentException($"頂点{id}に親ノードが存在しません");
        }

        if (c != 'L' && c != 'R')
        {
            throw new ArgumentException("不正な命令です");
        }

        int py = Nodes[(int)Nodes[id].ParentId!].Y;
        int px = Nodes[(int)Nodes[id].ParentId!].X;
        foreach (int i in _subtree[id])
        {
            (Nodes[i].Y, Nodes[i].X) = RotatePoint(py, px, Nodes[i].Y, Nodes[i].X, c);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        foreach (var node in Nodes)
        {
            sb.AppendLine(node.ToString());
        }
        return sb.ToString();
    }
}

// public class Field
// {
//     private int _n;
//     public bool[,] Current { get; private set; }
//     public RobotArm RobotArm { get; private set; }

//     public Field(int n, in bool[,] current, in RobotArm robotArm)
//     {
//         _n = n;
//         Current = new bool[_n, _n];
//         for (int i = 0; i < _n; i++)
//         {
//             for (int j = 0; j < _n; j++)
//             {
//                 Current[i, j] = current[i, j];
//             }
//         }
//         RobotArm = robotArm.DeepCopy();
//     }
// }

public class Program
{
    private static int _n;
    private static int _m;
    private static int _v;

    private static bool[,] _s = new bool[0, 0];
    private static bool[,] _t = new bool[0, 0];

    public static void Main(string[] args)
    {
        new Program();
    }

    public Program()
    {
        SharedStopwatch.Start();
        Input();
        Solve();
    }

    public void Input()
    {
        var buf = ReadLine()!.Split().Select(int.Parse).ToArray();
        (_n, _m, _v) = (buf[0], buf[1], buf[2]);

        _s = new bool[_n, _n];
        _t = new bool[_n, _n];

        for (int i = 0; i < _n; i++)
        {
            string s = ReadLine()!;
            for (int j = 0; j < _n; j++)
            {
                _s[i, j] = (s[j] == '1');
            }
        }

        for (int i = 0; i < _n; i++)
        {
            string t = ReadLine()!;
            for (int j = 0; j < _n; j++)
            {
                _t[i, j] = (t[j] == '1');
            }
        }
    }

    public void Solve()
    {
        // while (SharedStopwatch.ElapsedMilliseconds() <= 2900) ;
        var robotArm = new RobotArm(0, 0, _v, _n);
        robotArm.AddNode(0, 1);
        robotArm.AddNode(1, 1);
        robotArm.AddNode(1, 2);
        WriteLine("turn0");
        WriteLine(robotArm);

        robotArm.Move('R');
        robotArm.Rotate(1, 'R');
        robotArm.Rotate(2, 'L');
        WriteLine("turn1");
        WriteLine(robotArm);

        robotArm.Move('R');
        robotArm.Rotate(3, 'R');
        WriteLine("turn2");
        WriteLine(robotArm);

        robotArm.Move('D');
        robotArm.Rotate(1, 'R');
        robotArm.Rotate(2, 'R');
        WriteLine("turn3");
        WriteLine(robotArm);

        robotArm.Move('D');
        WriteLine("turn4");
        WriteLine(robotArm);
    }
}