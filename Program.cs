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

public struct Drink
{
    public int Sweetness { get; set; }
    public int Fizzy { get; set; }

    public Drink(int sweetness, int fizzy)
    {
        Sweetness = sweetness;
        Fizzy = fizzy;
    }

    public override string ToString()
    {
        return $"({Sweetness}, {Fizzy})";
    }
}

public class Factory
{
    private List<Drink> _finishedList;
    private HashSet<Drink> _finishedSet;
    private List<Drink> _unfinishedList;
    private HashSet<Drink> _unfinishedSet;
    private long _totalCost;
    private List<(Drink baseDrink, Drink newDrink)> _logs;

    public int UnfinishedCount { get { return _unfinishedSet.Count; } }

    public Factory(in int n, in List<int> a, in List<int> b)
    {
        _finishedSet = new() { new Drink(0, 0), };
        _unfinishedSet = new();
        _totalCost = 0;
        _logs = new();

        for (int i = 0; i < n; i++)
        {
            _unfinishedSet.Add(new Drink(a[i], b[i]));
        }

        if (_unfinishedSet.Contains(new Drink(0, 0)))
        {
            _unfinishedSet.Remove(new Drink(0, 0));
        }

        _finishedList = new(_finishedSet);
        _unfinishedList = new(_unfinishedSet);
    }

    private int CalcCost(Drink baseDrink, Drink newDrink)
    {
        return newDrink.Sweetness - baseDrink.Sweetness
            + newDrink.Fizzy - baseDrink.Fizzy;
    }

    private void MakeNewDrink(Drink baseDrink, Drink newDrink)
    {
        if (!_finishedSet.Contains(baseDrink))
        {
            throw new Exception("存在しない飲料を指定しました");
        }

        if (baseDrink.Sweetness > newDrink.Sweetness || baseDrink.Fizzy > newDrink.Fizzy)
        {
            throw new Exception("元の飲料より薄い飲料は作れません");
        }

        _totalCost += CalcCost(baseDrink, newDrink);
        _finishedList.Add(newDrink);
        _finishedSet.Add(newDrink);
        _unfinishedSet.Remove(newDrink);
        _unfinishedList.RemoveAt(_unfinishedList.Count - 1);
        _logs.Add((baseDrink, newDrink));
    }

    public bool IsDone()
    {
        return UnfinishedCount == 0;
    }

    public bool SimpleAction()
    {
        if (IsDone()) return false;

        Drink baseDrink = new(0, 0);
        Drink newDrink = _unfinishedList.Last();
        MakeNewDrink(baseDrink, newDrink);

        return true;
    }

    public void Print()
    {
        WriteLine(_logs.Count);
        foreach ((Drink baseDrink, Drink newDrink) in _logs)
        {
            WriteLine(string.Join(' ', new int[]{
                baseDrink.Sweetness, baseDrink.Fizzy, newDrink.Sweetness, newDrink.Fizzy
            }));
        }
    }
}

public class Program
{
    private int _n;
    private List<int> _a;
    private List<int> _b;

    public static void Main(string[] args)
    {
        new Program();
    }

    public Program()
    {
        SharedStopwatch.Start();
        _a = new();
        _b = new();

        Input();
        Solve();
    }

    public void Input()
    {
        _n = int.Parse(ReadLine()!);
        for (int _ = 0; _ < _n; _++)
        {
            int[] buf = ReadLine()!.Split().Select(int.Parse).ToArray();
            _a.Add(buf[0]);
            _b.Add(buf[1]);
        }
    }

    public void Solve()
    {
        var factory = new Factory(_n, _a, _b);
        while (!factory.IsDone()) factory.SimpleAction();
        factory.Print();
    }
}