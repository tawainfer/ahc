using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using Newtonsoft.Json;
using static System.Console;

public static class Extensions {
  private static Random r = new Random();

  public static void Shuffle<T>(this IList<T> list) {
    for (int i = list.Count - 1; i > 0; i--) {
      int j = r.Next(0, i + 1);
      var tmp = list[i];
      list[i] = list[j];
      list[j] = tmp;
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
  private int _size;
  // private Dictionary<int, Crane> _cranes = new Dictionary<int, Crane>();
  // private Dictionary<int, Container> _containers = new Dictionary<int, Container>();
  private List<List<char>> _processes = new List<List<char>>();
  private List<List<int>> _craneMap = new List<List<int>>();
  private List<List<int>> _containerMap = new List<List<int>>();
  private List<int> _grabbedContainer = new List<int>();
  private List<List<int>> _ready = new List<List<int>>();
  private List<List<int>> _done = new List<List<int>>();

  private Field(Field parent) {
    this._size = parent._size;
    this._processes = DeepCopy.Clone(parent._processes);
    this._craneMap = DeepCopy.Clone(parent._craneMap);
    this._containerMap = DeepCopy.Clone(parent._containerMap);
    this._grabbedContainer = DeepCopy.Clone(parent._grabbedContainer);
    this._ready = DeepCopy.Clone(parent._ready);
    this._done = DeepCopy.Clone(parent._done);
  }

  public Field Clone() {
    return new Field(this);
  }

  public Field(int size, List<List<int>> ready) {
    _size = size;
    _ready = ready;
    
    // for(int i = 0; i < _size; i++) {
    //   for(int j = 0; j < _size; j++) {
    //     if(j == 0) {
    //       _containers[_ready[i][j]] = new Container(_ready[i][j], i, 0);
    //     } else {
    //       _containers[_ready[i][j]] = new Container(_ready[i][j], -1, -1);
    //     }
    //   }
    // }

    for(int i = 0; i < _size; i++) {
      var tmp = new List<int>();
      var tmp2 = new List<int>();
      for(int j = 0; j < _size; j++) {
        tmp.Add(-1);
        tmp2.Add(-1);
      }
      _craneMap.Add(tmp);
      _containerMap.Add(tmp2);
      
      _done.Add(new List<int>());
    }

    for(int i = 0; i < _size; i++) {
      CarryIn(i);
      SetCrane(i, i, 0);
      _grabbedContainer.Add(-1);
      _processes.Add(new List<char>());
    }
  }

  public int Size {
    get {return _size;}
  }

  public int Turn {
    get {
      int maxTurn = 0;
      for(int i = 0; i < _size; i++) {
        if(_processes[i].Count > maxTurn) {
          maxTurn = _processes[i].Count;
        }
      }
      return maxTurn;
    }
  }

  private int CountInversion(List<int> a) {
    int res = 0;
    for(int i = 0; i < a.Count; i++) {
      for(int j = i + 1; j < a.Count; j++) {
        if(a[i] > a[j]) {
          res++;
        }
      }
    }
    return res;
  }

  private void CarryIn(int row) {
    if(!(0 <= row && row < _size)) throw new Exception("範囲外の行");
    if(_ready[row].Count == 0) throw new Exception($"待機列{row}にコンテナがありません");
    if(_containerMap[row][0] != -1) throw new Exception($"搬入口({row},{0})に別のコンテナが存在します");
    if(GetCraneID((row, 0)) != -1 && GetGrabbedContainer(GetCraneID((row, 0))) != -1) throw new Exception($"搬入口({row},{0})にコンテナを持ち上げているクレーンが存在します");

    _containerMap[row][0] = _ready[row][0];
    _ready[row].RemoveAt(0);
  }

  private void CarryOut() {
    for(int i = 0; i < _size; i++) {
      if(_containerMap[i][_size - 1] != -1) {
        _done[i].Add(_containerMap[i][_size - 1]);
        _containerMap[i][_size - 1] = -1;
      }
    }
  }

  public (int Ready, int Field, int Done, int All) GetContainerCount() {
    int ready = 0;
    for(int i = 0; i < _ready.Count; i++) {
      foreach(int x in _ready[i]) {
        ready++;
      }
    }

    int done = 0;
    for(int i = 0; i < _done.Count; i++) {
      foreach(int x in _done[i]) {
        done++;
      }
    }

    int field = _size * _size - ready - done;
    return (ready, field, done, _size * _size);
  }

  public int GetNextCarryOutContainer(int row) {
    if(!(0 <= row && row < _size)) {
      throw new Exception("範囲外の行を指定しています");
    }

    int next = row * _size + _done[row].Count;
    return (_done[row].Count == _size ? -1 : next);
  }

  private void SetCrane(int id, int y, int x) {
    if(!(0 <= y && y < _size && 0 <= x && x < _size)) throw new Exception("範囲外");
    _craneMap[y][x] = id;
  }

  public int GetCraneID((int Y, int X) p) {
    if(!(0 <= p.Y && p.Y < _size && 0 <= p.X && p.X < _size)) throw new Exception($"指定した座標({p.Y},{p.X})はフィールドの範囲外です");
    return _craneMap[p.Y][p.X];
  }

  public int GetContainerID((int Y, int X) p) {
    if(!(0 <= p.Y && p.Y < _size && 0 <= p.X && p.X < _size)) throw new Exception($"指定した座標({p.Y},{p.X})はフィールドの範囲外です");
    return _containerMap[p.Y][p.X];
  }

  public (int Y, int X) GetCranePos(int id) {
    for(int i = 0; i < _size; i++) {
      for(int j = 0; j < _size; j++) {
        if(_craneMap[i][j] == id) {
          return (i, j);
        }
      }
    }

    throw new Exception($"クレーン{id}はフィールドに存在しません");
  }

  public (int Y, int X) GetContainerPos(int id) {
    for(int i = 0; i < _size; i++) {
      for(int j = 0; j < _size; j++) {
        if(_containerMap[i][j] == id) {
          return (i, j);
        }
      }
    }

    throw new Exception($"コンテナ{id}はフィールドに存在しません");
  }

  public int GetGrabbedContainer(int id) {
    GetCranePos(id);
    return _grabbedContainer[id];
  }

  private void MoveCrane(int id, char direction, ref List<List<int>> afterCraneMap) {
    (int cy, int cx) = GetCranePos(id);
    (int ey, int ex) = direction switch {
      'U' => (cy - 1, cx),
      'D' => (cy + 1, cx),
      'L' => (cy, cx - 1),
      'R' => (cy, cx + 1),
      _ => (cy, cx),
    };

    if(!(0 <= ey && ey < _size && 0 <= ex && ex < _size)) {
      throw new Exception($"クレーン{id}が範囲外に移動しました");
    }

    // if(_craneMap[ey][ex] != -1) {
    //   throw new Exception($"クレーン{id}がクレーン{_craneMap[ey][ex]}と衝突しました");
    // }

    if(id != 0 && _grabbedContainer[id] != -1 && _containerMap[ey][ex] != -1) {
      throw new Exception($"小クレーン{id}はコンテナを掴んだ状態で別のコンテナが存在するマスに移動出来ません");
    }

    afterCraneMap[ey][ex] = id;
    // afterCraneMap[cy][cx] = -1;

    // if(cx == 0 && _grabbedContainer[id] != -1 && _ready[cy].Count >= 1) {
    //   CarryIn(cy);
    // }
  }

  private void Ban(int id, ref List<List<int>> afterCraneMap) {
    if(_grabbedContainer[id] != -1) {
      throw new Exception($"コンテナを掴んだ状態でクレーン{id}は爆破出来ません");
    }

    (int y, int x) = GetCranePos(id);
    afterCraneMap[y][x] = -1;
  }

  private void Grab(int id, ref List<List<int>> afterContainerMap, ref List<int> afterGrabbedContainer) {
    if(_grabbedContainer[id] != -1) {
      throw new Exception($"既にクレーン{id}はコンテナ{_grabbedContainer[id]}を掴んでいます");
    }

    (int y, int x) = GetCranePos(id);
    if(!(0 <= y && y < _size && 0 <= x && x < _size)) {
      throw new Exception($"クレーン{id}の位置が({y},{x})のように不正です");
    }

    // WriteLine(_containerMap[y][x]);
    // throw new Exception($"!{_containerMap[y][x]}!");
    // if(_containerMap[y][x] == -1) {
    //   throw new Exception($"クレーン{id}が現在いるマスにコンテナが存在しません");
    // }

    afterGrabbedContainer[id] = _containerMap[y][x];
    afterContainerMap[y][x] = -1;
  }

  private void Drop(int id, ref List<List<int>> afterContainerMap, ref List<int> afterGrabbedContainer) {
    if(_grabbedContainer[id] == -1) {
      throw new Exception($"掴んでいるコンテナがないためクレーン{id}に対して操作を行えません");
    }

    (int y, int x) = GetCranePos(id);
    if(_containerMap[y][x] != -1) {
      throw new Exception($"クレーン{id}の現在いるマスに別のコンテナが既に存在するためコンテナを置けません");
    }

    afterContainerMap[y][x] = _grabbedContainer[id];
    afterGrabbedContainer[id] = -1;
  }

  private bool NextPermutation(List<int> a) {
    if(a.Count <= 1) return false;

    int k = a.Count - 2;
    while(k >= 0 && a[k] >= a[k + 1]) {
      k--;
    }

    if(k < 0) {
      a.Reverse();
      return false;
    }

    int l = a.Count - 1;
    while(l > k && a[l] <= a[k]) {
      l--;
    }

    int tmp = a[k];
    a[k] = a[l];
    a[l] = tmp;

    a.Reverse(k + 1, a.Count - (k + 1));
    return true;
  }

  private bool IsValidTurn(in List<List<char>> processes, int turn) {
    var before = new List<(int Y, int X)>();
    var after = new List<(int Y, int X)>();
    for(int i = 0; i < _size; i++) {
      try {
        GetCranePos(i);
      } catch(Exception) {
        if(processes[i][turn] == '.') continue;
        throw new Exception($"破壊済みのクレーン{i}に操作を加えることは出来ません");
      }

      before.Add(GetCranePos(i));
      after.Add(processes[i][turn] switch {
        'P' => before[i],
        'Q' => before[i],
        '.' => before[i],
        'B' => (-1, -1),
        'U' => (before[i].Y - 1, before[i].X),
        'D' => (before[i].Y + 1, before[i].X),
        'L' => (before[i].Y, before[i].X - 1),
        'R' => (before[i].Y, before[i].X + 1),
        _ => throw new Exception("不正な入力"),
      });
    }

    for(int i = 0; i < before.Count; i++) {
      if((after[i].Y == -1 || after[i].X == -1) && after[i] != (-1, -1)) {
        WriteLine($"クレーン{i}が範囲外に移動しています");
        return false;
      }

      for(int j = i + 1; j < before.Count; j++) {
        if(before[i] == after[j] && after[i] == before[j]) {
          WriteLine($"クレーン{i}とクレーン{j}の場所を同時に入れ替えることは出来ません");
          return false;
        }
        if(after[i] == after[j]) {
          WriteLine($"クレーン{i}とクレーン{j}が衝突しています");
          return false;
        }
      }
    }

    return true;
  }

  public void Operate(List<List<char>> processes) {
    if(processes.Count != _size) {
      throw new Exception("操作列の数が間違っています");
    }

    int maxTurn = 0;
    for(int i = 0; i < _size; i++) {
      if(processes[i].Count > maxTurn) {
        maxTurn = processes[i].Count;
      }
    }
    for(int i = 0; i < _size; i++) {
      while(processes[i].Count < maxTurn) {
        processes[i].Add('.');
      }
    }

    for(int t = 0; t < maxTurn; t++) {
      if(!IsValidTurn(processes, t)) {
        throw new Exception($"ターン{t}で不正な操作が行われています");
      }

      var afterContainerMap = DeepCopy.Clone(_containerMap);
      var afterGrabbedContainer = DeepCopy.Clone(_grabbedContainer);
      var afterCraneMap = new List<List<int>>();
      for(int i = 0; i < _size; i++) {
        afterCraneMap.Add(new List<int>());
        for(int j = 0; j < _size; j++) {
          afterCraneMap[i].Add(-1);
        }
      }

      for(int c = 0; c < _size; c++) {
        switch(processes[c][t]) {
          case 'P':
            Grab(c, ref afterContainerMap, ref afterGrabbedContainer);
            break;
          case 'Q':
            Drop(c, ref afterContainerMap, ref afterGrabbedContainer);
            break;
          case 'B':
            Ban(c, ref afterCraneMap);
            break;
        }

        MoveCrane(c, processes[c][t], ref afterCraneMap);
      }

      _craneMap = DeepCopy.Clone(afterCraneMap);
      _containerMap = DeepCopy.Clone(afterContainerMap);
      _grabbedContainer = DeepCopy.Clone(afterGrabbedContainer);

      for(int i = 0; i < _size; i++) {
        if(afterContainerMap[i][_size - 1] == -1) {
          try {
            CarryIn(i);
          } catch(Exception) {
            // pass
          }
        }
      }
      CarryOut();

      int periodCount = 0;
      for(int i = 0; i < _size; i++) {
        if(processes[i][t] == '.') {
          periodCount++;
        }
      }

      if(periodCount != _size) {
        for(int i = 0; i < _size; i++) {
          _processes[i].Add(processes[i][t]);
        }
      }

      // debug
      // WriteLine(CalcScore());
    }
  }

  public int CalcDistance((int Y, int X) p1, (int Y, int X) p2) {
    return Math.Abs(p1.Y - p2.Y) + Math.Abs(p1.X - p2.X);
  }

  public int CalcScore() {
    int inv = 0;
    for(int i = 0; i < _size; i++) {
      inv += CountInversion(_done[i]);
    }

    int wrong = 0;
    for(int i = 0; i < _size; i++) {
      foreach(int x in _done[i]) {
        if(x / _size != i) {
          wrong++;
        }
      }
    }

    int yet = _size * _size;
    for(int i = 0; i < _size; i++) {
      yet -= _done[i].Count;
    }

    return Turn + (100 * inv) + (10000 * wrong) + (1000000 * yet);
  }

  public string GetAnswer() {
    var tmp = new StringBuilder();
    for(int i = 0; i < _size; i++) {
      foreach(char c in _processes[i]) {
        tmp.Append(c);
      }
      tmp.Append("\n");
    }
    return tmp.ToString();
  }

  public override string ToString() {
    var tmp = new StringBuilder();
    tmp.Append($"フィールド\n");

    tmp.Append($"マップ(クレーン、コンテナ)\n");
    for(int i = 0; i < _craneMap.Count; i++) {
      for(int j = 0; j < _craneMap[i].Count; j++) {
        tmp.Append($"(({_craneMap[i][j]},{(_craneMap[i][j] != -1 ? _grabbedContainer[_craneMap[i][j]] : -1)}){_containerMap[i][j]})".PadLeft(12));
      }
      tmp.Append("\n");
    }

    tmp.Append("待機\n");
    for(int i = 0; i < _size; i++) {
      tmp.Append($"<- [");
      foreach(var x in _ready[i]) {
        tmp.Append($"{x},");
      }
      tmp.Append("]\n");
    }

    tmp.Append("完了\n");
    for(int i = 0; i < _size; i++) {
      tmp.Append($"[");
      foreach(var x in _done[i]) {
        tmp.Append($"{x},");
      }
      tmp.Append("] <-\n");
    }

    return tmp.ToString();
  }
}

public class MainClass {
  public static void Input(ref int n, ref List<List<int>> a) {
    n = int.Parse(ReadLine());
    for(int i = 0; i < n; i++) {
      int[] tmp = ReadLine().Split().Select(int.Parse).ToArray();
      var tmp2 = new List<int>();
      foreach(int x in tmp) {
        tmp2.Add(x);
      }
      a.Add(tmp2);
    }
  }

  public static void Pack(ref Field f) {
    var processes = new List<List<char>>();
    for(int i = 0; i < f.Size; i++) {
      processes.Add(new List<char>());
    }
    processes[0].AddRange("PRQLPRRQLLPRRRQRDD");
    processes[1].AddRange("PRRRQLLLPRRQLLPRQD");
    processes[2].AddRange("PRRRQLLLPRRQLLPRQR");
    processes[3].AddRange("PRRRQLLLPRRQLLPRQR");
    processes[4].AddRange("PRRRQLLLPRRQLLPRQU");

    f.Operate(processes);
  }

  public static bool Tidy(ref Field f, List<int> order = null) {
    if(f.GetContainerCount().Done >= f.GetContainerCount().All) return false;

    if(order is null) {
      order = Enumerable.Range(0, f.Size).ToList();
    }

    int target = -1;
    int min_distance = int.MaxValue;

    foreach(int i in order) {
      int t = f.GetNextCarryOutContainer(i);
      if(t == -1) continue;

      try {f.GetContainerPos(t);}
      catch {continue;}

      int d = f.CalcDistance(f.GetCranePos(0), f.GetContainerPos(t)) + f.CalcDistance(f.GetContainerPos(t), (t / f.Size, f.Size - 1));
      if(d < min_distance) {
        target = t;
        min_distance = d;
      }
    }

    if(target == -1) {
      throw new Exception("ターゲットとなるコンテナが見つかりませんでした");
    }

    var processes = new List<List<char>>();
    for(int i = 0; i < f.Size; i++) {
      processes.Add(new List<char>());
    }

    for(int i = 0; i < Math.Abs(f.GetCranePos(0).Y - f.GetContainerPos(target).Y); i++) {
      processes[0].Add((f.GetCranePos(0).Y >= f.GetContainerPos(target).Y) ? 'U' : 'D');
    }
    for(int i = 0; i < Math.Abs(f.GetCranePos(0).X - f.GetContainerPos(target).X); i++) {
      processes[0].Add((f.GetCranePos(0).X >= f.GetContainerPos(target).X) ? 'L' : 'R');
    }

    processes[0].Add('P');

    for(int i = 0; i < Math.Abs(f.GetContainerPos(target).X - (f.Size - 1)); i++) {
      processes[0].Add((f.GetContainerPos(target).X >= (f.Size - 1)) ? 'L' : 'R');
    }
    for(int i = 0; i < Math.Abs(f.GetContainerPos(target).Y - (target / f.Size)); i++) {
      processes[0].Add((f.GetContainerPos(target).Y >= (target / f.Size)) ? 'U' : 'D');
    }

    processes[0].Add('Q');

    var cranePos = new List<(int Y, int X)>();
    var isGrabbedContainer = new List<bool>();
    for(int i = 0; i <= 4; i++) {
      cranePos.Add(f.GetCranePos(i));
      isGrabbedContainer.Add(f.GetGrabbedContainer(i) != -1);
    }

    int rCount = 0;
    foreach(char c in processes[0]) {
      switch(c) {
        case 'U':
          cranePos[0] = (cranePos[0].Y - 1, cranePos[0].X);
          break;
        case 'D':
          cranePos[0] = (cranePos[0].Y + 1, cranePos[0].X);
          break;
        case 'L':
          cranePos[0] = (cranePos[0].Y, cranePos[0].X - 1);
          break;
        case 'R':
          rCount++;
          cranePos[0] = (cranePos[0].Y, cranePos[0].X + 1);
          break;
      }

      for(int i = 1; i <= 4; i++) {
        switch(c) {
          case 'U':
            processes[i].Add('U');
            cranePos[i] = (cranePos[i].Y - 1, cranePos[i].X);
            break;
          case 'D':
            processes[i].Add('D');
            cranePos[i] = (cranePos[i].Y + 1, cranePos[i].X);
            break;
          case 'P':
            if(cranePos[i].Y == cranePos[0].Y && f.GetContainerID(cranePos[i]) != -1) {
              processes[i].Add('P');
              isGrabbedContainer[i] = true;
            } else {
              processes[i].Add('.');
            }
            break;
          case 'Q':
            processes[i].Add('.');
            break;
          case 'L':
            if(cranePos[i].Y == cranePos[0].Y) {
              if(cranePos[i].X == 0) {
                switch(cranePos[i].Y) {
                  case 0:
                    processes[i].Add('D');
                    cranePos[i] = (cranePos[i].Y + 1, cranePos[i].X);
                    break;
                  case 4:
                    processes[i].Add('U');
                    cranePos[i] = (cranePos[i].Y - 1, cranePos[i].X);
                    break;
                  default:
                    int u = 0;
                    int d = 0;
                    for(int x = 0; x <= f.Size - 2; x++) {
                      if(f.GetContainerID((cranePos[i].Y - 1, x)) != -1) u++;
                      if(f.GetContainerID((cranePos[i].Y + 1, x)) != -1) d++;
                    }
                    if(u >= d) {
                      processes[i].Add('U');
                      cranePos[i] = (cranePos[i].Y - 1, cranePos[i].X);
                    } else {
                      processes[i].Add('D');
                      cranePos[i] = (cranePos[i].Y + 1, cranePos[i].X);
                    }
                    break;
                }
              } else {
                processes[i].Add('L');
                cranePos[i] = (cranePos[i].Y, cranePos[i].X - 1);
              }
            } else {
              processes[i].Add('R');
              cranePos[i] = (cranePos[i].Y, cranePos[i].X + 1);
            }
            break;
          case 'R':
            switch(processes[i][processes[i].Count - (rCount * 2 + (rCount >= 2 ? 1 : 0))]) {
              case 'U':
                processes[i].Add('D');
                cranePos[i] = (cranePos[i].Y + 1, cranePos[i].X);
                break;
              case 'D':
                processes[i].Add('U');
                cranePos[i] = (cranePos[i].Y - 1, cranePos[i].X);
                break;
              case 'L':
                processes[i].Add('R');
                cranePos[i] = (cranePos[i].Y, cranePos[i].X + 1);
                break;
              case 'R':
                processes[i].Add('L');
                cranePos[i] = (cranePos[i].Y, cranePos[i].X - 1);
                break;
            }

            if(rCount == 1) {
              if(isGrabbedContainer[i]) {
                processes[i].Add('Q');
                isGrabbedContainer[i] = false;
              } else {
                processes[i].Add('.');
              }
            }
            break;
        }
      }
    }

    f.Operate(processes);
    // WriteLine(f.GetAnswer()); // debug
    return true;
  }

  public static int Main(string[] args) {
    var stopwatch = Stopwatch.StartNew();
    var timeout = TimeSpan.FromSeconds(2.9);

    int n = -1;
    var a = new List<List<int>>();
    Input(ref n, ref a);

    var f = new Field(n, a);
    Pack(ref f);

    var ff = new List<Field>();
    List<List<char>> processes;
    for(int i = 0; i < 16; i++) {
      ff.Add(f.Clone());
      processes = new List<List<char>>();
      for(int _ = 0; _ < f.Size; _++) {
        processes.Add(new List<char>());
      }

      processes[0].AddRange(i switch {
        0 => ".",
        1 => "...",
        2 => "...",
        3 => "U",
        4 => "U",
        5 => "U",
        6 => "UU",
        7 => "UU",
        8 => "UU",
        9 => "D",
        10 => "D",
        11 => "D",
        12 => ".",
        13 => ".",
        14 => ".",
        15 => ".",
      });
      processes[1].AddRange(i switch {
        0 => "L",
        1 => "PDQU",
        2 => "PRQR",
        3 => "UL",
        4 => "UPDQU",
        5 => "UPRQR",
        6 => "UUL",
        7 => "UUPDQU",
        8 => "UUPRQR",
        9 => "DL",
        10 => "DPDQU",
        11 => "DPRQR",
        12 => "LPDQUR",
        13 => "LPRQRR",
        14 => "RPDQRU",
        15 => "RPRQLL",
      });
      processes[2].AddRange(i switch {
        0 => "R",
        1 => "PLQL",
        2 => "PDQU",
        3 => "UR",
        4 => "UPLQL",
        5 => "UPDQU",
        6 => "UUR",
        7 => "UUPLQL",
        8 => "UUPDQU",
        9 => "DR",
        10 => "DPLQL",
        11 => "DPDQU",
        12 => "LPLQRR",
        13 => "LPDQLU",
        14 => "RPLQLL",
        15 => "RPDQUL",
      });
      processes[3].AddRange(i switch {
        0 => "U",
        1 => "PUQR",
        2 => "PLQU",
        3 => "UU",
        4 => "UPUQR",
        5 => "UPLQU",
        6 => "UUU",
        7 => "UUPUQR",
        8 => "UUPLQU",
        9 => "DU",
        10 => "DPUQR",
        11 => "DPLQU",
        12 => "LPUQRR",
        13 => "LPLQUR",
        14 => "RPUQLL",
        15 => "RPLQRU",
      });
      processes[4].AddRange(i switch {
        0 => "U",
        1 => "PRQU",
        2 => "PUQL",
        3 => "UU",
        4 => "UPRQU",
        5 => "UPUQL",
        6 => "UUU",
        7 => "UUPRQU",
        8 => "UUPUQL",
        9 => "DU",
        10 => "DPRQU",
        11 => "DPUQL",
        12 => "LPRQLU",
        13 => "LPUQRR",
        14 => "RPRQUL",
        15 => "RPUQLL",
      });

      ff[i].Operate(processes);
    }

    Field ans = null;
    var order = Enumerable.Range(0, f.Size).ToList();

    var costs = new List<int>();
    for(int i = 0; i < 16; i++) {
      costs.Add(1);
    }

    int idx = -1;
    while(stopwatch.Elapsed <= timeout) {
      idx = (idx + 1) % costs.Count;
      for(int j = 0; j < costs[idx]; j++) {
        if(stopwatch.Elapsed >= timeout) {
          break;
        }

        var fff = ff[idx].Clone();
        while(Tidy(ref fff, order)) order.Shuffle();

        int ct = (ans is null ? int.MaxValue : ans.Turn);
        if(fff.Turn < ct) {
          // WriteLine($"({idx}, {costs[idx]}) {ct} -> {fff.Turn}");
          ans = fff;
          costs[idx]++;
        }
      }
    }

    WriteLine(ans.GetAnswer());
    // WriteLine(ans.Turn); // debug
    return 0;
  }
}
