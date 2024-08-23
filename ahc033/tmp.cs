public class Container {
  private int _id;
  // private int _y;
  // private int _x;

  public int ID {
    get {return _id;}
  }

  // public int Y {
  //   get {return _y;}
  // }

  // public int X {
  //   get {return _x;}
  // }

  public Container(int id, int y, int x) {
    _id = id;
    // _y = y;
    // _x = x;
  }
  

  public override string ToString() {
    var tmp = new StringBuilder();
    tmp.Append($"コンテナ{_id} ");
    // tmp.Append($"({_y}, {_x}) ");
    return tmp.ToString();
  }
}

public class Crane {
  private static bool _instance = false;

  private int _id;
  // private int _y;
  // private int _x;
  private bool _isBan;
  private bool _isLarge;
  private Container _grabbedContainer;

  public int ID {
    get {return _id;}
  }

  // public int Y {
  //   get {return _y;}
  // }

  // public int X {
  //   get {return _x;}
  // }

  public bool IsBan {
    get {return _isBan;}
  }

  public bool IsLarge {
    get {return _isLarge;}
  }

  public Container GrabbedContainer {
    get {return _grabbedContainer;}
  }

  public Crane(int id, int y, int x) {
    _id = id;
    // _y = y;
    // _x = x;
    _isBan = false;
    _isLarge = !(_instance);
    _instance = true;
  }

  public override string ToString() {
    var tmp = new StringBuilder();
    tmp.Append($"{(_isLarge ? "大" : "小")}クレーン{_id} ");
    // tmp.Append($"({_y}, {_x}) ");
    tmp.Append($"{(_isBan ? "破壊済み" : "使用可能")} ");
    tmp.Append($"掴んでいるコンテナ...{(_grabbedContainer is null ? "なし" : _grabbedContainer.ID.ToString())}");
    return tmp.ToString();
  }
}
