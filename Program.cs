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
    public int Y { get; set; }
    public int X { get; set; }
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public bool IsFingertip { get; set; }
    public bool IsGrabbed { get; set; }

    public Node(int y, int x, int id, int? parentId, bool isFingertip, bool isGrabbed)
    {
        Y = y;
        X = x;
        Id = id;
        ParentId = parentId;
        IsFingertip = isFingertip;
        IsGrabbed = isGrabbed;
    }

    public Node DeepCopy()
    {
        return new Node(Y, X, Id, ParentId, IsFingertip, IsGrabbed);
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append($"id: {Id} ");
        sb.Append($"pid: {(ParentId is not null ? $"{ParentId}" : "x")} ");
        sb.Append($"({Y},{X}) ");
        sb.Append($"{(IsFingertip ? "Finger" : "")} ");
        sb.Append($"IsGrabbed: {(IsGrabbed ? "Grabbed" : "")} ");
        return sb.ToString();
    }
}

public class RobotArm
{
    private Dictionary<int, HashSet<int>> _graph;
    private Dictionary<int, HashSet<int>> _subtree;
    private List<Node> _nodes;

    public int Y { get; private set; }
    public int X { get; private set; }
    public int NodeCount { get { return _nodes.Count; } }

    public RobotArm()
    {
        _graph = new();
        _graph[0] = new();
        _subtree = new();
        _subtree[0] = new() { 0, };
        _nodes = new() { new Node(0, 0, 0, null, true, false), };
        Y = 0;
        X = 0;
    }

    public Node this[int i]
    {
        get
        {
            return new Node(
                _nodes[i].Y + Y,
                _nodes[i].X + X,
                _nodes[i].Id,
                _nodes[i].ParentId,
                _nodes[i].IsFingertip,
                _nodes[i].IsGrabbed
            );
        }
    }

    public void SetRootPosition(int y, int x)
    {
        Y = y;
        X = x;
    }

    public RobotArm DeepCopy()
    {
        var copy = new RobotArm();

        copy._graph = new Dictionary<int, HashSet<int>>();
        foreach (var kvp in _graph)
        {
            copy._graph[kvp.Key] = new HashSet<int>(kvp.Value);
        }

        copy._subtree = new Dictionary<int, HashSet<int>>();
        foreach (var kvp in _subtree)
        {
            copy._subtree[kvp.Key] = new HashSet<int>(kvp.Value);
        }

        copy._nodes = new List<Node>();
        foreach (var node in _nodes)
        {
            copy._nodes.Add(node.DeepCopy());
        }

        copy.Y = Y;
        copy.X = X;

        return copy;
    }

    public void AddNode(int parentId, int length)
    {
        if (parentId >= _nodes.Count)
        {
            throw new Exception($"親ノードとして指定した頂点{parentId}は存在しません");
        }

        int id = _nodes.Count;
        _nodes.Add(new Node(
            _nodes[parentId].Y, _nodes[parentId].X + length, id, parentId, true, false
        ));
        _nodes[parentId].IsFingertip = false;
        if (!_graph.ContainsKey(id)) _graph[id] = new();
        _graph[id].Add(parentId);
        _graph[parentId].Add(id);

        Queue<int> q = new();
        q.Enqueue(id);
        while (q.Count >= 1)
        {
            int cp = q.Dequeue();
            if (!_subtree.ContainsKey(cp)) _subtree[cp] = new();
            _subtree[cp].Add(id);
            if (_nodes[cp].ParentId == null) break;
            q.Enqueue((int)_nodes[cp].ParentId!);
        }
    }

    public int GetNodeDistance(int id1, int id2)
    {
        return Math.Abs(_nodes[id1].Y - _nodes[id2].Y) + Math.Abs(_nodes[id1].X - _nodes[id2].X);
    }

    public void Move(int dy, int dx)
    {
        Y += dy;
        X += dx;
    }

    private (int Ey, int Ex) RotatePoint(int py, int px, int cy, int cx, bool isClockwise)
    {
        int ry = cy - py;
        int rx = cx - px;

        int dy, dx;
        if (isClockwise)
        {
            dy = rx;
            dx = -ry;
        }
        else
        {
            dy = -rx;
            dx = ry;
        }

        int ey = py + dy;
        int ex = px + dx;
        return (ey, ex);
    }

    public bool CanRotate(int id, bool isClockwise)
    {
        if (id < 0 || id >= _nodes.Count)
        {
            // throw new ArgumentException($"ノード{id}は存在しません");
            return false;
        }

        if (_nodes[id].ParentId is null)
        {
            // throw new ArgumentException($"ノード{id}に親ノードが存在しません");
            return false;
        }

        return true;
    }

