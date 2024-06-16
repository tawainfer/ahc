using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using static System.Console;

public class MainClass {
  public static void Show(List<List<int>> f) {
    for(int i = 0; i < f.Count; i++) {
      for(int j = 0; j < f[i].Count; j++) {
        Write($"{f[i][j]} ".PadLeft(4));
      }
      WriteLine();
    }
  }

  public static bool Check(List<List<int>> f) {
    for(int i = 0; i < f.Count; i++) {
      for(int j = 0; j < f[i].Count; j++) {
        if(f[i][j] != 0) return false;
      }
    }
    return true;
  }

  // public static int Score(double abs_sum, double cost, double diff) {
  //   return Math.Round(1000000000 * abs_sum / (cost + diff));
  // }

  public static long Score(List<string> v, int abs_sum) {
    long cost = 0;
    foreach(string s in v) {
      if(s[0] == '+' || s[0] == '-' || s[0] == '!') {
        string t = s.Substring(1);
        cost += long.Parse(t);
      }
    }

    long diff = 0;
    // WriteLine($"cost: {cost}");
    // WriteLine($"score: {1000000000.0 * (double)abs_sum / (double)(cost + diff)}");
    return (long)Math.Round(1000000000.0 * (double)abs_sum / (double)(cost + diff)); 
  }

  public static List<string> Tidy(int base_cnt, int n, List<List<int>> e) {
    var res = new List<string>();
    var f = new List<List<int>>();
    for(int i = 0; i < e.Count; i++) {
      f.Add(new List<int>());
      for(int j = 0; j < e[i].Count; j++) {
        f[i].Add(e[i][j]);
      }
    }

    var my = new List<int>(){-1, 0, 1, 0};
    var mx = new List<int>(){0, -1, 0, 1};
    int cy = 0;
    int cx = 0;
    int w = 0;

    int cnt = base_cnt;
    while(!Check(f)) {
      var seen = new List<List<bool>>();
      for(int i = 0; i < n; i++) {
        seen.Add(new List<bool>());
        for(int j = 0; j < n; j++) {
          seen[i].Add(false);
        }
      }
      seen[cy][cx] = true;

      var q = new Queue<List<int>>();
      q.Enqueue(new List<int>(){cy, cx});
      var ptn = new List<List<int>>();

      while(q.Count >= 1) {
        int ccy = q.Peek()[0];
        int ccx = q.Peek()[1];
        q.Dequeue();

        for(int i = 0; i < 4; i++) {
          int eey = ccy + my[i];
          int eex = ccx + mx[i];
          if(!(0 <= eey && eey < n && 0 <= eex && eex < n)) continue;
          if(seen[eey][eex]) continue;
          seen[eey][eex] = true;
          q.Enqueue(new List<int>(){eey, eex});

          if((cnt == 0 && f[eey][eex] < 0) || (cnt >= 1 && f[eey][eex] > 0)) {
            ptn.Add(new List<int>(){eey, eex});
          }
        }
      }

      if(ptn.Count == 0) {
        cnt = 0;
        continue;
      }

      int ey = ptn[0][0];
      int ex = ptn[0][1];
      while(cy > ey) {
        res.Add("U");
        res.Add($"!{100 + w}");
        cy--;
      }
      while(cy < ey) {
        res.Add("D");
        res.Add($"!{100 + w}");
        cy++;
      }
      while(cx > ex) {
        res.Add("L");
        res.Add($"!{100 + w}");
        cx--;
      }
      while(cx < ex) {
        res.Add("R");
        res.Add($"!{100 + w}");
        cx++;
      }

      if(cnt >= 1) {
        int x = f[cy][cx];
        res.Add($"+{x}");
        w += x;
        f[cy][cx] = 0;
        cnt--;
      } else {
        int x = Math.Min(-f[cy][cx], w);
        if(x > 0) res.Add($"-{x}");
        w -= x;
        f[cy][cx] += x;

        if(w == 0) {
          cnt = base_cnt;
        }
      }

      cy = ey;
      cx = ex;
    }

    return res;
  }

  public static void Main(string[] args) {
    var stopwatch = Stopwatch.StartNew();
    var timeout = TimeSpan.FromSeconds(2.0);

    int n = int.Parse(ReadLine());
    int abs_sum = 0;
    var f = new List<List<int>>();
    for(int i = 0; i < n; i++) {
      f.Add(new List<int>());
      var tmp = ReadLine().Split().Select(int.Parse).ToArray();
      foreach(int x in tmp) {
        f[i].Add(x);
        abs_sum += (x > 0 ? x : -x);
      }
    }

    long max_score = -1;
    var ans = new List<string>();
    int bc = 1;
    while(stopwatch.Elapsed <= timeout) {
      List<string> res = Tidy(bc, n, f);
      long score = Score(res, abs_sum);
      if(score > max_score) {
        // WriteLine($"up!! bc = {bc} {max_score} => {score}");
        max_score = score;
        ans = res;
      } else {
        // WriteLine($"stay... bc = {bc} {max_score} => {score}");
      }
      bc++;
    }
    // WriteLine($"end bc = {bc}");

      foreach(string s in ans) {
        if(s[0] == '!') continue;
        WriteLine(s);
      }
  }
}
