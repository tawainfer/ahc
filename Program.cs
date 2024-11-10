using AtCoder;
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

public class Coord
{
    public int X { get; set; }
    public int Y { get; set; }

    public Coord(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"({X}, {Y})";
}

// 四角形のエリアを表す
public class Square
{
    public Coord[] Points { get; private set; }

    public Square(Coord p1, Coord p2, Coord p3, Coord p4)
    {
        Points = new Coord[] { p1, p2, p3, p4 };

        // Points[0]を左上として時計回りになるように4点を並び替える
        SortPointsClockwise();
    }

    // 4点を左上から時計回りに並び替える
    private void SortPointsClockwise()
    {
        // 重心を計算
        double centerX = (Points[0].X + Points[1].X + Points[2].X + Points[3].X) / 4.0;
        double centerY = (Points[0].Y + Points[1].Y + Points[2].Y + Points[3].Y) / 4.0;

        // 中心からの角度を元に4点をソートする
        Array.Sort(Points, (a, b) =>
        {
            double angleA = Math.Atan2(a.Y - centerY, a.X - centerX);
            double angleB = Math.Atan2(b.Y - centerY, b.X - centerX);
            return angleA.CompareTo(angleB);
        });

        // Points[0]が左上になるまで全体を回転させる
        while (Points[0].X > Points[1].X || Points[0].Y > Points[3].Y)
        {
            RotatePoints();
        }
    }

    // 全体を左回転させる
    private void RotatePoints()
    {
        Points = new Coord[] { Points[1], Points[2], Points[3], Points[0] };
    }

    public override string ToString()
    {
        // return $"Square [P1: {Points[0]}, P2: {Points[1]}, P3: {Points[2]}, P4: {Points[3]}]";
        StringBuilder sb = new();
        sb.AppendLine("4");
        for (int i = 0; i < 4; i++)
        {
            sb.Append($"{Points[i].X} {Points[i].Y}");
            if (i < 3) sb.Append("\n");
        }
        return sb.ToString();
    }
}

// public class Area : Square
// {
//     // 方向d(0<=d<4)ごとに橋の接続点を管理する
//     // 他のエリアと繋がっていない場合はNULL
//     // 繋がっている場合は点dと点(d+1)%4の間にある接続点の情報を入れる
//     public (Coord P1, Coord P2)?[] Ports { get; private set; }

//     public Area(Coord p1, Coord p2, Coord p3, Coord p4) : base(p1, p2, p3, p4)
//     {
//         Ports = new (Coord P1, Coord P2)?[4];
//         for (int i = 0; i < 4; i++)
//         {
//             Ports[i] = null;
//         }
//     }
// }

// 網を表す多角形の情報を管理するクラス
public class Net
{
    private static int[] _dx = new int[] { 0, 1, 0, -1 };
    private static int[] _dy = new int[] { -1, 0, 1, 0 };

    // メインで魚を捕まえる四角形のエリア
    public List<Square> Areas { get; private set; }

    // エリア同士を繋ぐ目的で作られるサブエリア
    public List<Square> Bridges { get; private set; }

    // 方向d(0<=d<4)ごとに橋の接続点を管理する
    // 他のエリアと繋がっていない場合はNULL
    // 繋がっている場合は点dと点(d+1)%4の間にある接続点の情報を入れる
    public List<(Coord P1, Coord P2)?[]> Ports { get; private set; }

    // インスタンス生成時に最初のエリアを渡す
    public Net(Square firstArea)
    {
        Areas = new() { firstArea, };
        Bridges = new();
        Ports = new() { new (Coord P1, Coord P2)?[] { null, null, null, null, } };
    }

