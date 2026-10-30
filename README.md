# IsoRim Waylith - VFE Factory Pack

[Vanilla Furniture Expanded - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3686924415) in the 3D view of [IsoRim Waylith](https://github.com/peren717/IsoRim-1.7) and on the floors of [IsoRimZ](https://github.com/peren717/IsoRim-ProjectZ) (RimWorld 1.7).

- 3D conveyor belts: straight belts, turns, mergers, splitters, filters, refuelling ports and underground belts
- 3D factory hoppers that show input and output like VFE's
- Items ride on the belts, with their stack counts and flow arrows on top
- With IsoRimZ: drill platforms stay on the ground; autofarmers work only on their own floor

Requires IsoRim Waylith (latest version), Vanilla Furniture Expanded - Factory and Vanilla Expanded Framework. IsoRimZ is optional. Load after all of them. With an older IsoRim Waylith (before Api 39) the pack still loads, without the conveyor models.

让 Vanilla Furniture Expanded - Factory 在 IsoRim Waylith 的 3D 视角和 IsoRimZ 的楼层上正常工作。

- 3D 传送带：直带、转弯、合流、分流、过滤器、加料口和地下传送带
- 3D 工厂料斗，和 VFE 一样显示输入和输出
- 物品在传送带上移动，数量标签和流向箭头显示在上方
- 配合 IsoRimZ：自动钻井平台只能建在地面；自动农场只在自己所在的楼层工作

需要最新版 IsoRim Waylith、Vanilla Furniture Expanded - Factory 和 Vanilla Expanded Framework。IsoRimZ 可选。请放在它们之后加载。IsoRim Waylith 版本较旧（Api 39 之前）时也能加载，只是没有传送带模型。

## For model makers: the belt height rule

All belt pieces share one height, **0.22 cells** (`Pack.BeltTop`): items ride at it and enter or leave hoppers at it.
Every belt model's top and `surfaceHeight` is 0.22; the hopper's bin floor and spout and the underground ends' mouths meet
it; factory machines use the machine port rule below. Notes and the review sheets: `model-review/vfefactory_b1/`.

## For model makers: the machine port rule

Factory machines (batch B2 on) put the bottom edge of every input and output port at **0.45 cells** (`MACHINE_PORT`), a
bit above the factory hopper's top (0.40), on every machine alike: items drop from a machine port into the hopper below it
and are lifted from a hopper into a machine at one height. Belt pieces, the hoppers' belt-side spout and bin floor and the
underground mouths keep the belt top 0.22. VFE attaches hoppers by cell: a hopper on the machine's interaction cell is its
output, a hopper on one of the running process's input slots is an input; each model has its ports where VFE's art draws
them. Machines may be taller than a storey (no `wallFit`): IsoRim reads their real height, and with IsoRimZ no floor can be
built over the tall ones (drill platform, distillery), which stand in open double-height rooms. Notes and review sheets:
`model-review/vfefactory_b2/` and the header of `tools/make_vfefactory_b2.py`.

## For model makers: the 3x4 and 3x5 factories (batch B3)

Smelter, biofuel refinery and ammunition press (3x4) and conveyor oven, autoloom and automated cannery (3x5; the cannery
only with Vanilla Cooking Expanded, its ModelDef has `MayRequire` and its gate runs in `PatchOperationFindMod`). For the
even 3x4 size the model's origin is the footprint centre, half a cell north of the thing's Position in its own frame. Port
cells in the model frame: output always south (z -2.5 on 3x4, -3 on 3x5); inputs north at x -1, 0, 1 (smelter, refinery,
oven), at x -1 and 1 (ammunition press), and west and east of the north row (z 2, autoloom and cannery, whose input hoods
open sideways). The smelter (2.08) and the refinery (1.84) are taller than a storey. Lit parts (the smelter's slot ring, the
oven's tunnel) are IsoRim `beam_` light faces over a dark-orange base, so they keep their brightness at night; a machine
without power is in the pack's model state `vfef_unpowered` and its ModelDef's `byState` draws the `_unlit` model without
them. Parts, sizes and review sheets:
`model-review/vfefactory_b3/` and the header of `tools/make_vfefactory_b3.py`.

## For model makers: the 5x5 and 5x3 factories (batch B4)

Automated assembler, alloy forge and machining bay (5x5) and neutroamine synthesizer and medicine granulator (5x3). Both
sizes are odd, so the model's origin is the thing's Position. Port cells in the model frame: output always south (z -3 on
5x5, -2 on 5x3); inputs north at x -1 and 1 plus west and east of the middle (z 0, assembler), north at x -2, 0, 2 (alloy
forge), north at x 0 plus west and east (machining bay), north at x -1 and 1 (neutroamine) and at x -1, 0, 1 (medicine). The
alloy forge (1.79, its handwheel) is taller than a storey. Its lit parts (coil windows, the tank hatch, the crucible floor,
the output strip) use the B3 glow method (`beam_` faces, `_unlit` model in the state `vfef_unpowered`), and so does the B2
crematorium's furnace mouth since this batch. Where VFE's south, east and west art keeps the tanks, vats or bowl at the back of
the picture, the models follow the north art. Parts, sizes and review sheets: `model-review/vfefactory_b4/` and the header
of `tools/make_vfefactory_b4.py`.

Vanilla Furniture Expanded - Factory is made by the Vanilla Expanded team (Oskar Potocki and others). The models are new, made after VFE's art; this pack contains no files of the original mod.

MIT License.
