#include <bits/stdc++.h>
using namespace std;
typedef long long ll;
const ll INF = 2e18;
const mt19937 rng(static_cast<unsigned>(time(0)));

struct Drink {
    ll Sweetness;
    ll Fizzy;

    Drink(ll sweetness = 0, ll fizzy = 0) : Sweetness(sweetness), Fizzy(fizzy) {}

    bool operator==(const Drink &other) const {
        return tie(Sweetness, Fizzy) == tie(other.Sweetness, other.Fizzy);
    }

    bool operator<(const Drink &other) const {
        return tie(Sweetness, Fizzy) < tie(other.Sweetness, other.Fizzy);
    }
};

struct DrinkHash {
    size_t operator()(const Drink &drink) const {
        return hash<ll>()(drink.Sweetness) ^ hash<ll>()(drink.Fizzy);
    }
};

// class SharedStopwatch {
// public:
//     void Start() {
//         start = chrono::high_resolution_clock::now();
//     }

//     ll ElapsedMilliseconds() {
//         auto now = chrono::high_resolution_clock::now();
//         return chrono::duration_cast<chrono::milliseconds>(now - start).count();
//     }

// private:
//     chrono::high_resolution_clock::time_poll start;
// };

class TimeKeeper {
private:
    chrono::high_resolution_clock::time_point _startTime;
    int64_t _timeThreshold;
public:
    TimeKeeper(const int64_t &timeThreshold) : _startTime(chrono::high_resolution_clock::now()), _timeThreshold(timeThreshold) {
    }

    bool IsTimeOver() {
        using chrono::duration_cast;
        using chrono::milliseconds;
        auto diff = chrono::high_resolution_clock::now() - this->_startTime;
        return duration_cast<milliseconds>(diff).count() >= _timeThreshold;
    }
};

class Factory {
private:
    deque<Drink> _finishedList;
    unordered_set<Drink, DrinkHash> _finishedSet;
    deque<Drink> _unfinishedList;
    unordered_set<Drink, DrinkHash> _unfinishedSet;
    multiset<ll> _multisetA;
    multiset<ll> _multisetB;
    ll _totalCost = 0;
    ll _firstAction = -1;
    vector<pair<Drink, Drink>> _logs;

public:
    Factory(ll n, const vector<ll> &a, const vector<ll> &b) : _totalCost(0) {
        _finishedSet.insert(Drink(0, 0));

        for (ll i = 0; i < n; i++) {
            _unfinishedSet.insert(Drink(a[i], b[i]));
            _multisetA.insert(a[i]);
            _multisetB.insert(b[i]);
        }

        if (_unfinishedSet.count(Drink(0, 0))) {
            _unfinishedSet.erase(Drink(0, 0));
        }

        _finishedList.assign(_finishedSet.begin(), _finishedSet.end());
        _unfinishedList.assign(_unfinishedSet.begin(), _unfinishedSet.end());
        sort(_unfinishedList.begin(), _unfinishedList.end());
    }

    bool operator<(const Factory& other) const {
        return _totalCost < other._totalCost;
    }

    bool IsDone() {
        return _unfinishedSet.empty();
    }

    // void SortDrink() {
    //     sort(_finishedList.begin(), _finishedList.end());
    //     sort(_unfinishedList.begin(), _unfinishedList.end());
    // }

    ll CalcCost(const Drink &baseDrink, const Drink &newDrink) {
        if (baseDrink.Sweetness > newDrink.Sweetness || baseDrink.Fizzy > newDrink.Fizzy) return INF;
        return (newDrink.Sweetness - baseDrink.Sweetness) + (newDrink.Fizzy - baseDrink.Fizzy);
    }

    void MakeNewDrink(const Drink &baseDrink, const Drink &newDrink) {
        if (_finishedSet.find(baseDrink) == _finishedSet.end()) {
            throw runtime_error("存在しない飲料を指定しました");
        }

        if (CalcCost(baseDrink, newDrink) == INF) {
            throw runtime_error("元の飲料より薄い飲料は作れません");
        }

        _totalCost += CalcCost(baseDrink, newDrink);
        _finishedList.push_back(newDrink);
        _finishedSet.insert(newDrink);
        if(_unfinishedSet.find(newDrink) != _unfinishedSet.end()) {
            _unfinishedSet.erase(newDrink);
            _unfinishedList.erase(find(_unfinishedList.begin(), _unfinishedList.end(), newDrink));
        }
        if(_multisetA.find(newDrink.Sweetness) != _multisetA.end()) _multisetA.erase(_multisetA.find(newDrink.Sweetness));
        if(_multisetB.find(newDrink.Fizzy) != _multisetB.end()) _multisetB.erase(_multisetB.find(newDrink.Fizzy));
        _logs.push_back(make_pair(baseDrink, newDrink));
    }

