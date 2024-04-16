
# Launcher

This project is a game launcher application that fetches configuration and updates from a server, and launches the game with the latest updates.


## Prerequisites

Before you begin, ensure you have met the following requirements:
- You have installed [.NET Framework 3.5](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net35-sp1).
- You have a Windows machine. The project is built with C# and WPF which are native to Windows.
- You have read and installed the [Visual Studio](https://visualstudio.microsoft.com/) documentation.


## Installation
Open the terminal where you want to locate the project

Clone the project to your local folder
```bash
  git clone https://github.com/nhaantran/Launcher-NewVersion.git
```
Navigate to the project directory
```bash
  cd Launcher-NewVersion
```
Open in Visual Studio and keep the terminal open
```bash
  start Launcher-NewVersion.sln
```
Build the project using Ctrl + Shift + B or follow the image
![Build Solution](https://github.com/nhaantran/Launcher-NewVersion/tree/master/Launcher-NewVersion/img/README/Build_solution.jpg)

Add new folder
```bash
  cd Launcher-NewVersion/bin/Debug
  mkdir Data/Libs
```

Open the .../Launcher-NewVersion/bin/Debug folder


Add launcher.json and messagebox.json

Add config file and run the project
```bash
  start Launcher.exe
```