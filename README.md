# WordlistSmith
A tool to quickly scrape a website and generate a wordlist. Multithreading capable.

## Introduction

WordlistSmith (working title) is a C# implimentation of [CeWL](https://github.com/digininja/CeWL), which is still an awesome tool. But, when running it recently on an engagement I noticed it took a large amount of time to run against a particular website with a ton of sub-pages at a default depth (think a mile wide and an inch deep application). This pushed me to create WordlistSmith to speed up the process and set a limit on the number of pages spidered.

## Setup:

It's probably easiest to use the built version under Releases, just note that it is compiled in Debug mode. If you want to build the solution yourself, follow the steps below.

1. Load WordlistSmith.sln into Visual Studio 2019
2. Go to Build at the top and then Build Solution if no modifications are wanted

## Usage

I decided to have the verbose option enabled by default due to the good amount of information it shows. If you don't want this, use the `-q` or `--quiet` flag.

- Help menu <br>
`WordlistSmith.exe --help`

- Default run (min word length = 3, max length = 10, delay = 100 seconds, timeout = 15 seconds, threads = 10, max depth = 3) <br>
`WordlistSmith.exe -u https://www.example.com`

- Setting a max number of pages regardless of depth to 1000 <br>
`WordlistSmith.exe -u https://www.example.com --max-pages 1000`

- Quick run (may have issues if site is slow) <br>
`WordlistSmith.exe -u https://www.example.com --max-pages 1000 --threads 20 --delay 0 -o outputWordlist.txt`

## Props
Thanks to Robin ([@digininja](https://twitter.com/digininja)) for the great work and inspiration on CeWL.
