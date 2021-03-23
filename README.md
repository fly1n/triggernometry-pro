# About this branch
Modified by NiniTechnology. Some additional features and functions will be added in the future to optimize the user experience.

## Original log lines as trigger event source
Add a new Trigger event source "Original log lines", which allow triggers to access raw log lines. Raw log lines contains more information and they can be useful when we look for the network data format of unknown mechanism. They often have information like this: 
```
251|2019-05-21T19:11:02.0268703-07:00|ProcessTCPInfo: New connection detected for Process [2644]:192.168.1.70:49413=>204.2.229.85:55021|909171c500bed915f8d79fc04d3589fa
```
To use the original log lines as event source, you need to check **(DEBUG) Enable Debug Options** and **(DEBUG) Log all Network Packets** in **ACT Plugins->FFXIV Settings page**. It can cause performance problems and write a huge amount of information in the log file.
for more information about this:
https://github.com/quisquous/cactbot/blob/main/docs/LogGuide.md#fb-debug

## Log message reparsed as ACT loglines
Add an option **"Reparse as ACT log line"** in Log Message Action. If you select this option, the specified log message will be re-parsed into an ACT log lines and can be accessed through the ACT encounter logs window. These logs will also be processed by all plugins like other parsed logs.

## Additional math functions
```
X8float = converts a base16 (hex) 4-bytes array to float types.
X4pos = converts a base16 (hex) uint16 types to a ffxiv ingame-position expression as follows: pos = (uint16 - 32767)/32.767, this is used in ActorCast type network packet.
X4Heading = converts a base16 (hex) uint16 types to a ffxiv ingame-heading expression as follows: pos = (uint16 - 32767)/32767*PI, this is used in some network packet.
max(x1, x2, x3, ...) = the largest of any number of variables
min(x1, x2, x3, ...) = the smallest of any number of variables
distanceToLine(x0, y0, x1, y1, x2, y2) = Find the shortest distance from (x, y) to the straight line connecting two other points.
direction(x0, y0, x1, y1) = Direction from (x0, y0) to (x1, y1) in radians (South 0, North ±PI)
dirToClock(dir) = converts a direction value in radians to an integer between 0-7. (North = 0, +1 every 45° clockwise). 
dirToClock(dir, number) = converts a direction value in radians to an integer between 0-(number-1). (North = 0, +1 every 360°/number clockwise). 
dirToClock(dir, number, offset) = converts a direction value in radians to an integer between 0-(number-1). (North with offset in radians = 0, +1 every 360°/number clockwise)). 
orderByDistance(index, x0, y0, x1, y1, x2, y2, ...) = Sort the points in the list [(x1,y1),(x2,y2),(x3,y3), ...] according to the distance to (x0, y0), and return the sorted position of the point with the specified ID in the new order.
orderByDistanceToLine(index, x_line0, y_line0, x_line1, y_line1, x1, y1, x2, y2, ...) = Sort the points in the list [(x1,y1),(x2,y2),(x3,y3), ...] according to the distance to the straight line connecting (x_line0, y_line0)->(x_line1, y_line1), and return the sorted position of the point with the specified ID in the new order.
```

## Reduction of combatant states update interval
Reduce update interval of combatant stats to 10ms (from original 1000ms), Enhance the real-time performance of memory data. But it is still limited by the memory update interval of ffxiv_act_plugin, which is 100ms.

## Customizable encounter format
Modify ExportActiveEncounter and ExportLastEncounter function. Now we have encounter exported in the selected miniparse window format instead of default format. 
The encounter export data contains a lot of interesting information, mainly related to the player's damage and healing statistics. It's customizable through **ACT Options->Output Display->Mini Parse Window->Mini-Parse Text Formatting**.
```
_lastencounter = ACT DPS information from the last encounter in selected miniparse format
_activeencounter = ACT DPS information from the ongoing encounter in selected miniparse format
```
## Automatic code completion for expressions
Automatic code completion function for regular expressions, mathematics, and string expressions.
Press the arrow and space, or use the mouse to select the code hint to complete the expression.
This is awesome!

## More combatant properties available
Add more Properties available in *_ffxivparty* and *_ffxiventity*:
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

## Direct Memory Reading Function
Add Combatant Memory Reading Function. This is a dangerous function which can read all the memory information of a certain combatant. Starting from the memory pointer, read every 4 Bytes as a unit. The result is returned as a string value. 
```
_ffxiventity[1234ABCD].memory[160,4] = Begin at memory offset 160 from combatant pointer address, and read 4 bytes. According to the data structure listed below, we can know that the return value represents a posX value of the entity in float type.
${numeric:round(X8float(${_ffxiventity[1234ABCD].memory[160,4]}),4)} = Through the combination of these functions, the posX coordinates of the character can be obtained, parsed as a float type, and 4 decimal places are retained.
```
Add another Direct Memory Reading Function.
```
 _ffxivmemory[010012341234ABCD,100,64] = Begin at memory address 0x010012341234ABCD, move forward at an offset of 100 bytes, and read 64 bytes.
```
All Memory function will return 4byte Hexes as groups, seperate by a space char. For an example:
```
1234ABCD 2234ABCD 3234ABCD 
```

