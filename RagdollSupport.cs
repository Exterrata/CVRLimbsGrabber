using MelonLoader;
using ml_prm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koneko;
internal class RagdollSupport
{
    public static RagdollController Ragdoll;
    public static Vector3 Velocity;
    public static bool WaitUnragdoll;

    public static void Initialize() => Ragdoll = LimbGrabber.PlayerLocal.gameObject.GetComponent<RagdollController>();
    
    public static void ToggleRagdoll() => Ragdoll.SwitchRagdoll();

    public static IEnumerator WaitToggleRagdoll()
    {
        WaitUnragdoll = true;
        while(WaitUnragdoll) {
            Velocity = LimbGrabber.PlayerLocal.position - LimbGrabber.LastRootPosition;
            if (Velocity.magnitude < 0.1) WaitUnragdoll = false;
            yield return new WaitForSeconds(1);
        }
        yield return new WaitForSeconds(2);
        if(Ragdoll.enabled) Ragdoll.SwitchRagdoll();
    }
}
