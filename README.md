# Save Game Mod for Schedule I Free Sample

## Overview

This mod enhances the gameplay by adding a convenient save functionality to the pause menu:

1. Adds a "Save Game" button to the pause menu
2. Bypasses all save cooldown restrictions
3. Ensures reliable saving through multiple methods

## Features

### 1. Save Button Integration

- Adds a "Save Game" button to the pause menu
- Positioned below the Quit button with proper spacing
- Matches the game's UI style by cloning the existing Quit button
- Uses TextMeshPro for text rendering to match the game's UI system

### 2. Cooldown Bypass System

- Completely removes the game's save cooldown restrictions
- Affects multiple save-related systems:
  - SaveManager cooldown checks
  - SaveInfo time restrictions
  - SavePoint SAVE_COOLDOWN constant
  - All CanSave methods

### 3. Advanced Save Functionality

- Implements multiple save methods in priority order:
  1. SaveInfo.SaveGame (static method)
  2. GenericSaveablesManager's Save/SaveAll methods
  3. BaseSaveables class methods
  4. Original SaveManager methods

## Installation

1. Clone the repository:
```bash
git clone https://github.com/kua8/SaveGameMod.git
cd SaveGameMod
```

2. Build the project:
```bash
dotnet build /p:Configuration=Release
```

3. Copy the compiled mod files from `bin/Release` to your game's mod directory
4. Launch the game with MelonLoader
5. The mod should be automatically detected and loaded

## Building from Source

### Prerequisites
- Visual Studio 2019 or higher
- .NET Framework 4.7.2 or higher
- MelonLoader development dependencies

### Build Steps
1. Open `SaveMod.csproj` in Visual Studio
2. Restore NuGet packages if needed
3. Build the solution in Release configuration
4. Find the compiled mod in the `bin/Release` directory

## Technical Details

### Initialization

- Loads on game startup
- Automatically finds and hooks into required game systems:
  - SaveManager
  - PauseMenu
  - UI Button system

### Error Handling

- Includes robust error handling for all operations
- Suppresses non-critical errors to keep console clean
- Automatically retries finding SaveManager every 5 seconds if not found
- Resets and reinitializes on scene changes

### Performance

- Silent operation with minimal console output
- Only creates the save button when needed
- Uses reflection to dynamically discover save methods
- Minimal performance impact on gameplay

### Technical Implementation

- Uses Harmony patches to override cooldown-related methods
- Sets all cooldown timers and delays to zero
- Ensures all CanSave checks always return true
- Implements multiple fallback mechanisms if primary save methods fail

## Requirements

- Game: Schedule I Free Sample by TVGS
- MelonLoader installed

## Notes

- Save functionality requires the game's internal save system to be available
- Button only appears in the main game scene
- Operates silently without cluttering the console with messages

## Contributing

Feel free to submit issues and pull requests. Please ensure you follow the existing code style and include appropriate tests.

## License

This project is licensed under a modified MIT License with additional conditions:
- You must provide appropriate credits to the original author (kua8)
- Modified versions must only be distributed through Nexus Mods
- Any modifications must clearly indicate they are derivatives of this original mod

See the LICENSE file for the full license terms. 