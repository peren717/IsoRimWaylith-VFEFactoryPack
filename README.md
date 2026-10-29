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

All belt pieces share one height, **0.22 cells** (`Pack.BeltTop`): items ride at it and enter or leave machines at it.
Every belt model's top and `surfaceHeight` is 0.22; the hopper's bin floor and spout and the underground ends' mouths meet
it; factory models put the bottom of every input or output opening at 0.22, so items never step up or down. Notes and the
review sheets: `model-review/vfefactory_b1/analysis.md`.

Vanilla Furniture Expanded - Factory is made by the Vanilla Expanded team (Oskar Potocki and others). The models are new, made after VFE's art; this pack contains no files of the original mod.

MIT License.
