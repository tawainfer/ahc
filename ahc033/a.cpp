#include <bits/stdc++.h>
using namespace std;
const int INF = 1e9;

int N;
vector<vector<int>> F(5, vector<int>(5));
vector<vector<int>> A(5, vector<int>(5, -1));
deque<deque<int>> B(5, deque<int>(5));
vector<vector<int>> C(5, vector<int>());
deque<deque<char>> ANS(5);
vector<pair<int, int>> CONTAINER_POS(25, make_pair(-1, -1));
vector<pair<int, int>> CRANE_POS(5, make_pair(-1, -1));

void update_container_pos() {
  for(auto &x : CONTAINER_POS) {
    x = make_pair(-1, -1);
  }

  for(int i = 0; i < N; i++) {
    for(int j = 0; j < N; j++) {
      if(0 <= A[i][j] && A[i][j] <= N * N) {
        CONTAINER_POS[A[i][j]] = make_pair(i, j);
      }
    }
  }
}

void move_crane(int x, string ptn) {
  int cy = CRANE_POS[x].first;
  int cx = CRANE_POS[x].second;

  for(char c : ptn) {
    switch(c) {
      case 'U':
        cy--;
        break;
      case 'D':
        cy++;
        break;
      case 'L':
        cx--;
        break;
      case 'R':
        cx++;
        break;
      default:
        break;
    }

// cout << c;
    if(!(0 <= cy && cy < N && 0 <= cx && cx < N)) {
// cout << cy << " " << cx << endl;
      throw runtime_error("クレーン" + to_string(x) + "が範囲外に移動");
    }
  }

  CRANE_POS[x] = make_pair(cy, cx);
}

void ban(int x) {
  if(CRANE_POS[x].first == -1) {
    throw runtime_error("クレーン" + to_string(x) + "は破壊済");
  }
  CRANE_POS[x] = make_pair(-1, -1);
}

void debug() {
  cout << "\n[debug start]\n";

  cout << "F\n";
  for(int i = 0; i < N; i++) {
    for(int j = 0; j < N; j++) {
      printf("%3d ", F[i][j]);
    }
    cout << endl;
  }

  cout << "A\n";
  for(int i = 0; i < N; i++) {
    for(int j = 0; j < N; j++) {
      printf("%3d ", A[i][j]);
    }
    cout << endl;
  }

  cout << "B\n";
  for(int i = 0; i < B.size(); i++) {
    for(int j = 0; j < B[i].size(); j++) {
      printf("%2d ", B[i][j]);
    }
    cout << endl;
  }

  cout << "C\n";
  for(int i = 0; i < C.size(); i++) {
    for(int j = 0; j < C[i].size(); j++) {
      printf("%2d ", C[i][j]);
    }
    cout << endl;
  }

  cout << "CONTAINER_POS\n";
  for(int i = 0; i < N * N; i++) {
    printf("[%d] = (%d, %d)\n", i, CONTAINER_POS[i].first, CONTAINER_POS[i].second);
  }

  cout << "CRANE_POS\n";
  for(int i = 0; i < N; i++) {
    printf("[%d] = (%d, %d)\n", i, CRANE_POS[i].first, CRANE_POS[i].second);
  }
  cout << "\n[debug end]\n";
}

void init() {
  string s = "PRRRQLLLPRRQLLPRQL";
  for(int i = 0; i < N; i++) {
    for(char c : s) {
      ANS[i].push_back(c);
    }
  }

  for(int i = 0; i < N; i++) {
    for(int j = N - 2; j >= 0; j--) {
      A[i][j] = B[i].front();
      B[i].pop_front();
    }
  }

  update_container_pos();
  for(int i = 0; i < N; i++) {
    move_crane(i, s);
  }
}

void pack() {
  // test
  // for(int i = 1; i < N; i++) {
  //   ANS[i].push_back('B');
  // }

  for(int t = 0; t < N; t++) {
    for(int k = 0; k < N; k++) {
      if(k == 0) {
        string s = "PRRRR";
        int d = t - (A[t][0] / N);
        s += string(abs(d), (d <= 0 ? 'D' : 'U'));
        s += "Q";
        d = (A[t][0] / N) - ((t + 1) % N);
        s += string(abs(d), (d <= 0 ? 'D' : 'U'));
        s += "LLLL";

        ANS[0].insert(ANS[0].end(), s.begin(), s.end());
        move_crane(0, s);

        A[t][0] = B[t].front();
        B[t].pop_front();
      } else {
        string s = "";
        if(t == 0) {
          s += string(4 - k, 'R') + string(k - 1, 'U') + ".U";
        } else if(t == 4) {
          s += "." + string(N - k, 'D') + string(k - 1, 'R');
        }

        s += string(ANS[0].size() - ANS[k].size() - s.size(), '.');
        ANS[k].insert(ANS[k].end(), s.begin(), s.end());
        move_crane(k, s);
      }
    }
  }

  while(ANS[0].back() == 'L') {
    for(int i = 0; i < N; i++) {
      ANS[i].pop_back();
    }
    move_crane(0, "R");
  }

  update_container_pos();
}

