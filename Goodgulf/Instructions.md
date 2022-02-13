# Instructions for the Part 5 unitypackage

## Basic steps

If you want to import the mirror-ui-standard-assets package into your project then please follow these steps:

1. Create a new project in Unity 2020LTS, import the Mirror package.
2. Import the Unity starter asset and enable the backends when asked.
3. Import the package from my Github page, you can find the link in the comments section.
4. Go to the PlayerArmature prefab in the starter asset folder and add the Player script.
5. You’ll need to configure client authority on the network transform and animator components and link the prefab to the animator property.
6. Add the Offline, Room and Playground scenes to the build settings and you’re ready to go.

## Errors

If you open the Offline scene before completing step 4 then make sure to add the PlayerArmature prefab again as the Player Prefab in the NetworkRoomManager.