    public void Rotate(int id, bool isClockwise)
    {
        if (!CanRotate(id, isClockwise))
        {
            throw new ArgumentException($"回転できません");
        }

        int py = _nodes[(int)_nodes[id].ParentId!].Y;
        int px = _nodes[(int)_nodes[id].ParentId!].X;
        foreach (int i in _subtree[id])
        {
            (_nodes[i].Y, _nodes[i].X) = RotatePoint(py, px, _nodes[i].Y, _nodes[i].X, isClockwise);
        }
    }

    public bool CanGrab(int id)
    {
        if (id < 0 || id >= _nodes.Count)
        {
            // throw new Exception($"ノード{id}は存在しません");
            return false;
        }

        if (!_nodes[id].IsFingertip)
        {
            // throw new Exception($"ノード{id}は指先ではありません");
            return false;
        }

        if (_nodes[id].IsGrabbed)
        {
            // throw new Exception($"ノード{id}は既に掴んだ状態です");
            return false;
        }

        return true;
    }

    public void Grab(int id)
    {
        if (!CanGrab(id))
        {
            throw new Exception("掴めません");
        }

        _nodes[id].IsGrabbed = true;
    }

    public bool CanPut(int id)
    {
        if (id < 0 || id >= _nodes.Count)
        {
            // throw new Exception($"ノード{id}は存在しません");
            return false;
        }

        if (!_nodes[id].IsFingertip)
        {
            // throw new Exception($"ノード{id}は指先ではありません");
            return false;
        }

        if (!_nodes[id].IsGrabbed)
        {
            // throw new Exception($"ノード{id}は置くものを持っていません");
            return false;
        }

        return true;
    }

    public void Put(int id)
    {
        if (!CanPut(id))
        {
            throw new Exception($"置けません");
        }

        _nodes[id].IsGrabbed = false;
    }

    public override string ToString()
    {
        StringBuilder sb = new();
        for (int i = 0; i < NodeCount; i++)
        {
            sb.AppendLine(this[i].ToString());
        }
        return sb.ToString();
    }
}

public struct Cell
{
    public int Y { get; set; }
    public int X { get; set; }
    public bool ItemExists { get; set; }
    public bool IsDestination { get; set; }

    public Cell(int y, int x, bool itemExists, bool isDestination)
    {
        Y = y;
        X = x;
        ItemExists = itemExists;
        IsDestination = isDestination;
    }

    public override string ToString()
    {
        return $"({Y},{X}) {(ItemExists ? "Item" : "")} {(IsDestination ? "Dest" : "")}";
    }
}

public class Field
{
    private int _n;
    private HashSet<(int Y, int X)> _finished;
    private HashSet<(int Y, int X)> _unfinished;
    private bool[,] _current;
    private readonly bool[,] _goal;
    private readonly RobotArm _arm;
    private readonly string _initLog;
    private List<char[]> _log;

    public int N { get { return _n; } }
    // public IReadOnlyCollection<(int Y, int X)> Finished { get { return _finished; } }
    // public IReadOnlyCollection<(int Y, int X)> Unfinished { get { return _unfinished; } }
    public int Turn { get { return _log.Count; } }

    public Field(int n, in bool[,] s, in bool[,] t, in RobotArm arm)
    {
        _n = n;

        _finished = new();
        _unfinished = new();

        _current = new bool[_n, _n];
        for (int i = 0; i < _n; i++)
        {
            for (int j = 0; j < _n; j++)
            {
                _current[i, j] = s[i, j];
                if (t[i, j])
                {
                    if (_current[i, j])
                    {
                        _finished.Add((i, j));
                    }
                    else
                    {
                        _unfinished.Add((i, j));
                    }
                }
            }
        }

        _goal = t;

        _arm = arm.DeepCopy();

        if (_arm.Y < 0 || _arm.Y >= n || _arm.X < 0 || _arm.X >= n)
        {
            throw new Exception($"アームがフィールドの範囲外に存在します");
        }

        StringBuilder sb = new();
        sb.AppendLine($"{_arm.NodeCount}");
        for (int i = 1; i < _arm.NodeCount; i++)
        {
            var node = _arm[i];
            sb.AppendLine($"{node.ParentId} {_arm.GetNodeDistance(i, (int)node.ParentId!)}");
        }
        sb.Append($"{_arm.Y} {_arm.X}");
        _initLog = sb.ToString();

        _log = new();
    }

