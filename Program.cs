using System.Diagnostics;
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

public class Program
{
    private int _n;
    private List<int> _x = new();
    private List<int> _y = new();

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
        for (int _ = 0; _ < _n; _++)
        {
            int[] buf = ReadLine()!.Split().Select(int.Parse).ToArray();
            _x.Add(buf[0]);
            _y.Add(buf[1]);
        }
    }

    public void Solve()
    {
        WriteLine(4);
        WriteLine($"{0} {0}");
        WriteLine($"{0} {100000}");
        WriteLine($"{100000} {100000}");
        WriteLine($"{100000} {0}");
    }
}