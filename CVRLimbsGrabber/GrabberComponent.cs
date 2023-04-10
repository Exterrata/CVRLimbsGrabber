using System.Collections.Generic;
using System;
using UnityEngine;
using ABI_RC.Core.Player;
using ABI_RC.Core.Networking.IO.Social;

namespace Koneko;
public class GrabberComponent : MonoBehaviour
{
    internal PlayerAvatarMovementData MovementData;
    internal PlayerDescriptor PlayerDescriptor;
    internal int grabber = 0;
    internal int Limb = -1;
    internal bool Grabbing;
    public bool Grab;

    void Update()
    {
        bool grabbing = false;
        if (grabber == 0) grabbing = Grab;
        else if (!Friends.FriendsWith(PlayerDescriptor.ownerId) && LimbGrabber.Friend.Value) return;
        else if (grabber == 1) grabbing = (int)MovementData.AnimatorGestureLeft == 1 || MovementData.LeftMiddleCurl > 0.5;
        else if (grabber == 2) grabbing = (int)MovementData.AnimatorGestureRight == 1 || MovementData.RightMiddleCurl > 0.5;

        if (grabbing && !Grabbing)
        {
            LimbGrabber.Grab(this);
            Grabbing = true;
        }
        else if (!grabbing && Grabbing)
        {
            LimbGrabber.Release(this);
            Grabbing = false;
        }
    }

    void OnDestroy()
    {
        LimbGrabber.Release(this);
    }
}