    private bool CanMoveArm(int dy, int dx)
    {
        int ny = _arm.Y + dy;
        int nx = _arm.X + dx;
        return (0 <= ny && ny < _n && 0 <= nx && nx < _n);
    }

    public Node this[int i] { get { return _arm[i]; } }

    public Cell this[int y, int x]
    {
        get
        {
            return new Cell(y, x, _current[y, x], _goal[y, x]);
        }
    }

    public bool CanMoveArm(char c)
    {
        switch (c)
        {
            case 'U':
                return CanMoveArm(-1, 0);
            case 'D':
                return CanMoveArm(1, 0);
            case 'L':
                return CanMoveArm(0, -1);
            case 'R':
                return CanMoveArm(0, 1);
            case '.':
                return true;
            default:
                throw new Exception($"不正な命令です({c})");
        }
    }

    private void MoveArm(int dy, int dx)
    {
        if (!CanMoveArm(dy, dx))
        {
            throw new Exception("範囲外に移動しました");
        }

        _arm.Move(dy, dx);
    }

    private void MoveArm(char c)
    {
        switch (c)
        {
            case 'U':
                MoveArm(-1, 0);
                break;
            case 'D':
                MoveArm(1, 0);
                break;
            case 'L':
                MoveArm(0, -1);
                break;
            case 'R':
                MoveArm(0, 1);
                break;
            case '.':
                // MoveArm(0, 0);
                break;
            default:
                throw new Exception($"不正な命令です({c})");
        }
    }

    public bool CanRotateArm(int id, bool isClockwise)
    {
        return _arm.CanRotate(id, isClockwise);
    }

    private void RotateArm(int id, bool isClockwise)
    {
        if (!CanRotateArm(id, isClockwise))
        {
            throw new Exception($"ノード{id}の部分木を回転できません");
        }

        _arm.Rotate(id, isClockwise);
    }

    public bool CanGrabArm(int id)
    {
        var node = _arm[id];
        (int y, int x) = (node.Y, node.X);

        if (y < 0 || y >= _n || x < 0 || x >= _n)
        {
            // throw new Exception($"ノード{id}はフィールドの範囲外に存在します");
            return false;
        }

        if (!_current[y, x])
        {
            // throw new Exception($"マス({y},{x})に掴めるものがありません");
            return false;
        }

        return _arm.CanGrab(id);
    }

    private void GrabArm(int id)
    {
        if (!CanGrabArm(id))
        {
            throw new Exception($"ノード{id}({_arm[id].Y},{_arm[id].X})でアイテムを掴めません");
        }

        var node = _arm[id];
        (int y, int x) = (node.Y, node.X);

        _arm.Grab(id);
        _current[y, x] = false;
        if (_finished.Contains((y, x)))
        {
            _finished.Remove((y, x));
            _unfinished.Add((y, x));
        }
    }

    public bool CanPutArm(int id)
    {
        var node = _arm[id];
        (int y, int x) = (node.Y, node.X);

        if (y < 0 || y >= _n || x < 0 || x >= _n)
        {
            // throw new Exception($"ノード{id}はフィールドの範囲外に存在します");
            return false;
        }

        if (_current[y, x])
        {
            // throw new Exception($"マス({y},{x})には既にアイテムが存在します");
            return false;
        }

        return _arm.CanPut(id);
    }

    private void PutArm(int id)
    {
        if (!CanPutArm(id))
        {
            throw new Exception($"ノード{id}はアイテムを置けません");
        }

        var node = _arm[id];
        (int y, int x) = (node.Y, node.X);

        _arm.Put(id);
        _current[y, x] = true;
        if (_unfinished.Contains((y, x)))
        {
            _unfinished.Remove((y, x));
            _finished.Add((y, x));
        }
    }

    public bool IsDone()
    {
        return _unfinished.Count == 0;
    }

    public void Operate(char[] operation)
    {
        MoveArm(operation[0]);

        for (int i = 1; i < _arm.NodeCount; i++)
        {
            if (operation[i] != 'L' && operation[i] != 'R') continue;
            RotateArm(i, (operation[i] == 'R' ? true : false));
        }

        for (int i = 0; i < _arm.NodeCount; i++)
        {
            if (operation[_arm.NodeCount + i] == 'P')
            {
                if (!_arm[i].IsGrabbed)
                {
                    GrabArm(i);
                }
                else
                {
                    PutArm(i);
                }
            }
        }

        _log.Add(operation);
    }

