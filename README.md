# vrising-server-monitor

A server-side V Rising mod that adds new announcements for multiplayer.
1. Monitors castle storage and announces when a player takes items from storage in another player's castle.

This mod watches inventory movements, including: drag, shift_click, take all, and drag from storage onto ground. For each request, the mod finds the source container, follows its link to the castle heart that owns it, and compares the heart's owner with the player who made the request. If they differ, it broadcasts an announcement. Example announcements include:

```
Jocasta took 20x Bone from Marcel's storage
Jocasta took 98x Wood_Standard, 12x Bone from Marcel's storage
Jocasta dropped 20x Bone from Marcel's storage
```

When a player moves items in their own inventory or in/out of their own storage, it is ignored. Any placed container (materials chests, the vampire lockbox, shelves, and so on) is handled the same way.

2. First-time boss kills are advertised to all players. [Under Development]
3. An achievement system which broadcasts to all players. This sends congratulations when a rare item or drop is obtained for the first time. [Under Development]
4. A next-steps for level advancement quest appears for players who opt in. Designed for new players. [Under Development]

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

# Log appearance
Simple alerts appear in the in-game chat. More detailed logs appear in the BepInEx console with a `[ChestWatch]` prefix. Each event logs when it
is received and which system it came from, then either an `Announcing:` line or an `Exit:`
line naming the filter that stopped it, for example:

```
[ChestWatch] Take-all event received (MoveAllItemsBetweenInventoriesSystem)
[ChestWatch] Using external inventory entity 12345:1
[ChestWatch] Snapshot stored: 3 item type(s) in source (MoveAllItemsBetweenInventoriesSystem)
[ChestWatch] Announcing: Jocasta took 98x Wood_Standard, 12x Bone from Marcel
```

# Build
- .NET 6 SDK was used to build this mod.
- run `dotnet build` to build again from source. 
- The build copies the resulting DLL into the dedicated server's `BepInEx\plugins` folder
(see `CopyToServer` in the project file). Stop the server before building so the
copy is not blocked.
# References
- https://wiki.vrisingmods.com/dev/
- https://old.thunderstore.io/c/v-rising/p/BepInEx/BepInExPack_V_Rising/
- https://old.thunderstore.io/c/v-rising/p/deca/VampireCommandFramework/
- Built with help from Claude.