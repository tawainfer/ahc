import io
import os
import re
import sys
import subprocess
from pathlib import Path

class Result:
  def __init__(self, dirname):
    self.dirname = dirname
    self.all = []
    self.ac = []
    self.wa = []
    self.sum = 0
    self.avg = 0
    self.max = -(1 << 60)
    self.min = (1 << 60)
    self.max_filename = ''
    self.min_filename = ''

  def add_ac(self, filename, score):
    self.ac.append((filename, score))
    self.all.append((filename, score))

    self.sum += score
    self.avg = self.sum / len(self.ac)
    if score > self.max:
      self.max = score
      self.max_filename = filename
    if score < self.min:
      self.min = score
      self.min_filename = filename

  def add_wa(self, filename, message):
    self.wa.append((filename, message))
    self.all.append((filename, message))

  def to_string(self):
    with io.StringIO() as buf:
      buf.write(f'[{self.dirname}] \n')
      buf.write(f'AC: {len(self.ac)}\n')
      buf.write(f'WA: {len(self.wa)}\n')
      # buf.write(f'sum: {self.sum}\n')
      buf.write(f'avg: {self.avg}\n')
      buf.write(f'max: {self.max} ({self.max_filename})\n')
      buf.write(f'min: {self.min} ({self.min_filename})\n\n')

      for t in self.all:
        buf.write(f'({t[0]}, {t[1]})\n')
      return buf.getvalue()

if __name__ == '__main__':
  # out_dir = Path(__file__).parent / 'out'
  # dirs = []
  # for item in sorted(out_dir.iterdir()):
  #   if item.is_dir():
  #     dirs.append(Path(item))

  dirs = sys.argv[1:]
  if not dirs:
    print('引数で集計対象とするディレクトリを指定してください')
    sys.exit(1)

  pattern = re.compile(r'^\d{4}\.txt$')
  for dir in dirs:
    dir = Path(__file__).parent / 'out' / Path(dir).name
    if (dir / 'aggregate.txt').is_file():
      continue

    files = []
    for file in sorted(os.listdir(dir)):
      if pattern.match(file):
        files.append(file)
    
    result = Result(dir.name)
    for file in files:
      file = Path(dir / file)

      score_str = subprocess.run(
        [
          'cargo', 'run', '--quiet', '--bin', 'vis',
          Path(__file__).parent / 'in' / file.name,
          dir / file.name
        ],
        cwd = Path(__file__).parent / 'tools',
        stderr = subprocess.PIPE,
        text = True
      ).stderr.strip().split()

      score = None
      if len(score_str) == 3:
        score = int(score_str[-1])

      if score is None:
        result.add_wa(file.name, ' '.join(score_str[:-3]))
      else:
        result.add_ac(file.name, score)

    with open((dir / 'aggregate.txt'), 'w') as wf:
      wf.write(result.to_string())
