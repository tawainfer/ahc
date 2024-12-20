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

  n, m, v = map(int, read_line().split())
  print(n, m, v)

  for _ in range(2 * n):
    print(read_line())
