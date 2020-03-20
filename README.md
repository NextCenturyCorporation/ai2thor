# Machine Common Sense Fork of AI2-THOR

Original documentation:  https://github.com/allenai/ai2thor

## Setup

The AI2-THOR documetation wants us to use Unity Editor version `2018.3.6` but that version was not available for the Linux Unity Editor.  However, version [`2018.3.0f2`](https://forum.unity.com/threads/unity-on-linux-release-notes-and-known-issues.350256/page-2#post-4009651) does work fine.  Do NOT use any version that starts with `2019` because its build files are not compatible with AI2-THOR.

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

## Changelog of AI2-THOR Classes

- `Scripts/AgentManager`:
  - Added properties to `ObjectMetadata`: `points`, `visibleInCamera`
  - Added properties to `ServerAction`: `logs`, `objectDirection`, `receptacleObjectDirection`, `sceneConfig`
  - Added `virtual` to functions: `Update`
  - Changed variables or functions from `private` to `protected`: `physicsSceneManager`
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
- `Scripts/PhysicsSceneManager`:
  - Added `virtual` to functions: `Generate_UniqueID`
- `Scripts/SimObjPhysics`:
  - Changed the `Start` function to `public` so we can call it from our scripts
- `Scripts/SimObjType`:
  - Added `IgnoreType` to the `SimObjType` enum, `ReturnAllPoints`, and `AlwaysPlaceUpright`
- `Shaders/DepthBW`:
  - Changed the divisor to increase the effective depth of field for the depth masks.
- `Scripts/MachineCommonSenseController`:
  - Added custom `RotateLook` to use relative inputs instead of absolute values.
  - Added checks to see whether objects exist and set lastActionStatus appropriately for `PutObject`
