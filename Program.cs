using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using static System.Console;

public static class Extensions {
  private static Random r = new Random();

  public static void Shuffle<T>(this IList<T> v) {
    for (int i = v.Count - 1; i > 0; i--) {
      int j = r.Next(0, i + 1);
      var tmp = v[i];
      v[i] = v[j];
      v[j] = tmp;
    }
  }
}

public static class DeepCopy {
  public static T Clone<T>(T obj) {
    string json = JsonConvert.SerializeObject(obj);
    return JsonConvert.DeserializeObject<T>(json);
  }
}

public class Seed {
  private int _id;
  private List<int> _evaluationItems;

  public int Id {get {return _id;}}
  public List<int> EvaluationItems {get {return _evaluationItems;} }

  public Seed(int id, List<int> evaluationItems) {
    _id = id;
    _evaluationItems = DeepCopy.Clone(evaluationItems);
  }

  public override string ToString() {
    return $"id: {_id} ({string.Join(",", _evaluationItems)})";
  }
}

public class Program {
  private int _n;
  private int _m;
  private int _t;
  private List<Seed> _seeds = new();

  public void Operate() {
    for(int _ = 0; _ < _t; _++) {
      _seeds.Shuffle();
      for(int i = 0; i < _n; i++) {
        for(int j = 0; j < _n; j++) {
          Write($"{_seeds[i * _n + j].Id}");
          if(j != _n - 1) {
            Write(" ");
          }
        }
        WriteLine();
      }
      Out.Flush();

      _seeds.Clear();
      for(int i = 0; i < _n * (_n - 1) * 2; i++) {
        var ev = ReadLine().Split().Select(int.Parse).ToList();
        _seeds.Add(new Seed(i, ev));
      }
    }
  }

  public Program() {
    var stopwatch = Stopwatch.StartNew();
    var timeout = TimeSpan.FromMilliseconds(1900);

    int[] tmp = ReadLine().Split().Select(int.Parse).ToArray();
    _n = tmp[0];
    _m = tmp[1];
    _t = tmp[2];

    for(int i = 0; i < _n * (_n - 1) * 2; i++) {
      var ev = ReadLine().Split().Select(int.Parse).ToList();
      _seeds.Add(new Seed(i, ev));
    }

    Operate();
  }

  public static void Main(string[] args) {
    new Program();
  }
}