void complete_simply() {
  for(int i = 1; i <= 4; i++) {
    ANS[i].push_back('B');
    ban(i);
  }

  string s = "";
  for(int i = 0; i < N * N; i++) {
    if(CONTAINER_POS[i].first == -1) continue;

    while(CONTAINER_POS[i] != CRANE_POS[0]) {
      if(CONTAINER_POS[i].second < CRANE_POS[0].second) {
        ANS[0].push_back('L');
        move_crane(0, "L");
        continue;
      } else if(CONTAINER_POS[i].first < CRANE_POS[0].first) {
        ANS[0].push_back('U');
        move_crane(0, "U");
        continue;
      } else {
        ANS[0].push_back('D');
        move_crane(0, "D");
        continue;
      }
    }

    ANS[0].push_back('P');

    while(CRANE_POS[0] != make_pair(i / N, N - 1)) {
      if(CRANE_POS[0].second < N - 1) {
        ANS[0].push_back('R');
        move_crane(0, "R");
        continue;
      } else if((i / N) < CRANE_POS[0].first) {
        ANS[0].push_back('U');
        move_crane(0, "U");
        continue;
      } else {
        ANS[0].push_back('D');
        move_crane(0, "D");
        continue;
      }
    }

    ANS[0].push_back('Q');
  }
}

void answer() {
  int ignore = 0;
  for(int i = 0; i < ANS.size(); i++) {
    for(int j = 0; j < ANS[i].size(); j++) {
      bool check = false;
      for(int k = 0; k < N; k++) {
        if(ANS[k][j] != '.' || (ANS[k][j] != ANS[(k + 1) % N][j])) {
          check = true;
          break;
        }
      }
      
      if(check) {
        cout << ANS[i][j];
      } else {
        ignore++;
      }
    }
    cout << endl;
  }
}

void init2() {
  string s = "PRQLPRRQLLPRRRQRDD.";
  ANS[0].insert(ANS[0].end(), s.begin(), s.end());
  move_crane(0, s);

  s = "PRRRQLLLPRRQLLPRQ";
  for(int i = 1; i < N; i++) {
    string t = "";
    switch(i) {
      case 1:
        t = "LD";
        break;
      case 2:
        t = "..";
        break;
      case 3:
        t = "RU";
        break;
      case 4:
        t = "B";
        ban(4);
        break;
    }

    t = s + t;
    ANS[i].insert(ANS[i].end(), t.begin(), t.end());
    if(i != 4) move_crane(i, t);
  }

  for(int i = 0; i < 3; i++) {
    A[0][i + 1] = B[0].front();
    B[0].pop_front();
  }
  A[0][0] = B[0].front();
  B[0].pop_front();

  for(int i = 1; i < N; i++) {
    for(int j = N - 2; j >= 0; j--) {
      A[i][j] = B[i].front();
      B[i].pop_front();
    }
  }

  update_container_pos();
}

int dis(pair<int, int> x, pair<int, int> y) {
  return abs(x.first - y.first) + abs(x.second - y.second);
}