Direct memory reading is the fastest way to access memory data of a combatant or an address. Each time this expression is evaluated, the latest memory data will be returned immediately. Please be careful not to read the memory at too high a frequency, which may cause performance problems. On the other hand, direct memory reading will also allow you to access some unclassified information, which may help your research on game mechanics.
The memory structure of a combatant is listed below as a reference. This code is copied from *FFXIV_ACT_Plugin.Memory.Models.Combatant64Struct*.
```
public struct Combatant64Struct
{
    // Fields
    [FixedBuffer(typeof(byte), 0x40), FieldOffset(0x30)]
    public <Name>e__FixedBuffer Name;
    [FieldOffset(0x74)]
    public uint ID;
    [FieldOffset(0x80)]
    public uint BNpcID;
    [FieldOffset(0x84)]
    public uint OwnerID;
    [FieldOffset(140)]
    public byte Type;
    [FieldOffset(0x92)]
    public byte EffectiveDistance;
    [FieldOffset(160)]
    public float PosX;
    [FieldOffset(0xa4)]
    public float PosZ;
    [FieldOffset(0xa8)]
    public float PosY;
    [FieldOffset(0xb0)]
    public float Heading;
    [FieldOffset(560)]
    public uint PCTargetID;
    [FieldOffset(0x18b0)]
    public uint NPCTargetID;
    [FieldOffset(0x1920)]
    public uint BNpcNameID;
    [FieldOffset(0x193c)]
    public ushort CurrentWorldID;
    [FieldOffset(0x193e)]
    public ushort HomeWorldID;
    [FieldOffset(0x1c4)]
    public uint CurrentHP;
    [FieldOffset(0x1c8)]
    public uint MaxHP;
    [FieldOffset(460)]
    public uint CurrentMP;
    [FieldOffset(0x1d4)]
    public ushort CurrentGP;
    [FieldOffset(470)]
    public ushort MaxGP;
    [FieldOffset(0x1d8)]
    public ushort CurrentCP;
    [FieldOffset(0x1da)]
    public ushort MaxCP;
    [FieldOffset(0x1e2)]
    public byte Job;
    [FieldOffset(0x1e3)]
    public byte Level;
    [FixedBuffer(typeof(byte), 540), FieldOffset(0x19d8)]
    public <Effects>e__FixedBuffer Effects;
    [FieldOffset(0x1b60)]
    public byte IsCasting1;
    [FieldOffset(0x1b62)]
    public byte IsCasting2;
    [FieldOffset(0x1b64)]
    public uint CastBuffID;
    [FieldOffset(0x1b70)]
    public uint CastTargetID;
    [FieldOffset(0x1b94)]
    public float CastDurationCurrent;
    [FieldOffset(0x1b98)]
    public float CastDurationMax;
    [FieldOffset(0x1ba0)]
    public OutgoingAbilityStruct OutgoingAbility;
    [FixedBuffer(typeof(byte), 0xe10), FieldOffset(0x1cd0)]
    public <IncomingAbilities>e__FixedBuffer IncomingAbilities;

    // Nested Types
    [StructLayout(LayoutKind.Sequential, Size=540), CompilerGenerated, UnsafeValueType]
    public struct <Effects>e__FixedBuffer
    {
        public byte FixedElementField;
    }

    [StructLayout(LayoutKind.Sequential, Size=0xe10), CompilerGenerated, UnsafeValueType]
    public struct <IncomingAbilities>e__FixedBuffer
    {
        public byte FixedElementField;
    }

    [StructLayout(LayoutKind.Sequential, Size=0x40), CompilerGenerated, UnsafeValueType]
    public struct <Name>e__FixedBuffer
    {
        public byte FixedElementField;
    }
}

```

# Original Triggernometry Readme
Triggernometry has a Wiki, containing useful information and documentation:

https://github.com/paissaheavyindustries/Triggernometry/wiki
To get answers to some commonly asked questions, and to get more information on how to use Triggernometry, you can find more information in the Triggernometry FAQ and examples section:

https://github.com/paissaheavyindustries/Triggernometry/wiki/Triggernometry-FAQ-and-examples

## Discord
Triggernometry also has a publicly available Discord server for announcements, suggestions, and questions related to the plugin. Feel free to join at:

https://discord.gg/6f9MY55
