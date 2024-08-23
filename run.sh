#!/bin/sh

cd $(dirname ${BASH_SOURCE[0]})
dotnet build

if [ $? -ne 0 ]
then
  exit 1
fi

mkdir -p output
echo '[start]'
python input.py | dotnet run > "./output/$(date "+%Y%m%d-%H%M%S").txt"
