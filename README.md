<p align="center">
	“Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.”
</p>

<h3 align="center">
    This mod is a work in progress, and while it may be stable, I make no guarantee that it will remain stable or will not cause damage to save files if updated. Use at your own risk, and thank you for checking it out.
</h3>

# Description
* A submod of Mechanical Humanlikes that completely changes how drones work to make them more interesting mechanically and intrinsically.

## Features
* Any race that is a drone by MHC's settings may opt-in to the reprogramming mechanic via a def mod extension.
* Reprogrammable drones start with no work types, skills, or directives enabled, and must be reprogrammed via surgery in order to begin contributing to the colony.
* Enabling work types, skills, and directives cost complexity, and the unit's energy and maintenance efficiency is directly affected by the relationship between current and maximum complexity.
* Implants and effects can offset complexity to allow for more powerful drones.
* Directives are unique programs that accentuate the drone's characteristics in special ways, often providing effects that fill a particular niche.
* Programmable drones belonging to other factions will have random programming, depending on their pawn kind, faction, and what kind of group (ie. traders) they are a part of.
* Programmable drones retain all normal features of drones, including moodlessness.

## For Modders
* All features of this mod are opt-in, and dependent upon MHC's configurations for Drones.
* To set a drone race to be programmable, give the race's ThingDef the MDR_ProgrammableDroneExtension. All nodes are optional, but are explained here: [Pawn extension](https://github.com/RWDevathon/MH-Drones-Reprogrammed/blob/main/Source/v1.4/Extensions/PawnExtensions.cs).

Example:
```xml
<AlienRace.ThingDef_AlienRace>
    <defName>AuthorPrefix_DroneRaceDefName</defName>
    <label>Example drone race</label>
    <!-- ... -->
    <modExtensions>
        <li Class="MechHumanlikes.MHC_MechanicalPawnExtension">
            <!-- Having this extension is not strictly necessary, but you should have one so that the normal drone mechanics are configurable and set -->
            <canBeDrone>true</canBeTrue> <!-- Defaults to true, even on races that do not have this extension, but is essential. -->
            <!-- ... -->
        </li>
        <li Class="MechHumanlikes.MDR_ProgrammableDroneExtension" MayRequire="Killathon.MechanicalHumanlikes.MechDronesReprogrammed">
            <!-- Having this extension is how drones opt-in to being programmable. There are no mandatory options. -->
            <!-- ... -->
        </li>
    </modExtensions>
</AlienRace.ThingDef_AlienRace>
```

* Work Types may have different complexity costs to enable and a minimum complexity requirement. If you want to apply custom costs to your custom work type, give it MDR_WorkTypeExtension. Options are explained here: [Work Type extension](https://github.com/RWDevathon/MH-Drones-Reprogrammed/blob/main/Source/v1.4/Extensions/WorkTypeExtensions.cs).
* If you want to create a patch for another mod's work types, see here for examples: [Work Type extension patches](https://github.com/RWDevathon/MH-Drones-Reprogrammed/blob/main/1.4/Patches/MDR_WorkTypeExtensionPatch.xml).

* Other factions' drones are controlled largely by existing pawn kind features, such as set skill ranges and required work types. If you would like to add required directives to particular pawn kinds, or control other options, add MDR_ProgrammableDroneKindExtension. Options are explained here: [Pawn kind extension](https://github.com/RWDevathon/MH-Drones-Reprogrammed/blob/main/Source/v1.4/Extensions/PawnKindExtensions.cs).

Example:
```xml
<PawnKindDef ParentName="ATR_AndroidCollectivePawnKindBase">
    <defName>AuthorPrefix_DronePawnKindDefName</defName>
    <label>Example drone pawn kind</label>
    <!-- ... -->
    <modExtensions>
        <li Class="MechHumanlikes.MDR_ProgrammableDroneKindExtension" MayRequire="Killathon.MechanicalHumanlikes.MechDronesReprogrammed">
            <!-- This extension forces the ranged directive for this pawn kind, and allows 1-2 additional directives and 5-20 complexity to be randomized. -->
            <requiredDirectives>
                <li>MDR_DirectiveRangeOptimized</li>
            </requiredDirectives>
            <discretionaryDirectives>1~2</discretionaryDirectives>
            <discretionaryComplexity>5~20</discretionaryComplexity>
        </li>
    </modExtensions>
</PawnKindDef>
```

* Creating your own directives can be done by creating a new DirectiveDef. They work relatively similarly to a GeneDef, but are generally designed to be as lightweight as possible. They have no graphic hooks built in, but do have hooks for associating with abilities and hediffs, so you can use them to indirectly add graphic effects to pawns. For all options that DirectiveDefs have with explanation, see here: [Directive Def](https://github.com/RWDevathon/MH-Drones-Reprogrammed/blob/main/Source/v1.4/Defs/DirectiveDef.cs)
* For examples of (simple) directives, see here: [Combat directive defs](https://github.com/RWDevathon/MH-Drones-Reprogrammed/blob/main/1.4/Defs/DirectiveDefs/Directives_Combat.xml).

* Directive Defs may have requirement workers that set rules on what drones they may be applied to. By default, there are no restrictions, but you can configure as specific and as many requirement workers as you would like by creating a DirectiveRequirementWorker and adding into the DirectiveDef's requirementWorkers list.
* For examples of requirement workers and explanations on how they work, see here: [Directive Requirement Workers](https://github.com/RWDevathon/MH-Drones-Reprogrammed/tree/main/Source/v1.4/Directives/DirectiveRequirementWorkers)


## Known Issues / Incompatibilities
* None so far, but they're out there, I know it.

## Links
[Discord](https://discord.gg/udNCpbkABT)

[GitHub](https://github.com/RWDevathon/MH-Drones-Reprogrammed/tree/main)