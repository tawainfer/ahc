import sys
from pathlib import Path
from collections import deque

FILE = None
FILE_READLINES = None

def read_line():
  global FILE
  global FILE_READLINES
  
  if FILE:
    if FILE_READLINES is None:
      with open(Path(__file__).parent / 'in' / FILE, 'r') as rf:
        FILE_READLINES = deque(rf.readlines())
    return FILE_READLINES.popleft().strip()

  return input()

if __name__ == '__main__':
  if len(sys.argv) >= 2:
    FILE = sys.argv[1].strip()

  n, m, t, la, lb = map(int, read_line().split())
  print(n, m, t, la, lb)

  for _ in range(m):
    u, v = map(int, read_line().split())
    print(u, v)

  order = list(map(int, read_line().split()))
  print(*order)

  for _ in range(n):
    x, y = map(int, read_line().split())
    print(x, y)
