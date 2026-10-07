# Svimma

Swimming stops putting your weapon away. A sword, a pickaxe or a fishing rod stays in your
hand in deep water, you can take one out there, and a swing costs a bit more because the
water does not give your stamina back.

*Svimma* is Old Norse for to swim.

## Why

This is deliberately narrower than "use anything anywhere", and that is the design, not a
first draft of it.

Vanilla treats the water as a place where you cannot hold anything. Every frame you are
swimming with no ground under you, the game puts your hand items away, and it refuses to
equip one while you are in that state. Nothing else stops you swinging: the attack code never
asks whether you are swimming, which is why you can already punch in the water. So the whole
block is two checks, plus a third on the hide key that stops you drawing again. Svimma lifts
those three for the hand items you have allowed, and changes nothing else.

The obvious alternative was to make swimming transparent to combat: every tool, every weapon,
no cost. It was rejected for three reasons, each of which is a setting.

- **Ranged weapons and magic stay put away.** A bow or a staff from the water lets you fight
  anything on the shore with no cost for being out of reach, which is a far bigger change
  than swinging a sword at something that has come to the water. Bows, crossbows and staffs
  are three separate switches, all off.
- **Building tools stay put away.** The hammer, the hoe and the cultivator place pieces and
  reshape ground from where you could not have stood to do it. One switch, off.
- **A swing costs 1.5 times as much stamina.** Stamina does not come back while you swim, so
  a swing in the water is paid for out of a bar that stays spent until you leave. Wading is
  not swimming: standing on the bottom, even up to the neck, costs the ordinary price. The
  multiplier is a setting, from 1 (free) to 5.

Blocking is off in deep water as well, because you cannot plant your feet in it. A shield in
your other hand is carried and does not block until you switch that on.

## What it does

- **Melee weapons, pickaxes and fishing rods stay in hand while you swim,** and equip while
  you swim. Swords, knives, clubs, spears, polearms and axes count as melee. Each of melee,
  pickaxes and fishing is its own switch, on by default.
- **Torches are still put away.** A flame in the water is not something this changes, and
  there is no switch for it.
- **Armour, food and everything that is not a hand item are still refused in the water.**
  Only hand items are in scope.
- **The hide key works again in deep water,** so a weapon you put away can be drawn without
  swimming back to shore.
- **A deep swing costs more.** The raised price is also the price you must be able to pay: a
  swing you cannot afford is refused, as on land, and an empty bar while swimming is drowning.
- Nothing is registered, spawned, saved or written to a world.

## Installing

Needs BepInEx. Nothing else. Through a mod manager it is one install. By hand, put
`Svimma.dll` in `BepInEx/plugins/Svimma/`.

Then start the game once and quit. That first run writes the config file. It does not exist
before the mod has loaded, which is the usual reason people think it is broken.

## Settings

The file is `BepInEx/config/ezomic.valheim.svimma.cfg`. Open it in any text editor. Every
setting has a comment above it, so the file explains itself. They are the host's rules on a
server (see below) except `Verbose`, which is yours.

Note that changing a default in a new version does nothing on a machine that has already run
the mod. BepInEx writes every entry on first run and the saved value wins.

## Multiplayer

**The host needs it.** Clients without it are let in and are unaffected. They simply cannot
swing in deep water, and what a player with it does is seen through the equipment and
animation every client already shows.

The reason it is not "everyone": Svimma registers no prefab, changes no item data and writes
nothing of its own, so a client without it cannot fail to resolve anything. The host's
settings apply to everyone connected to it, in memory only, and your own config file is
never written to and comes back the moment you disconnect. Without
[Core](https://github.com/Ezomic/valheim-core) the mod still runs; what is lost is the host
being able to impose its rules.

## What has not been checked

Built, not yet run in game, and never on a server with other players. Two things only the running game can answer: whether the swing
animation plays sensibly while you are swimming, and whether a blow reaches something in or
under the surface. The attack code puts no condition on either, but the animator has its own
swim state and its transitions are not in the code. A Devkit scenario covers the equip, the
hand items staying out and the price (`scenarios/svimma-deep-water-keeps-the-weapon.txt`), and
lists the checks that need a person.

## Bugs and ideas

Both go to the site. [longhouse.thijssensoftware.nl/bugs](https://longhouse.thijssensoftware.nl/bugs)
is for anything broken, and [longhouse.thijssensoftware.nl/ideas](https://longhouse.thijssensoftware.nl/ideas)
is for what a mod should do next. You can vote on other people's ideas there as well.

Signing in takes a Steam or Discord account. I work from that list, so the votes decide what
I pick up next.

## Licence

MIT. See `LICENSE`.
