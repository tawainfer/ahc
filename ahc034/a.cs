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

  public static long Score(List<string> v, int abs_sum) {
    long cost = 0;
    foreach(string s in v) {
      if(s[0] == '+' || s[0] == '-' || s[0] == '!') {
        string t = s.Substring(1);
        cost += long.Parse(t);
      }
    }

    long diff = 0;
    return (long)Math.Round(1000000000.0 * (double)abs_sum / (double)(cost + diff)); 
  }

  public static List<string> Move(int sy, int sx, int gy, int gx) {
    var res = new List<string>();

    while(sy > gy) {
      res.Add("U");
      res.Add($"!{100}");
      sy--;
    }
    while(sy < gy) {
      res.Add("D");
      res.Add($"!{100}");
      sy++;
    }
    while(sx > gx) {
      res.Add("L");
      res.Add($"!{100}");
      sx--;
    }
    while(sx < gx) {
      res.Add("R");
      res.Add($"!{100}");
      sx++;
    }

    return res;
  }

  public static List<string> Tidy(int cy, int cx, int max_w, int n, List<List<int>> e) {
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
    int w = 0;

    bool dig = true;
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

          if((!dig && f[eey][eex] < 0) || (dig && f[eey][eex] > 0)) {
            ptn.Add(new List<int>(){eey, eex});
          }
        }
      }

      if(ptn.Count == 0) {
        dig = false;
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

      if(dig) {
        int x = f[cy][cx];
        res.Add($"+{x}");
        w += x;
        f[cy][cx] = 0;
        if(w + x > max_w) dig = false;
      } else {
        int x = Math.Min(-f[cy][cx], w);
        if(x > 0) res.Add($"-{x}");
        w -= x;
        f[cy][cx] += x;
        if(w == 0) dig = true;
      }

      cy = ey;
      cx = ex;
    }

    return res;
  }

  public static void Main(string[] args) {
    var stopwatch = Stopwatch.StartNew();
    var timeout = TimeSpan.FromSeconds(1.9);

    int n = int.Parse(ReadLine());
    int abs_sum = 0;
    var points = new List<List<int>>();

    var f = new List<List<int>>();
    for(int i = 0; i < n; i++) {
      f.Add(new List<int>());
      var tmp = ReadLine().Split().Select(int.Parse).ToArray();
      for(int j = 0; j < n; j++) {
        int x = tmp[j];
        f[i].Add(x);
        int abs = (x > 0 ? x : -x);
        abs_sum += abs;
        points.Add(new List<int>(){abs, i, j});
      }
    }
    points.Sort((v1, v2) => v2[0] - v1[0]);
    int min_abs = points[0][0];

    long max_score = -1;
    var ans = new List<string>();

    int search_point = 0;
    // while(stopwatch.Elapsed < timeout) {
    foreach(var p in points) {
      int abs = p[0];
      int ty = p[1];
      int tx = p[2];
      // if(abs != min_abs) break;
      if(stopwatch.Elapsed >= timeout) break;
      search_point++;

      var rand = new Random();
      // int ty = rand.Next(n);
      // int tx = rand.Next(n);
      var res1 = Move(0, 0, ty, tx);

      for(int i = 1; i <= 10; i++) {
        int max_w = rand.Next(150, 250);
        // int max_w = 30 * i;
        // int max_w = 200;

        if(stopwatch.Elapsed >= timeout) break;
        List<string> res2 = Tidy(ty, tx, max_w, n, f);

        var res = new List<string>();
        foreach(string s in res1) res.Add(s);
        foreach(string s in res2) res.Add(s);

        long score = Score(res, abs_sum);
        if(score > max_score) {
          // WriteLine($"up!! search_point[{search_point}][{i}] max_w = {max_w} {max_score} => {score}");
          max_score = score;
          ans = res;
        } else {
          // WriteLine($"stay... search_point[{search_point}]-[{i}] max_w = {max_w} {max_score} => {score}");
        }
      }
    }

    // WriteLine($"last score: {Score(ans, abs_sum)}");
    // WriteLine($"search_point: {search_point}");
    foreach(string s in ans) {
      if(s[0] == '!') continue;
      WriteLine(s);
    }
  }
}
