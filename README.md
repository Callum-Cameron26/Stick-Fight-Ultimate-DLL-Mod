# Stick Fight Ultimate

A mod for Stick Fight: The Game providing various gameplay enhancements and utilities.

## Features

### Combat
- God Mode - Complete invincibility
- Infinite Ammo - Unlimited ammunition for all weapons  
- No Cooldown - Attack without cooldown delays

### Movement
- Fly Mode - Enable flight controls
- Click Teleport - Right-click to teleport anywhere
- No Clip - Walk through walls and objects

### Utilities
- Instant Win - Automatically win rounds
- Kill All Enemies - Eliminate all opponents at once
- Weapon Spawning - Spawn random, specific, or all weapons

### Interface
- Custom GUI Menu - Tabbed interface with clean styling
- Toggle Notifications - Show/hide activation messages
- Keybind Support - F1 for menu, F2 for god mode

## Installation

1. Install MelonLoader for Stick Fight: The Game
2. Place StickFightUltimate.dll in the Mods folder
3. Launch the game and press F1 to open the menu

## Dependencies

The following DLLs are required and should be placed in the Dependencies folder:

- 0Harmony.dll - Harmony patching framework
- MelonLoader.dll - Mod loader framework
- Assembly-CSharp.dll - Game assembly
- Assembly-CSharp-firstpass.dll - Game framework assembly
- UnityEngine.dll - Unity engine core
- UnityEngine.UI.dll - Unity UI system

## Controls

- F1 - Toggle menu
- F2 - Toggle god mode
- Right-click - Teleport (when enabled)
- WASD - Movement controls

## Technical Details

### Harmony Patches
- HealthHandler.TakeDamage() - God mode implementation
- HealthHandler.Die() - Death prevention
- Weapon.ActuallyShoot() - Infinite ammo handling
- Fighting.Attack() - No cooldown functionality
- Fighting.ThrowWeapon() - Weapon throwing prevention
- Fighting.NetworkThrowWeapon() - Network weapon handling

### Build Requirements
- .NET Framework 3.5
- MelonLoader 0.5+
- Harmony 2.x+

## Notes

- Designed for single-player and private use
- Use responsibly in multiplayer environments
- Some features may not work in all game modes
- Always backup game files before modding

## License

This mod is provided as-is for educational and entertainment purposes.
Use at your own risk.