    // 新しいエリアを追加する
    // 接続元のエリアの番号、接続元・接続先のエリアで橋を伸ばす方向を指定する
    public void AddArea(Square newArea, int baseAreaId, int baseAreaDir, int newAreaDir)
    {
        // 接続方向が不適な場合を弾く
        if ((baseAreaDir + 2) % 4 != newAreaDir)
        {
            throw new Exception("エリアの接続方向が不適です");
        }

        // 接続元のエリアの方向で別のエリアがすでに接続されているなら弾く
        if (Ports[baseAreaId][baseAreaDir] is not null)
        {
            throw new Exception("接続元のエリアの方向に別のエリアが既に接続されています");
        }

        // エリア配列に新しいエリアを追加
        Areas.Add(newArea);

        // 橋をかける
        int baseAreaNextDir = (baseAreaDir + 1) % 4;
        int newAreaNextDir = (newAreaDir + 1) % 4;
        Coord p1 = new(Areas[baseAreaId].Points[baseAreaDir].X + _dx[baseAreaNextDir],
            Areas[baseAreaId].Points[baseAreaDir].Y + _dy[baseAreaNextDir]);
        Coord p2 = new(Areas[baseAreaId].Points[baseAreaDir].X + _dx[baseAreaNextDir] * 2,
            Areas[baseAreaId].Points[baseAreaDir].Y + _dy[baseAreaNextDir] * 2);
        Coord p3 = new(Areas[^1].Points[newAreaDir].X + _dx[newAreaNextDir],
            Areas[^1].Points[newAreaDir].Y + _dy[newAreaNextDir]);
        Coord p4 = new(Areas[^1].Points[newAreaDir].X + _dx[newAreaNextDir] * 2,
            Areas[^1].Points[newAreaDir].Y + _dy[newAreaNextDir] * 2);
        Bridges.Add(new Square(p1, p2, p3, p4));

        // Portsを更新して接続点の情報を最新にする
        Ports.Add(new (Coord P1, Coord P2)?[] { null, null, null, null, });
        Ports[baseAreaId][baseAreaDir] = baseAreaDir switch
        {
            0 => new(Bridges.Last().Points[3], Bridges.Last().Points[2]),
            1 => new(Bridges.Last().Points[0], Bridges.Last().Points[3]),
            2 => new(Bridges.Last().Points[1], Bridges.Last().Points[0]),
            _ => new(Bridges.Last().Points[2], Bridges.Last().Points[1]),
        };
        Ports[^1][newAreaDir] = newAreaDir switch
        {
            0 => new(Bridges.Last().Points[3], Bridges.Last().Points[2]),
            1 => new(Bridges.Last().Points[0], Bridges.Last().Points[3]),
            2 => new(Bridges.Last().Points[1], Bridges.Last().Points[0]),
            _ => new(Bridges.Last().Points[2], Bridges.Last().Points[1]),
        };
        // Ports[baseAreaId][baseAreaDir] = baseAreaDir switch
        // {
        //     0 => new(Bridges.Last().Points[2], Bridges.Last().Points[3]),
        //     1 => new(Bridges.Last().Points[3], Bridges.Last().Points[0]),
        //     2 => new(Bridges.Last().Points[0], Bridges.Last().Points[1]),
        //     _ => new(Bridges.Last().Points[1], Bridges.Last().Points[2]),
        // };

        // Ports[^1][newAreaDir] = newAreaDir switch
        // {
        //     0 => new(Bridges.Last().Points[2], Bridges.Last().Points[3]),
        //     1 => new(Bridges.Last().Points[3], Bridges.Last().Points[0]),
        //     2 => new(Bridges.Last().Points[0], Bridges.Last().Points[1]),
        //     _ => new(Bridges.Last().Points[1], Bridges.Last().Points[2]),
        // };
    }

    public override string ToString()
    {
        List<Coord> ans = new();
        // for(int i = 1; i < n; i++) {

        // }

        void Dfs(int i)
        {
            if (Ports[i][0] is not null)
            {
                ans.Add(Ports[i][0]?.P1!);
            }

            ans.Add(Areas[i].Points[1]);
            ans.Add(Areas[i].Points[2]);

            if (Ports[i][2] is not null)
            {
                ans.Add(Ports[i][2]?.P2!);
            }

            if (i < Areas.Count - 1)
            {
                Dfs(i + 1);
            }

            if (Ports[i][2] is not null)
            {
                ans.Add(Ports[i][2]?.P1!);
            }

            ans.Add(Areas[i].Points[3]);
            ans.Add(Areas[i].Points[0]);

            if (Ports[i][0] is not null)
            {
                ans.Add(Ports[i][0]?.P2!);
            }
        }
        Dfs(0);

        StringBuilder sb = new();
        // WriteLine(ans.Count);
        sb.AppendLine($"{ans.Count}");
        foreach (Coord c in ans)
        {
            // WriteLine($"{c.Y} {c.X}");
            sb.AppendLine($"{c.Y} {c.X}");
        }
        return sb.ToString();
    }
}

// 区間和の演算を定義した構造体
struct Op : ISegtreeOperator<long>
{
    public long Identity => 0;

    public long Operate(long x, long y) => x + y;
}

public class Program
{
    private int _n;
    private List<int> _x = new();
    private List<int> _y = new();
    private int _mx = 100000;
    private int _my = 100000;

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
        _n = int.Parse(ReadLine()!);
        _x = new();
        _y = new();
        for (int _ = 0; _ < 2 * _n; _++)
        {
            int[] buf = ReadLine()!.Split().Select(int.Parse).ToArray();
            _x.Add(buf[0]);
            _y.Add(buf[1]);
        }
    }

    // x軸とy軸が入れ替わるバグを作り込んで未完成...
    // ビジュアライザが妙な多角形を表示してくる
    public void Solve()
    {
        // フィールドを縦に100等分して行ごとにセグメントツリーで魚の数を管理する
        var seg = new Segtree<long, Op>(100009);
        Segtree<long, Op>[] mackerelSeg = new Segtree<long, Op>[100];
        Segtree<long, Op>[] sardineSeg = new Segtree<long, Op>[100];
        for (int i = 0; i < 100; i++)
        {
            mackerelSeg[i] = new Segtree<long, Op>(100009);
            sardineSeg[i] = new Segtree<long, Op>(100009);
        }
        for (int i = 0; i < _n; i++)
        {
            mackerelSeg[_y[i] / 1000][_x[i]]++;
            sardineSeg[_y[i + _n] / 1000][_x[i + _n]]++;
        }

        var net = new Net(new(new(0, 0), new(100000, 0), new(100000, 499), new(0, 499)));
        for (int i = 1; i < 100; i++)
        {
            net.AddArea(new(new(0, i * 1000), new(100000, i * 1000), new(100000, 499 + i * 1000), new(0, 499 + i * 1000)), i - 1, 2, 0);
        }

        WriteLine(net.ToString());
    }
}