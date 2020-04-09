﻿using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
using System.Collections.Generic;

public class MachineCommonSenseController : PhysicsRemoteFPSAgentController {
    public static float POSITION_Y = 0.4625f;

    public static float DISTANCE_HELD_OBJECT_Y = 0.15f;
    public static float DISTANCE_HELD_OBJECT_Z = 0.15f;

    // TODO MCS-95 Make the room size configurable in the scene configuration file.
    // The room dimensions are always 5x5 so the distance from corner to corner is around 7.08.
    public static float MAX_DISTANCE_ACCROSS_ROOM = 7.08f;

    // The number of times to run Physics.Simulate after each action from the player is LOOPS * STEPS.
    public static int PHYSICS_SIMULATION_LOOPS = 5;
    public static int PHYSICS_SIMULATION_STEPS = 3;

    public int step = 0;

    protected int minHorizon = -90;
    protected int maxHorizon = 90;
    protected float minRotation = -360f;
    protected float maxRotation = 360f;

    public override void CloseObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        base.CloseObject(action);
    }

    private string ConvertObjectDirectionToId(Vector3 direction, string previousObjectId) {
        // If the objectId was set or the direction vector was not set, return the previous objectId.
        if ((previousObjectId != null && !previousObjectId.Equals("")) ||
            (direction.x == 0 && direction.y == 0 && direction.z == 0)) {
            return previousObjectId;
        }

        int layerMask = (1 << 8); // Only look at objects on the SimObjVisible layer.
        List<RaycastHit> hits = Physics.RaycastAll(this.transform.position, direction,
            MachineCommonSenseController.MAX_DISTANCE_ACCROSS_ROOM, layerMask).ToList();
        if (hits.Count == 0) {
            this.errorMessage = "Cannot find any object on the directional vector.";
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            this.actionFinished(false);
            return previousObjectId;
        }
        else {
            hits.Sort(delegate (RaycastHit one, RaycastHit two) {
                return one.distance.CompareTo(two.distance);
            });
            SimObjPhysics simObjPhysics = hits.First().transform.gameObject
                .GetComponentInParent<SimObjPhysics>();
            if (simObjPhysics == null) {
                this.errorMessage = "The closest object on the directional vector is not interactable.";
                this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_INTERACTABLE);
                this.actionFinished(false);
                return previousObjectId;
            }
            else {
                return simObjPhysics.UniqueID;
            }
        }
    }

    public override bool DropHandObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return false;
        }

        SimObjPhysics target = null;

        if (physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
            target = physicsSceneManager.UniqueIdToSimObjPhysics[action.objectId];
        }

        // Reactivate the object BEFORE trying to drop it so that we can see if it's obstructed.
        // TODO MCS-77 This object will always be active, so we won't need to reactivate this object.
        if (target != null && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(true);
        }

        bool status = base.DropHandObject(action);

        // Deactivate the object again if the drop failed.
        // TODO MCS-77 We should never need to deactivate this object again (see PickupObject).
        if (target != null && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }

        return status;
    }

    public override ObjectMetadata[] generateObjectMetadata() {
        // TODO MCS-77 The held objects will always be active, so we won't need to reactivate this object again.
        bool deactivate = false;
        if (this.ItemInHand != null && !this.ItemInHand.activeSelf) {
            deactivate = true;
            this.ItemInHand.SetActive(true);
        }

        List<string> visibleObjectIds = this.GetAllVisibleSimObjPhysics(this.m_Camera,
            MachineCommonSenseController.MAX_DISTANCE_ACCROSS_ROOM).Select((obj) => obj.UniqueID).ToList();

        ObjectMetadata[] objectMetadata = base.generateObjectMetadata().ToList().Select((metadata) => {
            // The "visible" property in the ObjectMetadata really describes if the object is within reach.
            // We also want to know if we can currently see the object in our camera view.
            metadata.visibleInCamera = visibleObjectIds.Contains(metadata.objectId);
            return metadata;
        }).ToArray();

        // TODO MCS-77 The held objects will always be active, so we shouldn't deactivate this object again.
        if (deactivate) {
            this.ItemInHand.SetActive(false);
        }

        return objectMetadata;
    }

    public override MetadataWrapper generateMetadataWrapper() {
        MetadataWrapper metadata = base.generateMetadataWrapper();
        metadata.lastActionStatus = this.lastActionStatus;
        metadata.reachDistance = this.maxVisibleDistance;
        return this.agentManager.UpdateMetadataColors(this, metadata);
    }

    public override void Initialize(ServerAction action) {
        base.Initialize(action);

        this.step = 0;
        MachineCommonSenseMain main = GameObject.Find("MCS").GetComponent<MachineCommonSenseMain>();
        main.enableVerboseLog = main.enableVerboseLog || action.logs;
        // Reset the MCS scene configuration data and player.
        main.ChangeCurrentScene(action.sceneConfig);
    }

    public void MCSCloseObject(ServerAction action) {
        // The AI2-THOR Python library has buggy error checking specifically for the CloseObject function,
        // so create our own function and call it from the Python API.
        this.CloseObject(action);
    }

    public void MCSOpenObject(ServerAction action) {
        // The AI2-THOR Python library has buggy error checking specifically for the OpenObject function,
        // so create our own function and call it from the Python API.
        this.OpenObject(action);
    }

    protected override ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible) {
        ObjectMetadata objectMetadata = base.ObjectMetadataFromSimObjPhysics(simObj, isVisible);

        // Each SimObjPhysics object should have a MeshFilter component.
        MeshFilter meshFilter = simObj.gameObject.GetComponentInChildren<MeshFilter>();

        // Use the object's renderer (each object should have a renderer, except maybe shelf children in complex
        // receptacle objects) for its position because its transform's position may not be its actual position
        // in the camera since the renderer may be defined in a child object with an offset position.
        Renderer renderer = simObj.gameObject.GetComponentInChildren<Renderer>();
        if (renderer != null) {
            objectMetadata.position = renderer.bounds.center;
            // Use the object's new position for the distance previously set in generateObjectMetadata.
            objectMetadata.distance = Vector3.Distance(this.transform.position, objectMetadata.position);
        }

        // From https://docs.unity3d.com/Manual/DirectionDistanceFromOneObjectToAnother.html
        objectMetadata.heading = objectMetadata.position - this.transform.position;
        objectMetadata.direction = (objectMetadata.heading / objectMetadata.heading.magnitude);

        // Calculate a distance with only the X and Z coordinates for our Python API.
        objectMetadata.distanceXZ = Vector3.Distance(new Vector3(this.transform.position.x, 0, this.transform.position.z),
            new Vector3(simObj.transform.position.x, 0, simObj.transform.position.z));

        if (objectMetadata.objectBounds == null && simObj.BoundingBox != null) {
            objectMetadata.objectBounds = this.WorldCoordinatesOfBoundingBox(simObj);
        }

        return objectMetadata;
    }

    public override void OpenObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        base.OpenObject(action);
    }

    public override void PickupObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        SimObjPhysics target = null;

        if (physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
            target = physicsSceneManager.UniqueIdToSimObjPhysics[action.objectId];

            // Update our hand's position so that the object we want to hold doesn't clip our body.
            // TODO MCS-77 We may want to change how this function is used.
            this.UpdateHandPositionToHoldObject(target);
        }

        base.PickupObject(action);

        // TODO MCS-77 Find a way to handle held object collisions so we don't have to deactivate this object.
        if (target != null && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }
    }

    public override void ProcessControlCommand(ServerAction controlCommand) {
        // Never let the placeable objects ignore the physics simulation (they should always be affected by it).
        controlCommand.placeStationary = false;

        base.ProcessControlCommand(controlCommand);

        // Clear the saved images from the previous step.
        ((MachineCommonSensePerformerManager)this.agentManager).ClearSavedImages();

        if (!controlCommand.action.Equals("Initialize")) {
            this.step++;
        }
    }

    public override void PullObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        if (physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().uniqueID) {
            Debug.Log("Cannot pull. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            base.PullObject(action);
        }

    }

    public override void PushObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        if (physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().uniqueID) {
            Debug.Log("Cannot push. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            base.PushObject(action);
        }
    }

    public override void PutObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        if (!physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
            errorMessage = "Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            actionFinished(false);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            return;
        }

        if (!physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.receptacleObjectId)) {
            errorMessage = "Receptacle Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            actionFinished(false);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            return;
        }

        SimObjPhysics target = physicsSceneManager.UniqueIdToSimObjPhysics[action.objectId];

        // Reactivate the object BEFORE trying to place it so that we can see if it's obstructed.
        // TODO MCS-77 This object will always be active, so we won't need to reactivate this object.
        if (target && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(true);
        }

        base.PutObject(action);

        // Deactivate the object again if the placement failed.
        // TODO MCS-77 We should never need to deactivate this object (see PickupObject).
        if (target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }
    }

    public override void ResetAgentHandPosition(ServerAction action) {
        // Don't reset the player's hand position if the player is just moving or rotating.
        // Use this.lastAction here because this function's ServerAction argument is sometimes null.
        if (this.lastAction.StartsWith("Move") || this.lastAction.StartsWith("Rotate") ||
            this.lastAction.StartsWith("Look") || this.lastAction.StartsWith("Teleport")) {
            return;
        }
        base.ResetAgentHandPosition(action);
    }

    public override void RotateLook(ServerAction response)
    {
        // Need to calculate current rotation/horizon and increment by inputs given
        float updatedRotationValue = transform.localEulerAngles.y + response.rotation.y;
        float currentHorizonValue = m_Camera.transform.localEulerAngles.x;
        // The horizon should always be either between 0 and 90 (looking down) or 270 and 360 (looking up).
        // If looking up, we must change the horizon from [360, 270] to [0, -90].
        currentHorizonValue = (currentHorizonValue >= 270 ? (currentHorizonValue - 360) : currentHorizonValue);
        float updatedHorizonValue = currentHorizonValue + response.horizon;

        // Check to ensure rotation value stays between -360 and 360
        while (updatedRotationValue >= maxRotation)
        {
            updatedRotationValue -= maxRotation;
        }

        while (updatedRotationValue <= minRotation)
        {
            updatedRotationValue += maxRotation;
        }

        // Limiting where to look based on realistic expectation (for instance, a person can't turn
        // their head 180 degrees)
        if (updatedHorizonValue > maxHorizon || updatedHorizonValue < minHorizon)
        {
            Debug.Log("Value of horizon needs to be between " + minHorizon + " and " + maxHorizon +
                ". Setting value to 0.");
            updatedHorizonValue = 0;
        }

        ServerAction action = new ServerAction();
        action.rotation.y = updatedRotationValue;
        action.horizon = updatedHorizonValue;
        base.RotateLook(action);
    }

    public void SimulatePhysics() {
        if (this.agentManager.renderImage) {
            // We only need to save ONE image of the scene after initialization.
            StartCoroutine(this.SimulatePhysicsSaveImagesIncreaseStep(this.step == 0 ? 1 :
                MachineCommonSenseController.PHYSICS_SIMULATION_LOOPS));
        }

        else {
            // (Also simulate the physics after initialization so that the objects can settle down onto the floor.)
            this.SimulatePhysicsCompletely();
            // Notify the AgentManager to send the action output metadata and images to the Python API.
            ((MachineCommonSensePerformerManager)this.agentManager).FinalizeEmit();
        }
    }

    private void SimulatePhysicsCompletely() {
        for (int i = 0; i < MachineCommonSenseController.PHYSICS_SIMULATION_LOOPS; ++i) {
            this.SimulatePhysicsOnce();
        }
    }

    private void SimulatePhysicsOnce() {
        // Call Physics.Simulate multiple times with a small step value because a large step
        // value causes collision errors.  From the Unity Physics.Simulate documentation:
        // "Using step values greater than 0.03 is likely to produce inaccurate results."
        for (int i = 0; i < MachineCommonSenseController.PHYSICS_SIMULATION_STEPS; ++i) {
            Physics.Simulate(0.01f);
        }
    }

    private IEnumerator SimulatePhysicsSaveImagesIncreaseStep(int thisLoop) {
        yield return new WaitForEndOfFrame(); // Required for coroutine functions

        // Run the physics simulation for a little bit, then pause and save the images for the current scene.
        this.SimulatePhysicsOnce();

        ((MachineCommonSensePerformerManager)this.agentManager).SaveImages(this.imageSynthesis);

        int nextLoop = thisLoop - 1;

        if (nextLoop > 0) {
            // Continue the next loop: run the physics simulation, then save more images.
            StartCoroutine(this.SimulatePhysicsSaveImagesIncreaseStep(nextLoop));
        }
        else {
            // Once finished, notify the AgentManager to send the action output metadata and images to the Python API.
            ((MachineCommonSensePerformerManager)this.agentManager).FinalizeEmit();
        }
    }

    public override void ThrowObject(ServerAction action) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        if (!physicsSceneManager.UniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
            errorMessage = "Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            actionFinished(false);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            return;
        }

        SimObjPhysics target = physicsSceneManager.UniqueIdToSimObjPhysics[action.objectId];

        // Reactivate the object BEFORE trying to throw it so that we can see if it's obstructed.
        // TODO MCS-77 This object will always be active, so we won't need to reactivate this object.
        if (target && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(true);
        }

        GameObject gameObj = ItemInHand;

        if(base.DropHandObject(action)) {
            if (action.objectDirection.x != 0 || action.objectDirection.y != 0 || action.objectDirection.z != 0) {
                gameObj.GetComponent<SimObjPhysics>().ApplyRelativeForce(action.objectDirection, action.moveMagnitude);
            } else {
                // throw object forward if no direction input is given
                gameObj.GetComponent<SimObjPhysics>().ApplyRelativeForce(Vector3.forward, action.moveMagnitude);
            }
        }

        // Deactivate the object again if the throw failed.
        // TODO MCS-77 We should never need to deactivate this object (see PickupObject).
        if (target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }
    }

    public override void ToggleObject(ServerAction action, bool toggleOn, bool forceAction) {
        bool continueAction = TryConvertingEachObjectDirectionToId(action);

        if (!continueAction) {
            return;
        }

        base.ToggleObject(action, toggleOn, forceAction);
    }

    private bool TryConvertingEachObjectDirectionToId(ServerAction action) {
        action.objectId = this.ConvertObjectDirectionToId(action.objectDirection,
            action.objectId);
        if (!this.actionComplete) {
            action.receptacleObjectId = this.ConvertObjectDirectionToId(action.receptacleObjectDirection,
                action.receptacleObjectId);
        }
        // If we haven't yet called actionFinished then actionComplete will be false; continue the action.
        return !this.actionComplete;
    }

    private void UpdateHandPositionToHoldObject(SimObjPhysics target) {
        MeshFilter meshFilter = target.gameObject.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null) {
            // Move the player's hand on the Y axis corresponding to the size of the target object so that the object,
            // once held, is shown at the bottom of the player's camera view.
            float handY = (meshFilter.mesh.bounds.size.y * meshFilter.transform.localScale.y);
            // Move the player's hand on the Z axis corresponding to the size of the target object so that the object,
            // once held, never collides with the player's body.
            float handZ = (meshFilter.mesh.bounds.size.z / 2.0f * meshFilter.transform.localScale.z);
            if (!GameObject.ReferenceEquals(meshFilter.gameObject, target.gameObject)) {
                handY = (handY + (meshFilter.transform.localPosition.y * meshFilter.transform.localScale.y));
                handZ = ((handZ - meshFilter.transform.localPosition.z) * target.gameObject.transform.localScale.z);
            }
            this.AgentHand.transform.localPosition = new Vector3(this.AgentHand.transform.localPosition.x,
                (handY + MachineCommonSenseController.DISTANCE_HELD_OBJECT_Y) * -1,
                (handZ + MachineCommonSenseController.DISTANCE_HELD_OBJECT_Z) * (1.0f / this.transform.localScale.z));
        } else {
            Debug.LogError("PickupObject target " + target.gameObject.name + " does not have a MeshFilter!");
        }
    }
}
