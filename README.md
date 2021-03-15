## About this branch
Modified by NiniTechnology. Some additional features and functions will be added in the future to optimize the user experience.

1.Add a new Trigger event source "Original log lines", which allow triggers to access raw log lines. Raw log lines contains more information and they can be useful when we need to test our triggers. They often have information like this: 

```
251|2019-05-21T19:11:02.0268703-07:00|ProcessTCPInfo: New connection detected for Process [2644]:192.168.1.70:49413=>204.2.229.85:55021|909171c500bed915f8d79fc04d3589fa
```
for more information about this:
https://github.com/quisquous/cactbot/blob/main/docs/LogGuide.md#fb-debug

2.Add more math functions.
```
X8float = converts a base16 (hex) 4-bytes array to float types.
X4pos = converts a base16 (hex) uint16 types to a ffxiv ingame-position expression as follows: pos = (uint16 - 32767)/32.767, this is used in ActorCast type network packet.
```

3.Reduce update interval of combatant stats to 10ms (from original 1000ms), Enhance the real-time performance of memory data. But it is still limited by the memory update interval of ffxiv_act_plugin, which is 100ms.

4.Modify ExportActiveEncounter and ExportLastEncounter function. Now we have encounter information in the selected miniparse format instead of default format.
```
_lastencounter = ACT DPS information from the last encounter in selected miniparse format
_activeencounter = ACT DPS information from the ongoing encounter in selected miniparse format
```

Triggernometry has a Wiki, containing useful information and documentation:

https://github.com/paissaheavyindustries/Triggernometry/wiki

5.Add more Properties available in _ffxivparty and _ffxiventity:
```
name = Name of the actor
job = Current job as a three letter acronym (AST, CUL, MIN, SMN, etc)
jobid = Numeric (internal ingame) representation of the current job
currenthp, currentmp, currentcp, currentgp = Current HP, MP, CP, and GP
maxhp, maxmp, maxcp, maxgp = Maximum HP, MP, CP, and GP
level = Level of the actor
x, y, and z = Position information of the actor
inparty = Party membership flag (0 = character doesn't exist or is not in party, 1 = character exists and is in the party)
id = Hexadecimal ID of the actor
order = Order number of the actor inside the _ffxivparty structure (this property is 0 for _ffxiventity)
worldid = Numeric ID of the home world of the character
worldname = Name of the home world of the character
currentworldid = Numeric ID of the current world of the character
heading = Heading of the entity in radians (South 0, North ±PI)
targetid = Hexadecimal ID of the actor currently selected as target (0 is there is no target selected)
casttargetid = Hexadecimal ID of the actor the current cast is targetting (0 is there is no cast in progress)
distance = Distance from the main combatant (you) to the entity
role = Role of the actor ("Tank", "Healer", "DPS", "Crafter", "Gatherer", or blank if not available)

ownerid
type
iscasting = (0 = character is not casting, 1 = character is casting)
castbuffid
castdurationcurrent
castdurationmax
bnpcnameid
bnpcid
pointer = Hexadecimal pointer address of character data in memory.
```

To get answers to some commonly asked questions, and to get more information on how to use Triggernometry, you can find more information in the Triggernometry FAQ and examples section:

https://github.com/paissaheavyindustries/Triggernometry/wiki/Triggernometry-FAQ-and-examples

## Discord

Triggernometry also has a publicly available Discord server for announcements, suggestions, and questions related to the plugin. Feel free to join at:

https://discord.gg/6f9MY55
