n, m, t, la, lb = map(int, input().split())
print(n, m, t, la, lb)

for _ in range(m):
  u, v = map(int, input().split())
  print(u, v)

order = list(map(int, input().split()))
print(*order)

for _ in range(n):
  x, y = map(int, input().split())
  print(x, y)
