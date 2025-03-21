# OneHandedRumble

RUMBLE mod that adds an accessibility option for one-handed players.

## How to install the mod
1. Install MelonLoader
2. Run Game
3. Drop Folders into Rumble Folder
4. Play Game

## How it works
When playing in one-handed mode, one of the controllers isn't tracked at all, and the corresponding hand mirrors the real one instead. At any point, you can press the trigger on either controller to stop the mirroring, and the simulated hand will be frozen in place. You can use this to do all asymmetrical moves such as straight or disc (any move at all, really), by first reaching one hand position, pressing the trigger, then reaching the second hand position. Sprinting is also possible with the right trigger timing.

## Help, I can't get through the T-pose loader scene!
By default, the mod does not change the game at all, it starts in normal two-handed mode. You need to change the options to disable one of the hands. Since ModUI can only be opened past the loader scene, you can also edit the provided settings file before starting the game.

## T-pose measurement
The two measurement buttons are replaced by only one of the two when the game runs in one-handed mode (the instruction statues are updated accordingly). This works both in the gym and in the loader scene.

## Configuration options
- **Left enabled**: whether or not to track the left controller
- **Right enabled**: whether or not to track the right controller
- **Use Mute on enabled controller**: if true, then the active controller will have the "mute" action on the primary button (default is push-to-talk)
- **Use Turn on enabled controller**: if true, then the active controller will have the "turn" action on the joystick (default is move)
- **Keep haptics**: if true, the haptic signals will be kept active on the disabled controller (no haptics by default).

Notice that the second controller is never *completely* disabled. It is possible to play without a second controller at all of course (you can even remove the batteries if you wish), but it can be nice to still have it for secondary actions such as mute and turn. This is intended for uses where the disabled hand can hold the controller despite not being able to do poses.

## It doesn't work in matches?
The mod automatically switches to normal two-handed mode when starting a match. Matches in RUMBLE give you two currencies, battle points and gear coins, are these off-limits for gameplay-altering mods. This rule ensures a fair multiplayer experience for all.

You can still enjoy the mod in your gym and in parks however! So you can still have one-handed matches, but without rank progression.

## Help And Other Resources
Get help and find other resources in the Modding Discord:
https://discord.gg/fsbcnZgzfa
