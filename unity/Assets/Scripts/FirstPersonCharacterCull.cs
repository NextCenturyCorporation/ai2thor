﻿using UnityEngine;
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

[ExecuteInEditMode]
public class FirstPersonCharacterCull : MonoBehaviour
{
    public bool StopCullingThingsForASecond = false;
    public MeshRenderer [] RenderersToHide; //Mesh renderer that you want this script's camera to cull
    public PhysicsRemoteFPSAgentController FPSController;


    //references to renderers for when Agent is in Tall mode
    public MeshRenderer [] TallRenderers;
    //references to renderers for when the Agent is in Bot mode
    public MeshRenderer [] BotRenderers;

    public void SwitchRenderersToHide(agentMode mode)
    {
        if(mode == agentMode.Tall)
        RenderersToHide = TallRenderers;

        else if(mode == agentMode.Bot)
        RenderersToHide = BotRenderers;
    }

    void OnPreRender() //Just before this camera starts to render...
    {
        if(!StopCullingThingsForASecond)
        {
            if(FPSController != null && RenderersToHide != null && FPSController.IsVisible)//only do this if visibility capsule has been toggled on
            {
                foreach (MeshRenderer mr in RenderersToHide)
                {
                    mr.enabled = false; //Turn off renderer
                }
            }
        }

    }

    void OnPostRender() //Immediately after this camera renders...
    {
        if(!StopCullingThingsForASecond)
        {
            if(FPSController != null && RenderersToHide != null && FPSController.IsVisible)//only do this if visibility capsule is toggled on
            {
                foreach (MeshRenderer mr in RenderersToHide)
                {
                    mr.enabled = true; //Turn it back on
                }
            }
        }
    }

}