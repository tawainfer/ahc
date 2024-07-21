with open('tmp.txt', 'r') as f:
  lines = f.readlines()
  
  is_output = False
  for line in lines:
    if line.strip() == '[output start]':
      is_output = True
    elif line.strip() == '[output end]':
      is_output = False
    
    if is_output:
      if line.strip() != '[output start]':
        print(line.strip())