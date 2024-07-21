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

public class Field {
  private List<List<Seed>> _map = new();
  
  public List<List<Seed>> Map {get {return _map;}}

  public Field(int n, List<Seed> seeds) {
    for(int i = 0; i < n; i++) {
      _map.Add(new List<Seed>());
      for(int j = 0; j < n; j++) {
        _map[i].Add(seeds[i * n + j]);
      }
    }
  }

  public int PredictScore(int n) {
    // List<Seed> seeds = new();
    int res = 0;
    // int id = 0;

    for(int i = 0; i <= n - 1; i++) {
      for(int j = 0; j <= n - 2; j++) {
        // List<int> evaluationItems = new();
        for(int k = 0; k < _map[i][j].EvaluationItems.Count; k++) {
          // evaluationItems.Add((
          //   u[j][k] == '0'
          //   ? field.Map[i][j].EvaluationItems[k]
          //   : field.Map[i][j + 1].EvaluationItems[k]
          // ));
          // evaluationItems.Add(
          //   (field.Map[i][j].EvaluationItems[k]
          //   + field.Map[i][j + 1].EvaluationItems[k]) / 2
          // );
          res += (_map[i][j].EvaluationItems[k]
            + _map[i][j + 1].EvaluationItems[k]) / 2;
        }

        // seeds.Add(new Seed(id, evaluationItems));
        // id++;
      }
    }

    for(int i = 0; i <= n - 2; i++) {
      for(int j = 0; j <= n - 1; j++) {
        // List<int> evaluationItems = new();
        for(int k = 0; k < _map[i][j].EvaluationItems.Count; k++) {
          // evaluationItems.Add((
          //   v[j][k] == '0'
          //   ? _map[i][j].EvaluationItems[k]
          //   : _map[i + 1][j].EvaluationItems[k]
          // ));
          // evaluationItems.Add(
          //   (_map[i][j].EvaluationItems[k]
          //   + _map[i + 1][j].EvaluationItems[k]) / 2
          // );
          res += (_map[i][j].EvaluationItems[k]
            + _map[i + 1][j].EvaluationItems[k]) / 2;
        }

        // seeds.Add(new Seed(id, evaluationItems));
        // id++;
      }
    }

    return res;
  }
}

public class Seed {
  private int _id;
  private List<int> _evaluationItems;
  private int _evaluationSum;

  public int Id {get {return _id;}}
  public List<int> EvaluationItems {get {return _evaluationItems;}}
  public int EvaluationSum {get {return _evaluationSum;}}

  public Seed(int id, List<int> evaluationItems) {
    _id = id;
    _evaluationItems = DeepCopy.Clone(evaluationItems);
    _evaluationSum = _evaluationItems.Sum();
  }

  public override string ToString() {
    return $"id: {_id} ({string.Join(",", _evaluationItems)})";
  }
}

public class Responser {
  public List<Seed> Local(int n, Field field) {
    List<Seed> res = new();
    int id = 0;

    for(int i = 0; i <= n - 1; i++) {
      string[] u = ReadLine().Split();

      for(int j = 0; j <= n - 2; j++) {
        List<int> evaluationItems = new();
        for(int k = 0; k < u[j].Length; k++) {
          evaluationItems.Add((
            u[j][k] == '0'
            ? field.Map[i][j].EvaluationItems[k]
            : field.Map[i][j + 1].EvaluationItems[k]
          ));
        }

        res.Add(new Seed(id, evaluationItems));
        id++;
      }
    }

    for(int i = 0; i <= n - 2; i++) {
      string[] v = ReadLine().Split();

      for(int j = 0; j <= n - 1; j++) {
        List<int> evaluationItems = new();
        for(int k = 0; k < v[j].Length; k++) {
          evaluationItems.Add((
            v[j][k] == '0'
            ? field.Map[i][j].EvaluationItems[k]
            : field.Map[i + 1][j].EvaluationItems[k]
          ));
        }

        res.Add(new Seed(id, evaluationItems));
        id++;
      }
    }

    return res;
  }

  public List<Seed> Production(int n) {
    List<Seed> res = new();

    for(int i = 0; i < n * (n - 1) * 2; i++) {
      var ev = ReadLine().Split().Select(int.Parse).ToList();
      res.Add(new Seed(i, ev));
    }

    return res;
  }
}

public class Program {
  private int _n;
  private int _m;
  private int _t;
  private List<Seed> _seeds = new();

  public void Operate(bool isTest = false) {
    for(int _ = 0; _ < _t; _++) {
      _seeds.Sort((seed1, seed2) => seed1.EvaluationSum - seed2.EvaluationSum);
      _seeds.Reverse();
      while(_seeds.Count > _n * _n) {
        _seeds.RemoveAt(_seeds.Count - 1);
      }

      var stopwatch = Stopwatch.StartNew();
      var timeout = TimeSpan.FromMilliseconds(150);
      Field field = new Field(_n, DeepCopy.Clone(_seeds));
      int maxPredictScore = 0;

      while(stopwatch.Elapsed <= timeout) {
        _seeds.Shuffle();
        var tmpField = new Field(_n, DeepCopy.Clone(_seeds));
        int currentPredictScore = tmpField.PredictScore(_n);
        if(currentPredictScore > maxPredictScore) {
          // WriteLine($"up! {maxPredictScore} => {currentPredictScore}");
          maxPredictScore = currentPredictScore;
          field = DeepCopy.Clone(tmpField);
        }
      }

      if(isTest) WriteLine("[output start]");
      for(int i = 0; i < _n; i++) {
        for(int j = 0; j < _n; j++) {
          Write($"{field.Map[i][j].Id}");
          if(j != _n - 1) {
            Write(" ");
          }
        }
        WriteLine();
      }
      Out.Flush();
      if(isTest) WriteLine("[output end]");

      if(isTest) _seeds = new Responser().Local(_n, DeepCopy.Clone(field));
      else _seeds = new Responser().Production(_n);
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

    Operate(true);
    // Operate();
  }

  public static void Main(string[] args) {
    new Program();
  }
}