    bool ReplenishSupportDrink() {
        if(IsDone()) return false;

        if(_multisetA.empty() || _multisetB.empty()) return false;

        Drink newDrink(*_multisetA.begin(), *_multisetB.begin());
        Drink baseDrink(0, 0);
        for(auto candidateDrink : _finishedSet) {
            if(CalcCost(candidateDrink, newDrink) < CalcCost(baseDrink, newDrink)) {
                baseDrink = candidateDrink;
            }
        }
        MakeNewDrink(baseDrink, newDrink);

        return true;
    }

    bool SimpleAction() {
        if (IsDone()) return false;

        Drink baseDrink(0, 0);
        Drink newDrink = _unfinishedList.back();
        MakeNewDrink(baseDrink, newDrink);

        return true;
    }

    bool SimpleAction2() {
        if (IsDone()) return false;

        Drink newDrink = _unfinishedList.front();
        Drink baseDrink(0, 0);
        for(auto candidateDrink : _finishedSet) {
            if(CalcCost(candidateDrink, newDrink) < CalcCost(baseDrink, newDrink)) {
                baseDrink = candidateDrink;
            }
        }
        MakeNewDrink(baseDrink, newDrink);

        return true;
    }

    bool ChokudaiSearchAction(ll beamWidth, ll beamDepth, ll beamNumber) {
        auto beam = vector<priority_queue<Factory>>(beamDepth + 1);
        for(int t = 0; t <= beamDepth; t++) beam[t] = priority_queue<Factory>();
        beam[0].push(*this);

        for(int cnt = 0; cnt < beamNumber; cnt++) {
            for(int t = 0; t < beamDepth; t++) {
                auto &nowBeam = beam[t];
                auto &nextBeam = beam[t + 1];
                for(int i = 0; i < beamWidth; i++) {
                    if(nowBeam.empty()) break;
                    Factory nowState = nowBeam.top();
                    if(nowState.IsDone()) break;
                    nowBeam.pop();
                    
                    for(int action = 0; action < 3; action++) {
                        Factory nextState = nowState;
                        if(action == 1) nextState.ReplenishSupportDrink();
                        nextState.SimpleAction2();
                        if(action == 2) nextState.ReplenishSupportDrink();
                        
                        if(t == 0) nextState._firstAction = action;
                        nextBeam.push(nextState);
                    }
                }
            }
        }

        for(int t = beamDepth; t >= 0; t--) {
            const auto &nowBeam = beam[t];
            if(!nowBeam.empty()) {
                if(nowBeam.top()._firstAction == 1) ReplenishSupportDrink();
                SimpleAction2();
                if(nowBeam.top()._firstAction == 2) ReplenishSupportDrink();
                return true;
            }
        }

        return false;
    }

    void Print() {
        cout << _logs.size() << endl;
        for (const auto &log : _logs) {
            cout << log.first.Sweetness << " " << log.first.Fizzy << " "
                << log.second.Sweetness << " " << log.second.Fizzy << endl;
        }
    }

    ll GetTotalCost() {
        return _totalCost;
    }
};

int main() {
    // SharedStopwatch sw;
    // sw.Start();

    ll n;
    cin >> n;

    vector<ll> a(n), b(n);
    for (ll i = 0; i < n; ++i) {
        cin >> a[i] >> b[i];
    }

    Factory factory(n, a, b);

    auto timeKeeper = TimeKeeper(1900);
    while(!factory.IsDone() && !timeKeeper.IsTimeOver()) {
        factory.ChokudaiSearchAction(5, 10, 1);
    }

    while (!factory.IsDone()) {
        factory.ReplenishSupportDrink();
        factory.SimpleAction2();
    }

    factory.Print();
    // cout << factory.GetTotalCost() << endl;

    return 0;
}
