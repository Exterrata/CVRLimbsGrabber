using MelonLoader;
using ml_prm;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koneko;
internal class RagdollSupport
{
    internal static RagdollController Ragdoll;

    public static void Initialize() => Ragdoll = RagdollController.Instance;

    public static void ToggleRagdoll() {
        if (!Ragdoll.IsRagdolled()) Ragdoll.SwitchRagdoll();
    }
}
