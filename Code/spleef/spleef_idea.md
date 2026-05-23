# Spleef Game

Idea: The platform is made up of individual tiles, the tiles can be broken and fall away. There are several layers of the floor. If you fall through all the floors to the zone below you are out. The goal is to be the last man standing.

Game modes: 
- Traditional Spleef: Walking over the tile has a small delay before it falls away
- Spleef attack: Players use a weapon/tool to knock away the tiles
- Pattern match mode: Players are prompted to stand on a specific type of tile, they have limited time to get on a tile matching that style, when time is up if they are standing on the wrong one, they fall and are out. 


Game Components:
- Player

- Platform (composed of multiple tiles in various patterns)
    - Tiles
        - Health
        - OnPlayerEnter -> TimeUntil to cause it to break/fall
        - Wobble effect before falling
        - BreakTile()
            - Tile falls, collision off, TimeUntil -> Destroy obj

- KillZone
    - Area where player falls into

- Scoreboard / Leaderboard

Scenes:
- Lobby
- Game level (could have multiple variations)
