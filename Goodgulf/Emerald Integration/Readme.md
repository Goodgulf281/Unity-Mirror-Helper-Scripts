# Instructions for integrating A Star Pathfinding Project into Emerald 2024

## Pre-requisites

* Import Emerald 2024 into your Unity project.
* Import AStar Pathfining Project into your Unity project.


## Patch the files

Copy the following files from the project to a temporary folder:
* EmeraldSystem.cs
* EmeraldMovement.cs
* EmeraldDebugger.cs

Then download the diff files for the right version of Emerald from this Github repo.
If you are running Unity on a Windows machine you can get diff/patch binaries easily (use Google).
Instructions on how to patch the files can be found here [Patch and Diff](https://www.pair.com/support/kb/paircloud-diff-and-patch/) or just Google it.

Patch the three above list files and copy them back into your project. Also include my NavMeshAgentImposter.cs.

Create an empty game object in your scene and add the AStarPathfinding component. Add a grid graph and set it up properly.

Add an ASTAR scripting define symbol to the project settings and all should work fine.


## Important notes

I have updated the files for two versions of Emerald 2024. These diff files are pretty much readable so it should be possible to patch future versions manually.
I will not be keeping track of updates to Emerald 2024 and update these regularly. I have sent a message to the devs from Black Horizon Studios and offered the integration for free.
If they include it in their code it will be much easier to keep it up to date. To date they have not responded or shown any interest. 