    public void Operate(string operation)
    {
        Operate(operation.ToArray());
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Turn: {Turn}");

        sb.AppendLine("Current Field State (_current):");
        for (int y = 0; y < _n; y++)
        {
            for (int x = 0; x < _n; x++)
            {
                sb.Append(_current[y, x] ? "1 " : "0 ");
            }
            sb.AppendLine();
        }

        sb.AppendLine("Finished Cells (_finished):");
        foreach (var (Y, X) in _finished)
        {
            sb.AppendLine($"({Y}, {X})");
        }

        sb.AppendLine("Unfinished Cells (_unfinished):");
        foreach (var (Y, X) in _unfinished)
        {
            sb.AppendLine($"({Y}, {X})");
        }

        sb.AppendLine($"Robot Arm Position: ({_arm.Y}, {_arm.X})");

        sb.AppendLine($"Robot Arm:");
        for (int i = 0; i < _arm.NodeCount; i++)
        {
            var node = _arm[i];
            sb.Append($"id: {node.Id} ");
            sb.Append($"pid: {node.ParentId ?? -1} ");
            sb.Append($"({node.Y},{node.X}) ");
            sb.Append($"IsFingertip: {node.IsFingertip} ");
            sb.AppendLine($"IsGrabbed: {node.IsGrabbed}");
        }

        sb.AppendLine("Operation Log (_log):");
        foreach (var operations in _log)
        {
            sb.AppendLine(new string(operations));
        }

        return sb.ToString();
    }

    public void PrintLog()
    {
        WriteLine(_initLog);
        foreach (var l in _log)
        {
            foreach (char c in l)
            {
                Write(c);
            }
            WriteLine();
        }
    }
}

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

        // Sample();
        Greedy();
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

    public void Sample()
    {
        var arm = new RobotArm();
        arm.SetRootPosition(0, 0);
        arm.AddNode(0, 1);
        arm.AddNode(1, 1);
        arm.AddNode(1, 2);

        var field = new Field(_n, _s, _t, arm);
        // WriteLine(field);

        // field.Operate("RRL...PP".ToArray());
        // WriteLine(field);

        // field.Operate("R..R..P.".ToArray());
        // WriteLine(field);

        // field.Operate("DRR...P.".ToArray());
        // WriteLine(field);

        // field.Operate("D.....PP".ToArray());
        // WriteLine(field);

        field.PrintLog();
    }

    public void Greedy()
    {
        int[] dy = new int[] { -1, 0, 1, 0 };
        int[] dx = new int[] { 0, 1, 0, -1 };

        var arm = new RobotArm();
        arm.SetRootPosition(0, 0);

        var field = new Field(_n, _s, _t, arm);

        while (!field.IsDone())
        {
            var rootNode = field[0];

            HashSet<(int Y, int X)> seen = new() { (rootNode.Y, rootNode.X), };
            Queue<(int Y, int X)> q = new();
            q.Enqueue((rootNode.Y, rootNode.X));

            (int Y, int X) targetNode = (int.MinValue, int.MinValue);
            while (q.Count >= 1)
            {
                (int cy, int cx) = q.Dequeue();
                var cell = field[cy, cx];
                if (rootNode.IsGrabbed && !cell.ItemExists && cell.IsDestination
                    || !rootNode.IsGrabbed && cell.ItemExists && !cell.IsDestination)
                {
                    targetNode = (cy, cx);
                    break;
                }

                for (int i = 0; i < 4; i++)
                {
                    int ey = cy + dy[i];
                    int ex = cx + dx[i];
                    if (ey < 0 || ey >= field.N || ex < 0 || ex >= field.N) continue;
                    if (seen.Contains((ey, ex))) continue;
                    seen.Add((ey, ex));
                    q.Enqueue((ey, ex));
                }
            }

            int distanceY = Math.Abs(rootNode.Y - targetNode.Y);
            char moveDirectionY = (rootNode.Y > targetNode.Y ? 'U' : 'D');
            for (int i = 0; i < distanceY; i++)
            {
                field.Operate(new char[] { moveDirectionY, '.', });
            }

            int distanceX = Math.Abs(rootNode.X - targetNode.X);
            char moveDirectionX = (rootNode.X > targetNode.X ? 'L' : 'R');
            for (int i = 0; i < distanceX; i++)
            {
                field.Operate(new char[] { moveDirectionX, '.', });
            }

            field.Operate(new char[] { '.', 'P', });
        }

        field.PrintLog();
    }
}