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

While entering values, a live translucent preview of the house appears in the viewport and updates as each prompt is answered. The preview disappears when the command finishes or is cancelled.

The command adds six Brep objects to the document:

- **Body** — rectangular box forming the walls, with door and window openings cut out
- **Roof** — gable roof with an overhang on each side
- **Door** — centered on the front face
- **Chimney** — offset toward the rear-right of the roof
- **Left window** — on the front face, between the door and the left wall
- **Right window** — on the front face, between the door and the right wall

## Display Conduit

The plugin uses a `HouseDisplayConduit` to render a translucent preview of the house directly in the Rhino viewport while the `BuildHouse` command is running. The conduit is part of Rhino's display pipeline and draws on top of existing document objects without adding anything permanent to the file.

### How it works

1. When the plugin loads, it creates a single shared `HouseDisplayConduit` instance and stores it on the plugin class. The conduit starts disabled.

2. When `BuildHouse` runs, the conduit is enabled before the first prompt. As you answer each prompt, the command builds the corresponding geometry and pushes it into the conduit, then triggers a redraw so the preview updates immediately in the viewport.

3. When the command finishes — whether it succeeds, fails, or you press Escape — the conduit is disabled in a `finally` block, and the preview disappears.

### What you see

|Stage                                                                  | What appears in the viewport                      |
|-----------------------------------------------------------------------|---------------------------------------------------|
| After picking insertion point & entering width/depth/height           | Translucent wall box                              |
| After entering roof height                                            | Gable roof added to preview                       |
| After roof is built                                                   | Door panel appears on front face                  |
| After door                                                            | Left and right window panels appear               |
| After windows                                                         | Chimney appears on the roof                       |
| Command completes                                                     | Preview removed; final geometry added to document |

### Notes

- The preview geometry is translucent (50% transparency) so you can see through it while positioning.
- The conduit draws shaded Breps only, it does not show the boolean cuts for the door and window openings during the preview. Those are applied to the final geometry when it is added to the document.
- The conduit is shared across command runs. It is disabled between runs and cleaned up when the plugin unloads.

## Building

The project uses `net48` (Rhino 7/8 Windows) and `net7.0` (Rhino 8). It depends on the `RhinoCommon` NuGet package and produces a `.rhp` assembly.

```
dotnet build
```

## Project Structure

```
StullerHousePlugin.cs      — PlugIn entry point; owns the shared HouseDisplayConduit instance

StullerHouseCommand.cs     — BuildHouse command implementation

HouseDisplayConduit.cs     — DisplayConduit subclass for live viewport previews

Properties/
  AssemblyInfo.cs          — GUID and metadata

EmbeddedResources/
  plugin-utility.ico       — icon
```
