#!/bin/sh

cd $(dirname ${BASH_SOURCE[0]})

dotnet build
if [ $? -ne 0 ]
then
  exit 1
fi

mkdir -p './out'
file="$(date "+%Y%m%d-%H%M%S").txt"

echo "${file}"
echo '入力を貼り付けてください'
python input.py | dotnet run > "./out/${file}"

