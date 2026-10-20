# Setup

This is required for installing the mod on a server:
- Install the V Rising Dedicated Server (find using the Steam Tools filter). For other servers, see below*.
- Install BepInEx for VRising, put items from that folder into `VRisingDedicatedServer` directory
- Download VampireCommandFramework. Put `VampireCommandFramework.dll` into the `BepInEx\plugins`
- Download this mod's `vrising_server_monitor.dll` into the same `BepInEx\plugins`
- Start the server and check that the mod loaded.
```
[Info   :   BepInEx] 2 plugins to load
[Info   :   BepInEx] Loading [VampireCommandFramework 0.11.0]
[Info   :   BepInEx] Loading [vrising_server_monitor ...]
[Info   :vrising_server_monitor] Plugin vrising_server_monitor version ... is loaded!
```

*To test a server on your own computer, launch V Rising and use the direct connect option with `127.0.0.1:9876`.

# Requirements
- .NET 6 SDK
# Build
- dotnet build
# References
- https://wiki.vrisingmods.com/dev/
- https://old.thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/
- https://old.thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/
- Built with help from Claude.