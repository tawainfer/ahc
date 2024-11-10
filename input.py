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

  n = int(read_line())
  print(n)

  x = list()
  y = list()
  for _ in range(n):
    a, b = map(int, read_line().split())
    x.append(a)
    y.append(b)
    print(a, b)
