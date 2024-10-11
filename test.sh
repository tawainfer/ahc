#!/bin/sh

cd $(dirname ${BASH_SOURCE[0]})

testcase='1000'
if [ $# -ge 1 ]
then
  if [[ ! $1 =~ ^[0-9]+$ ]]
  then
    echo 'テストケース数を正しく入力してください'
    exit 1
  fi

  testcase=$1
fi

dotnet publish -c Release
if [ $? -ne 0 ]; then
  exit 1
fi

d=$(date "+%Y%m%d-%H%M%S")
mkdir -p "./out/${d}"

files=$(ls './in' | grep '^[0-9]\{4\}\.txt$' | sort)
files_count=$(echo "$files" | wc -l)

if [ $# -ge 2 ]
then
  echo 'shuffle'
  files=$(ls './in' | grep '^[0-9]\{4\}\.txt$' | shuf)
fi

if [ "$files_count" -lt "$testcase" ]
then
  testcase="$files_count"
fi

result_dir="${d}"
count=0
for file in $files
do
  if [ "$count" -ge "$testcase" ]
  then
    break
  fi

  count=$((count + 1))
  echo "${file} (${count}/${testcase})"
  python input.py "${file}" | './bin/Release/net7.0/linux-x64/publish/ahc038' > "./out/${result_dir}/${file}"

  cd tools
  cargo run --quiet --bin vis "../in/${file}" "../out/${result_dir}/${file}"
  cd ..
done

echo '集計中...'
python aggregate.py "${result_dir}"
cat "./out/${result_dir}/aggregate.txt"
