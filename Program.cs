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
    private Dictionary<int, HashSet<int>> _graph;
    private Dictionary<int, HashSet<int>> _subtree;
    public List<Node> Nodes { get; private set; }

    public RobotArm()
    {
        _graph = new();
        _graph[0] = new();
        _subtree = new();
        _subtree[0] = new() { 0, };
        Nodes = new() { new Node(0, 0, 0, null, true, false), };
    }

    public RobotArm DeepCopy()
    {
        RobotArm copy = new RobotArm();

        copy.Nodes = new();
        foreach (var node in Nodes)
        {
            copy.Nodes.Add(new Node(node.Y, node.X, node.Id, node.ParentId, node.IsFingertip, node.IsGrabbed));
        }

        copy._graph = new();
        foreach (var kvp in _graph)
        {
            copy._graph[kvp.Key] = new HashSet<int>(kvp.Value);
        }

        copy._subtree = new();
        foreach (var kvp in _subtree)
        {
            copy._subtree[kvp.Key] = new HashSet<int>(kvp.Value);
        }

        return copy;
    }

    public void AddNode(int parentId, int length)
    {
        if (parentId >= Nodes.Count)
        {
            throw new Exception($"親ノードとして指定した頂点{parentId}は存在しません");
        }

        int id = Nodes.Count;
        Nodes.Add(new Node(
            Nodes[parentId].Y, Nodes[parentId].X + length, id, parentId, true, false
        ));
        Nodes[parentId].IsFingertip = false;
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
            if (Nodes[cp].ParentId == null) break;
            q.Enqueue((int)Nodes[cp].ParentId!);
        }
    }

    public int GetNodeDistance(int id1, int id2)
    {
        return Math.Abs(Nodes[id1].Y - Nodes[id2].Y) + Math.Abs(Nodes[id1].X - Nodes[id2].X);
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
        if (id < 0 || id >= Nodes.Count)
        {
            // throw new ArgumentException($"ノード{id}は存在しません");
            return false;
        }

        if (Nodes[id].ParentId is null)
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

        int py = Nodes[(int)Nodes[id].ParentId!].Y;
        int px = Nodes[(int)Nodes[id].ParentId!].X;
        foreach (int i in _subtree[id])
        {
            (Nodes[i].Y, Nodes[i].X) = RotatePoint(py, px, Nodes[i].Y, Nodes[i].X, isClockwise);
        }
    }

    public bool CanGrab(int id)
    {
        if (id < 0 || id >= Nodes.Count)
        {
            // throw new Exception($"ノード{id}は存在しません");
            return false;
        }

        if (!Nodes[id].IsFingertip)
        {
            // throw new Exception($"ノード{id}は指先ではありません");
            return false;
        }

        if (Nodes[id].IsGrabbed)
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

        Nodes[id].IsGrabbed = true;
    }

    public bool CanPut(int id)
    {
        if (id < 0 || id >= Nodes.Count)
        {
            // throw new Exception($"ノード{id}は存在しません");
            return false;
        }

        if (!Nodes[id].IsFingertip)
        {
            // throw new Exception($"ノード{id}は指先ではありません");
            return false;
        }

        if (!Nodes[id].IsGrabbed)
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

        Nodes[id].IsGrabbed = false;
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

public class Field
{
    private int _n;
    private bool[,] _current;
    private HashSet<(int Y, int X)> _finished;
    private HashSet<(int Y, int X)> _unfinished;
    private readonly RobotArm _arm;
    private int _armY;
    private int _armX;
    private readonly string _initLog;
    private List<char[]> _log;

    public int Turn { get { return _log.Count; } }

    public Field(int n, in bool[,] s, in bool[,] t, in RobotArm arm, int armY, int armX)
    {
        _n = n;

        _current = new bool[_n, _n];
        _finished = new();
        _unfinished = new();

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

        _arm = arm.DeepCopy();

        if (armY < 0 || armY >= n || armX < 0 || armX >= n)
        {
            throw new Exception($"アームをフィールドの範囲外に配置しようとしました");
        }
        _armY = armY;
        _armX = armX;

        StringBuilder sb = new();
        sb.AppendLine($"{_arm.Nodes.Count}");
        for (int i = 1; i < _arm.Nodes.Count; i++)
        {
            sb.AppendLine($"{_arm.Nodes[i].ParentId} {_arm.GetNodeDistance(i, (int)_arm.Nodes[i].ParentId!)}");
        }
        sb.Append($"{_armY} {_armX}");
        _initLog = sb.ToString();

        _log = new();
    }

    public (int Y, int X) GetArmNodePosition(int id)
    {
        return (_armY + _arm.Nodes[id].Y, _armX + _arm.Nodes[id].X);
    }

    private bool CanMoveArm(int dy, int dx)
    {
        int ny = _armY + dy;
        int nx = _armX + dx;
        return (0 <= ny && ny < _n && 0 <= nx && nx < _n);
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
            default:
                throw new Exception("不正な命令です");
        }
    }

    private void MoveArm(int dy, int dx)
    {
        if (!CanMoveArm(dy, dx))
        {
            throw new Exception("範囲外に移動しました");
        }

        _armY += dy;
        _armX += dx;
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
            default:
                throw new Exception("不正な命令です");
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
        (int y, int x) = GetArmNodePosition(id);

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
            throw new Exception($"ノード{id}でアイテムを掴めません");
        }

        (int y, int x) = GetArmNodePosition(id);
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
        (int y, int x) = GetArmNodePosition(id);

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

        (int y, int x) = GetArmNodePosition(id);
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

        for (int i = 1; i < _arm.Nodes.Count; i++)
        {
            if (operation[i] != 'L' && operation[i] != 'R') continue;
            RotateArm(i, (operation[i] == 'R' ? true : false));
        }

        for (int i = 0; i < _arm.Nodes.Count; i++)
        {
            if (operation[_arm.Nodes.Count + i] == 'P')
            {
                if (!_arm.Nodes[i].IsGrabbed)
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

        sb.AppendLine($"Robot Arm Position: ({_armY}, {_armX})");

        sb.AppendLine($"Robot Arm:");
        foreach (var node in _arm.Nodes)
        {
            sb.Append($"id: {node.Id} ");
            sb.Append($"pid: {node.ParentId ?? -1} ");
            sb.Append($"{GetArmNodePosition(node.Id)} ");
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
        Sample();
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
        arm.AddNode(0, 1);
        arm.AddNode(1, 1);
        arm.AddNode(1, 2);

        var field = new Field(_n, _s, _t, arm, 0, 0);
        // field.Operate("RRL...PP".ToArray());
        // field.Operate("R..R..P.".ToArray());
        // field.Operate("DRR...P.".ToArray());
        // field.Operate("D.....PP".ToArray());

        field.PrintLog();
    }
}