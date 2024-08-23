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

    public Stopwatch Stopwatch { get { return _stopwatch; } }
    public TimeSpan Timeout { get { return _timeout; } }

    public static void Main(string[] args)
    {
        new Program();
    }

    public Program()
    {
        _stopwatch = Stopwatch.StartNew();
        _timeout = TimeSpan.FromMilliseconds(1900);
        Solve();
    }

    public void Solve()
    {
        while (Stopwatch.Elapsed <= Timeout) { }
    }
}
