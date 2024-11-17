ma = 1000
cnt = 1
while cnt * 4 + (cnt - 1) * 4 <= ma:
  print(f'cnt={cnt} {cnt * 3} {(cnt - 1) * 4} {cnt * 3 + (cnt - 1) * 4}')
  cnt += 1
