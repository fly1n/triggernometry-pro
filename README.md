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

4.Reduce update interval of combatant stats to 10ms (from original 1000ms), Enhance the real-time performance of memory data. But it is still limited by the memory update interval of ffxiv_act_plugin, which is 100ms.




Triggernometry has a Wiki, containing useful information and documentation:

https://github.com/paissaheavyindustries/Triggernometry/wiki

To get answers to some commonly asked questions, and to get more information on how to use Triggernometry, you can find more information in the Triggernometry FAQ and examples section:

https://github.com/paissaheavyindustries/Triggernometry/wiki/Triggernometry-FAQ-and-examples

## Discord

Triggernometry also has a publicly available Discord server for announcements, suggestions, and questions related to the plugin. Feel free to join at:

https://discord.gg/6f9MY55
