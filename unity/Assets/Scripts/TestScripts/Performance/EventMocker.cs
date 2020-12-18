using System;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace PerformanceTests
{
    public class EventMocker : MonoBehaviour
    {
        ServerAction action;
        PhysicsRemoteFPSAgentController controller;
        bool isRunning = true;
        
        void Start()
        {
            action = new ServerAction
            {
                continuous = true,
                forceAction = false,
                gridSize = 0.1f,
                visibilityDistance = 0.4f,
                action = "RotateRight"
            };

            controller = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();

            isRunning = true;
        }

        void Update()
        {
            if (isRunning && controller.actionComplete) 
                controller.ProcessControlCommand(action);
        }

        public void Stop()
        {
            isRunning = false;
        }
    }
}
