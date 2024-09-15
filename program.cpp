#include <bits/stdc++.h>
using namespace std;
typedef long long ll;
const ll INF = 2e18;

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

class Factory {
private:
    deque<Drink> _finishedList;
    unordered_set<Drink, DrinkHash> _finishedSet;
    deque<Drink> _unfinishedList;
    unordered_set<Drink, DrinkHash> _unfinishedSet;
    ll _totalCost;
    vector<pair<Drink, Drink>> _logs;

public:
    Factory(ll n, const vector<ll> &a, const vector<ll> &b) : _totalCost(0) {
        _finishedSet.insert(Drink(0, 0));

        for (ll i = 0; i < n; ++i) {
            _unfinishedSet.insert(Drink(a[i], b[i]));
        }

        if (_unfinishedSet.count(Drink(0, 0))) {
            _unfinishedSet.erase(Drink(0, 0));
        }

        _finishedList.assign(_finishedSet.begin(), _finishedSet.end());
        _unfinishedList.assign(_unfinishedSet.begin(), _unfinishedSet.end());
        sort(_unfinishedList.begin(), _unfinishedList.end());
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
        _unfinishedSet.erase(newDrink);
        _unfinishedList.erase(find(_unfinishedList.begin(), _unfinishedList.end(), newDrink));
        _logs.push_back(make_pair(baseDrink, newDrink));
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

    void Prll() {
        cout << _logs.size() << endl;
        for (const auto &log : _logs) {
            cout << log.first.Sweetness << " " << log.first.Fizzy << " "
                << log.second.Sweetness << " " << log.second.Fizzy << endl;
        }
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

    while (!factory.IsDone()) {
        // factory.SimpleAction();
        factory.SimpleAction2();
    }

    factory.Prll();

    return 0;
}