void pack2() {
  int cnt = 0;
  while(cnt < N * N) {
    cnt++;

    pair<int, int> min_ptn = make_pair(INF, -1); // dis, target
    vector<int> idx = {0, 1, 2, 3, 4};
    random_device seed_gen;
    mt19937 engine(seed_gen());
    shuffle(idx.begin(), idx.end(), engine);

    for(int i : idx) {
      if(C[i].size() >= 6) continue;
      int target = C[i].back() + 1;
      if(CONTAINER_POS[target].first == -1) continue;
      int d = dis(CRANE_POS[0], CONTAINER_POS[target]) + dis(CONTAINER_POS[target], make_pair(target / N, N - 1));
      
      if(d < min_ptn.first) {
        min_ptn = make_pair(d, target);
      }
    }
// cout << "min_cnt=(" << min_ptn.first << "," << min_ptn.second << ")\n";
    string s0 = "";
    string tmp = "";
    tmp += string(abs(CRANE_POS[0].first - CONTAINER_POS[min_ptn.second].first), (
      CRANE_POS[0].first >= CONTAINER_POS[min_ptn.second].first ? 'U' : 'D'
    ));
    tmp += string(abs(CRANE_POS[0].second - CONTAINER_POS[min_ptn.second].second), (
      CRANE_POS[0].second >= CONTAINER_POS[min_ptn.second].second ? 'L' : 'R'
    ));

    move_crane(0, tmp);
    s0 += tmp;
    s0 += "P";

    tmp = "";
    tmp += string(abs(CRANE_POS[0].second - (N - 1)), (
      CRANE_POS[0].second >= (N - 1) ? 'L' : 'R'
    ));
    tmp += string(abs(CRANE_POS[0].first - (min_ptn.second / N)), (
      CRANE_POS[0].first >= (min_ptn.second / N) ? 'U' : 'D'
    ));

    move_crane(0, tmp);
    s0 += tmp;
    s0 += "Q";
    ANS[0].insert(ANS[0].end(), s0.begin(), s0.end());

    int check_dis = min_ptn.first;
    int wait = (int)distance(s0.begin(), find(s0.begin(), s0.end(), 'Q')) + 1;
    for(char c : s0) {
      if(!(c == 'U' || c == 'D' || c == 'Q')) {
        wait++;
      }
    }

    for(int i = 1; i <= 3; i++) {
      string s = "";
// cout << "i=" << i << endl;
// cout << s0 << endl;

      s += string(distance(s0.begin(), find(s0.begin(), s0.end(), 'L')), s0.front());
      if(i <= 4 - count(s0.begin(), s0.end(), 'L')) {
        s += string(count(s0.begin(), s0.end(), 'L'), '.');
        // s += "PRQL";
        s += (A[CONTAINER_POS[min_ptn.second].first][i - 1] == -1 ? ".R.L" : "PRQL");
// cout << CONTAINER_POS[min_ptn.second].first << " " << (min_ptn.second / N) << " " << abs(CONTAINER_POS[min_ptn.second].first - (min_ptn.second / N)) << endl;
        tmp += string(abs(CONTAINER_POS[min_ptn.second].first - (min_ptn.second / N)), (
          CONTAINER_POS[min_ptn.second].first >= (min_ptn.second / N) ? 'U' : 'D'
        ));
        check_dis = max(check_dis, (int)s.size());
        wait = max(wait, (int)distance(s.begin(), find(s.begin(), s.end(), 'Q')) + 1);
      } else {
        int my = (CONTAINER_POS[min_ptn.second].first == 0 ? 1 : -1);
        string tmp = (CONTAINER_POS[min_ptn.second].first == 0 ? "D" : "U");
        tmp += string(wait - 2, '.');
        // tmp += (CONTAINER_POS[min_ptn.second].first == 0 ? "U" : "R");
        tmp += string(abs((CONTAINER_POS[min_ptn.second].first + my) - (min_ptn.second / N)), (
          (CONTAINER_POS[min_ptn.second].first + my) >= (min_ptn.second / N) ? 'U' : 'D'
        ));
        s += tmp;
      }

      int tmp_cnt = 0;
      for(int j = s0.size() - 2; j >= 0; j--) {
        if(!(s0[s0.size() - 2] == 'U' || s0[s0.size() - 2] == 'D')) break;
        if(s0[j] != s0[s0.size() - 2]) break;
        tmp_cnt++;
      }
      if(i <= 4 - count(s0.begin(), s0.end(), 'L')) s += string(tmp_cnt, s0[s0.size() - 2]);

// cout << "path=" << s << endl;
      move_crane(i, s);
      ANS[i].insert(ANS[i].end(), s.begin(), s.end());

      // debug();
      // answer();
    }

    int max_size = 0;
    for(int i = 0; i < N; i++) {
      if(ANS[i].size() > max_size) {
        max_size = ANS[i].size();
      }
    }
    for(int i = 0; i < N; i++) {
      while(ANS[i].size() < max_size) {
        ANS[i].push_back('.');
      }
    }

    for(int i = CONTAINER_POS[min_ptn.second].second; i >= 0; i--) {
      if(i == 0) {
        if(B[CONTAINER_POS[min_ptn.second].first].size() >= 1) {
          A[CONTAINER_POS[min_ptn.second].first][0] = B[CONTAINER_POS[min_ptn.second].first].front();
          B[CONTAINER_POS[min_ptn.second].first].pop_front();
        } else {
          A[CONTAINER_POS[min_ptn.second].first][0] = -1;
        }
      } else {
        A[CONTAINER_POS[min_ptn.second].first][i] = A[CONTAINER_POS[min_ptn.second].first][i - 1];
      }
    }
    // A[CONTAINER_POS[min_ptn.second].first][CONTAINER_POS[min_ptn.second].second] = (
    //   B[CONTAINER_POS[min_ptn.second].first].size() >= 1 ? B[CONTAINER_POS[min_ptn.second].first].front() : -1
    // );
    // if(B[CONTAINER_POS[min_ptn.second].first].size() >= 1) B[CONTAINER_POS[min_ptn.second].first].pop_front();
    C[min_ptn.second / N].push_back(min_ptn.second);
    update_container_pos();

    // debug();
    // answer();
  }
}

int main() {
  cin >> N;
  for(int i = 0; i < N; i++) {
    for(int j = 0; j < N; j++) {
      cin >> F[i][j];
      B[i][j] = F[i][j];
    }
  }

  for(int i = 0; i < N; i++) {
    C[i].push_back(i * 5 - 1);
  }

  for(int i = 0; i < N; i++) {
    CRANE_POS[i] = make_pair(i, 0);
  }

  // init();
  // pack();

  // for(int i = 0; i < N; i++) {
  //   string s = (i == 0 ? "L" : ".");
  //   s += "PRQLLPRRQLLLPRRRQLLLLPRRRRQ";
  //   ANS[i].insert(ANS[i].end(), s.begin(), s.end());
  // }

  // complete_simply();

  init2();
  pack2();
  // debug();
  answer();
}