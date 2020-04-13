# Machine Common Sense Fork of AI2-THOR

Original documentation:  https://github.com/allenai/ai2thor

## Setup

### Editor

The AI2-THOR documetation wants us to use Unity Editor version `2018.3.6` but that version was not available for the Linux Unity Editor.  However, version [`2018.3.0f2`](https://forum.unity.com/threads/unity-on-linux-release-notes-and-known-issues.350256/page-2#post-4009651) does work fine.  Do NOT use any version that starts with `2019` because its build files are not compatible with AI2-THOR.

### Assets

Checkout the [MCS private GitHub repository](https://github.com/NextCenturyCorporation/mcs-private) and copy the files from its `UnityAssetStore` folder into `unity/Assets/Resources/MCS/UnityAssetStore/`.

## Run

If you want to run an MCS Scene in the Unity Editor:

- Copy a config file from the [AI2-THOR scenes folder in our MCS GitHub repository](https://github.com/NextCenturyCorporation/MCS/tree/master/ai2thor_wrapper/scenes) into the `unity/Assets/Resources/MCS/Scenes/` folder.
- In the "MCS" Game Object, enter the name of your scene file in the "Default Scene File" property.
- If you want to see the class/depth/object masks, enable "Image Synthesis" in the "FirstPersonCharacter" (camera) within the "FPSController" Game Object.
- While running, use WASD to move, arrow buttons to look, and escape to pass.

## Build

Open the Unity Editor and build the project.

Alternatively, if you want to build the Unity project via the command line, run the command below, replacing the path to your Unity executable file, log file name, `<cloned_repository>`, and the `executeMethod` as needed.  Please note that this command will build ALL the AI2-THOR scenes which will take a very long time (my only solution was to delete all the AI2-THOR scenes with `rm <cloned_repository>/unity/Assets/Scenes/FloorPlan*`).

```
./Unity-2018.3.0f2/Editor/Unity -quit -batchmode -logfile MCS-Unity-Build.log -projectpath <cloned_repository>/unity/ -executeMethod Build.Linux64
```

## TAR

To TAR the application's Data directory:

```
cd <cloned_repository>/unity/
tar -czvf MCS-AI2-THOR-Unity-App-<version>_Data.tar.gz MCS-AI2-THOR-Unity-App-<version>_Data/
```

## Important Files and Folders

- [`unity/`](./unity)  The MCS Unity project.  Add this folder as a project in your Unity Hub.
- `unity/Assets/Scenes/MCS.unity`  The MCS Unity Scene.  You can load and edit this in the Unity Editor.
- [`unity/Assets/Scripts/MachineCommonSenseMain.cs`](./unity/Assets/Scripts/MachineCommonSenseMain.cs)  The main MCS Unity script that is imported into and runs within the Scene.
- [`unity/Assets/Scripts/MachineCommonSensePerformerManager.cs`](./unity/Assets/Scripts/MachineCommonSensePerformerManager.cs)  A custom subclass extending AI2-THOR's [AgentManager](./unity/Assets/Scripts/AgentManager.cs) that handles all the communication between the Python API and the Unity Scene.
- [`unity/Assets/Resources/MCS/`](./unity/Assets/Resources/MCS)  Folder containing all MCS runtime resources.
- [`unity/Assets/Resources/MCS/mcs_object_registry.json`](./unity/Assets/Resources/MCS/mcs_object_registry.json)  Config file containing the MCS Scene's specific custom Game Objects that may be loaded at runtime. 
- [`unity/Assets/Resources/MCS/primitive_object_registry.json`](./unity/Assets/Resources/MCS/primitive_object_registry.json)  Config file containing the MCS Scene's Unity Primitive Game Objects that may be loaded at runtime. 
- [`unity/Assets/Resources/MCS/Materials/`](./unity/Assets/Resources/MCS/Materials)  Copy of AI2-THOR's [`unity/Assets/QuickMaterials/`](./unity/Assets/QuickMaterials).  Must be in the `Resources` folder to access at runtime.
- [`unity/Assets/Resources/MCS/Scenes/`](./unity/Assets/Resources/MCS/Scenes)  Folder containing sample scene config files (see [Run](#run)).

## Differences from AI2-THOR Scenes

- The `FPSController` object is mostly the same, but I made it smaller to simulate a baby.  This also allowed me to downscale the room which not only improves performance but also was necessary to get the depth masks to work (while standing at one end of the room, you still want to see the far wall in the depth masks).  Changes affected the `Transform`, `Character Controller`, and `Capsule Collider` in the `FPSController` and the `Transform` in the `FirstPersonCharacter` (camera) nested inside the `FPSController`.
- In the `FPSController` object, I replaced the `PhysicsRemoteFPSAgentController` and `StochasticRemoteFPSAgentController` scripts with our `MachineCommonSensePerformerManager` script.
- In the `PhysicsSceneManager` object, I replaced the `AgentManager` script with our `MachineCommonSensePerformerManager` script.
- Added structural objects (walls, floor, ceiling).
- Added the invisible `MCS` object containing our `MachineCommonSenseMain` script that runs in the background.

## Code Workflow

### Shared Workflow

1. (Unity) `BaseFPSAgentController.ProcessControlCommand` will use `Invoke` to call the specific action function in `BaseFPSAgentController` or `PhysicsRemoteFPSAgentController` (like `MoveAhead` or `LookUp`)
2. (Unity) The specific action function will call `BaseFPSAgentController.actionFinished()` to set `actionComplete` to `true`

### Python API Workflow

1. (Python) **You** create a new Python AI2-THOR `Controller` object
2. (Python) The `Controller` class constructor will automatically send a `Reset` action over the AI2-THOR socket to `AgentManager.ProcessControlCommand(string action)`
3. (Unity) `AgentManager.ProcessControlCommand` will create a `ServerAction` from the action string and call `AgentManager.Reset(ServerAction action)` to load the MCS Unity scene
4. (Python) **You** call `controller.step(dict action)` with an `Initialize` action to load new MCS scene configuration JSON data and re-initialize the player
5. (Unity) The action is sent over the AI2-THOR socket to `AgentManager.ProcessControlCommand(string action)`
6. (Unity) `AgentManager.ProcessControlCommand` will create a `ServerAction` from the action string and call `AgentManager.Initialize(ServerAction action)`
7. (Unity) `AgentManager.Initialize` will call `AgentManager.addAgents(ServerAction action)`, then call `AgentManager.addAgent(ServerAction action)`, then call `BaseFPSAgentController.ProcessControlCommand(ServerAction action)` with the `Initialize` action
8. (Unity) See the [**Shared Workflow**](#shared-workflow)
9. (Unity) `AgentManager.LateUpdate`, which is run every frame, will see `actionComplete` is `true` and call `AgentManager.EmitFrame()`
10. (Unity) `AgentManager.EmitFrame` will return output from the `Initialize` action to the Python API (`controller.step`) and await the next action
11. (Python) **You** call `controller.step(dict action)` with a specific action
12. (Unity) The action is sent over the AI2-THOR socket to `AgentManager.ProcessControlCommand(string action)`
13. (Unity) `AgentManager.ProcessControlCommand` will create a `ServerAction` from the action string and call `BaseFPSAgentController.ProcessControlCommand(ServerAction action)` (except on `Reset` or `Initialize` actions)
14. (Unity) See the [**Shared Workflow**](#shared-workflow)
15. (Unity) `AgentManager.LateUpdate`, which is run every frame, will see `actionComplete` is `true` and call `AgentManager.EmitFrame()`
16. (Unity) `AgentManager.EmitFrame` will return output from the specific action to the Python API (`controller.step`) and await the next action

### Unity Editor Workflow

1. (Unity) Loads the Unity scene
2. (Editor) Waits until **you** press a key
3. (Unity) `DebugDiscreteAgentController.Update`, which is run every frame, will create a `ServerAction` from the key you pressed and call `BaseFPSAgentController.ProcessControlCommand(SeverAction action)`
4. (Unity) See the [**Shared Workflow**](#shared-workflow)
5. (Unity) `DebugDiscreteAgentController.Update` will see `actionComplete` is `true` and then waits until **you** press another key

## Lessons Learned

- Adding AI2-THOR's custom Tags and Layers to your Game Objects is needed for their scripts to work properly.  For example, if you don't tag the walls as `Structure`, then the player can walk fully into them.
- Fast moving objects that use Unity physics, as well as all structural objects, should have their `Collision Detection` (in their `Rigidbody`) set to `Continuous`.  With these changes, a fast moving object that tries to move from one side of a wall to the other side in a single frame will be stopped as expected.
- The FPSController object's robot model is half scale, and ends up being about 0.5 high while the game is running.  I had to change the properties of the `Capsule Collider` and the `Character Controller` so the FPSController would not collide with the floor while moving (`PhysicsRemoteFPSAgentController.capsuleCastAllForAgent`).  Previously:  `center.y=-0.45`, `radius=0.175`, `height=0.9`.  Now:  `center.y=-0.05`, `radius=0.2`, `height=0.5` (though these numbers seem smaller than they should really be).

## Making a New Prefab

![sample_prefab](./sample_prefab.png)

Take a GameObject (we'll call it the "Target" object) containing a MeshFilter, MeshRenderer, and material(s). Sometimes the components are on the Target itself, and sometimes they are on a child of the Target.

1. Set the Tag of the Target to "SimObjPhysics" and set the Layer to "SimObjVisible".
2. Add a Rigidbody to the Target. Ensure its "Use Gravity" property is true.
3. If the Target (or its child) does not have any Colliders, you'll have to make them. Create an Empty Child under the Target called "Colliders" and mark it static. Then create an Empty Child under "Colliders" for each Collider you need to make (give them useful names). On each child, add the correct Collider component (often a box, but sometimes others -- note that all MeshColliders should be CONVEX). Adjust the Transform of each child to position the Collider as needed. Set the Tag of each child to "SimObjPhysics" and set the Layer to "SimObjVisible".
4. Create an Empty Child under the Target called "VisibilityPoints" (no space!) and mark it static. Then create an Empty Child under "VisibilityPoints" for each visibility point you need to make. Adjust the Transform of each child to position the visibility point as needed. Set the Layer of each child to "SimObjVisible". A visibility point should be positioned on each corner of the Target, plus one or more points should be positioned on each large surface (think: if all the corners are occluded, can I still draw line-of-sight to the center of the Target?).
5. Create an Empty Child under the Target called "BoundingBox" (no space!) and add a BoxCollider component to it. Ensure this BoxCollider is NOT ACTIVE (but NOT the other Colliders). Adjust the Transform of the "BoundingBox" to completely enclose the Target. Set the Layer of the Target to "SimObjInvisible".
6. On the Target itself, add a SimObjPhysics component (it's an AI2-THOR script). Set the "Primary Property" to "Static" (for non-moveable objects), "Moveable", or "Can Pickup" (a subset of Moveable). Set the "Secondary Properties" as needed (like "Receptacle" and/or "Can Open"). Set the "Bounding Box" property to the "BoundingBox" child you created. Set the "Visibility Points" property to the visibility point children you created. Set the "My Colliders" property to the Collider children you created. Optionally, set the "Salient Materials" property as needed.
7. If the Target is openable, add a Can Open_Object component (AI2-THOR script) to the Target. Set the "Moving Parts" property to the Target. Set the "Open Positions" and the "Close Positions" to the correct positions. Change the "Movement Type" property to "Slide", "Rotate", or "Scale" as needed.
8. If the Target is a Receptacle, create an Empty Child under the Target called "ReceptacleTriggerBox" (no spaces!) and mark it static. Set the Tag of the "ReceptacleTriggerBox" to "Receptacle" and set the Layer to "SimObjInvisible". Add a "BoxCollider" component to the "ReceptacleTriggerBox" and set its "Is Trigger" property to true. Adjust the Transform of the "ReceptacleTriggerBox" to the receptacle area that can contain objects (I'm not sure if the height actually matters). Add a Contains component (AI2-THOR script) to the "ReceptacleTriggerBox".
9. For each other receptacle within the Target (like a cabinet door, drawer, shelf, etc.), create an Empty Child under the Target (we'll call this the Sub-Target), give it a useful name, and move the mesh corresponding to the Sub-Target to be under the child. Repeat steps 1-8 (EXCEPT the Bounding Box) on each Sub-Target.
10. Click-and-drag the finished Target into the Project tab of the Unity Editor to save it as a new Prefab file. Add a new entry for it in the mcs_object_registry file.

## Changelog of AI2-THOR Classes

- `Scripts/AgentManager`:
  - Added properties to `ObjectMetadata`: `points`, `visibleInCamera`
  - Added properties to `ServerAction`: `logs`, `objectDirection`, `receptacleObjectDirection`, `sceneConfig`
  - Added `virtual` to functions: `setReadyToEmit`, `Update`
  - Changed variables or functions from `private` to `protected`: `physicsSceneManager`, most `render*Image` variables
  - Changed properties in `ServerAction`: `horizon` (from int to float)
  - Changed variables or functions from `private` to `public`: `captureScreen`, 'renderImage'
  - Split the existing metadata-update-behavior of the `addObjectImageForm` function into a separate, new function called `UpdateMetadataColors`
  - Created the `InitializeForm` and `FinalizeMultiAgentMetadata` virtual functions and called them both inside `EmitFrame`
  - Changed `readyToEmit = true;` to `this.setReadyToEmit(true);` in `addAgents`, `ProcessControlCommand`, `Start`
  - Added Object Types: `Hollow`
- `Scripts/BaseFPSAgentController`:
  - Added `virtual` to functions: `Initialize`, `ProcessControlCommand`
  - Removed the hard-coded camera properties in the `SetAgentMode` function
  - Replaced the call to `checkInitializeAgentLocationAction` in `Initialize` with calls to `snapToGrid` and `actionFinished` so re-initialization doesn't cause the player to move for a few steps
  - Added `lastActionStatus` to `Initialize` to help indicate success or failure
- `Scripts/CanOpen_Object`:
  - Rewrote part of the `Interact` function so it doesn't use iTween if `animationTime` is `0`.  Also the `Interact` function now uses the `openPercentage` on both "open" and "close".
- `Scripts/DebugDiscreteAgentController`:
  - Calls `ProcessControlCommand` on the controller object with an "Initialize" action in its `Start` function (so the Unity Editor Workflow mimics the Python API Workflow)
  - Added a way to "Pass" (with the "Escape" button) or "Initialize" (with the "Backspace" button) on a step while playing the game in the Unity Editor
  - Added support for executing other actions and properties while playing the game in the Unity Editor
- `Scripts/InstantiatePrefabTest`:
  - Fixed a bug in the `CheckSpawnArea` function in which the object's bounding box was not adjusted by the object's scale.
- `Scripts/PhysicsRemoteFPSAgentController`:
  - Changed variables or functions from `private` to `protected`: `physicsSceneManager`, `ObjectMetadataFromSimObjPhysics`
  - Added `virtual` to functions: `CloseObject`, `DropHandObject`, `OpenObject`, `PickupObject`, `PullObject`, `PushObject`, `PutObject`, `ResetAgentHandPosition`, `ThrowObject`, `ToggleObject`
  - Commented out a block in the `PickupObject` function that checked for collisions between the held object and other objects in the scene because it caused odd behavior if you were looking at the floor.  The `Look` functions don't make this check either, and we may decide not to move the held object during `Look` actions anyway.
  - In the `PlaceHeldObject` function: ignores `PlacementRestrictions` if `ObjType` is `IgnoreType`; sets the held object's parent to null so the parent's properties (like scale) don't affect the placement validation; sets the held object's `isKinematic` property to `false` if placement is successful.
  - Added `lastActionStatus` to Move actions, as well as to `DropHandObject`, `PickupObject`, and `PutObject` to help indicate success or reason for failure
  - Added check to make sure object exists for `DropHandObject`
  - Make sure objectId specified is actually the object being held for `DropHandObject` and `PutObject`
  - Undid objectId being reset to receptableObjectId and not allowing objects to be placed in closed receptacles regardless of type of receptacle for `PutObject`
  - In `PickupContainedObjects` and `DropContainedObjects`, added a null check for the Colliders object and added a loop over the colliders array in the SimObjPhysics script.
  - Added code to allow movement that will auto calculate the space to close the distance to an object within 0.1f
- `Scripts/PhysicsSceneManager`:
  - Added `virtual` to functions: `Generate_UniqueID`
- `Scripts/SimObjPhysics`:
  - Changed the `Start` function to `public` so we can call it from our scripts
  - Added `ApplyRelativeForce` to apply force in a direction relative to the agent's current position.
- `Scripts/SimObjType`:
  - Added `IgnoreType` to the `SimObjType` enum, `ReturnAllPoints`, and `AlwaysPlaceUpright`
- `Shaders/DepthBW`:
  - Changed the divisor to increase the effective depth of field for the depth masks.
- `Scripts/MachineCommonSenseController`:
  - Added custom `RotateLook` to use relative inputs instead of absolute values.
  - Added checks to see whether objects exist and set lastActionStatus appropriately for `PutObject`
  - Added custom `ThrowObject` in order to use a relative directional vector to throw object towards.
  - Changed 'CheckIfAgentCanMove' to take a reference to a directionMagnitude instead of a copy parameter, so if distance to object is greater than zero, we can move a partial distance in 'moveInDirection' by adjusting the Vector3
- `ImageSynthesis/ImageSynthesis`:
  - Added a null check in `OnSceneChange`
