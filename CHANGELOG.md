# Changelog

## 1.0.0 - 2026-10-08

First release, a member of Longhouse 2.4.0. Swimming with no ground under you no longer puts your hand items away, and you
can equip them there. Melee weapons, pickaxes and fishing rods are allowed, bows, crossbows,
staffs and building tools are not, and each of those is a switch in the config.

Vanilla blocked this in three places, all of them one call to `IsSwimming` inside a longer
condition: `Humanoid.UpdateEquipment` hid your hands every frame, `Humanoid.EquipItem` refused
to equip anything, and the hide key in `Player.Update` would not draw your items again while
you swam. Nothing else stops an attack in the water, so those three are the whole change.
Torches are still put away, and so is everything that is not a hand item.

A swing in deep water costs 1.5 times the stamina, set by `DeepSwingStaminaMultiplier`, because
stamina does not regenerate while you swim. Blocking is off in deep water unless `AllowBlocking`
is on. Wading, standing on the bottom with the water at your chest, is not deep water and is
untouched.

Built and compiled, not yet run in game, alone or on a server. Whether the swing animation looks right while swimming
and whether a blow reaches something in or under the water are only answerable there. A Devkit
scenario (`scenarios/svimma-deep-water-keeps-the-weapon.txt`) checks the equip, the hand items
staying out and the price, and lists the manual checks.
