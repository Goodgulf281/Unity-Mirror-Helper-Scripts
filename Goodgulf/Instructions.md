# Instructions for the Part 5 unitypackage

## Basic steps

If you want to import the mirror-ui-standard-assets package into your project then please follow these steps:

1. Create a new project in Unity 2020LTS, import the Mirror package.
2. Import the [Unity starter asset](https://assetstore.unity.com/packages/essentials/starter-assets-third-person-character-controller-196526) and enable the backends when asked.
3. Import the package "mirror-ui-standard-assets.unitypackage" from my Github page.
4. Go to the StarterAssets>ThirdpersonController>Prefabs>PlayerArmature prefab in the starter asset folder and add the PlayerScript to it.
5. You’ll need to configure client authority on the network transform and animator components and link the prefab to the animator property.
6. Add the Offline, Room and Playground scenes to the build settings and you’re ready to go.


![Network Transform](https://github.com/Goodgulf281/Unity-Mirror-Helper-Scripts/blob/main/Goodgulf/mir5a.png)

![Network Animator](https://github.com/Goodgulf281/Unity-Mirror-Helper-Scripts/blob/main/Goodgulf/mir5b.png)



## Errors

If you open the Offline scene before completing step 4 then make sure to add the PlayerArmature prefab again as the Player Prefab in the NetworkRoomManager.

![Network Room Manager UI](https://github.com/Goodgulf281/Unity-Mirror-Helper-Scripts/blob/main/Goodgulf/mir5c.png)
