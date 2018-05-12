using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HutongGames.PlayMaker.Actions
{
#if PLAYMAKER
    [ActionCategory( ActionCategory.Events )]
    [Tooltip("Allows Playmaker to generate VRChat Trigger events")]
    public class VRCPlaymakerAction : FsmStateAction
    {
        [RequiredField]
        [CheckForComponent(typeof(VRCSDK2.VRC_Trigger))]
        public FsmOwnerDefault gameObject;

        [Tooltip("Name of a custom event in the attached VRC_Trigger object")]
        public FsmString eventName;

        public override void OnEnter()
        {
            VRCSDK2.VRC_Trigger.TriggerCustom(gameObject.GameObject.Value, eventName.ToString() );
            Finish();
        }
    }

#endif
}
