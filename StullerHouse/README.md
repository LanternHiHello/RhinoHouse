# StullerHouse

A Rhino 3D plugin that generates a house model from a single command.

## Requirements

- Rhino 7 or Rhino 8 (Windows or Mac)

## Installation

1. Build the project in Visual Studio — this makes  `StullerHouse.rhp` in `bin/Debug/net48`.

2. Open Rhino and open File Explorer.

3. In File Explorer open the `StullerHouse` folder and find `StullerHouse.rhp` in the path `bin/Debug/net48`.

4. Click and drag the `.rhp` file into Rhino, and it's ready to use.

## Usage

Run the `BuildHouse` command in Rhino. You will be prompted for:

| Prompt                | Default | Description                              |
|-----------------------|---------|------------------------------------------|          
| House insertion point | Origin  | Base point for the house                 |
| House width           | 10.0    | Overall width along the X axis           |
| House depth           | 8.0     | Overall depth along the Y axis           |
| House wall height     | 6.0     | Height of the walls                      |
| Roof height           | 5.0     | Height of the gable peak above the walls |

The command adds four Brep objects to the document:

- **Body** — rectangular box forming the walls
- **Roof** — gable roof with an overhang on each side
- **Door** — centered on the front face
- **Chimney** — offset toward the rear-right of the roof

## Building

The project uses `net48` (Rhino 7/8 Windows) and `net7.0` (Rhino 8). It depends on the `RhinoCommon` NuGet package and produces a `.rhp` assembly.

```
dotnet build
```

## Project Structure

```
StullerHousePlugin.cs   — PlugIn entry point

StullerHouseCommand.cs  — BuildHouse command implementation

Properties/
  AssemblyInfo.cs       — GUID and metadata

EmbeddedResources/
  plugin-utility.ico    — icon
```
