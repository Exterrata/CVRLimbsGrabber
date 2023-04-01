using System.Collections.Generic;
using System;
using UnityEngine;
using MelonLoader;
using HarmonyLib;
using RootMotion.FinalIK;
using ABI_RC.Core.Player;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Systems.IK.SubSystems;

namespace Koneko;
public class Patches
{
    public static Dictionary<int, bool> grabbing;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "AvatarInstantiated")]
    public static void SetupGrabber(PlayerDescriptor ____playerDescriptor, PlayerAvatarMovementData ____playerAvatarMovementDataCurrent, float ____distance, Animator ____animator)
    {
        Transform LeftHand = ____animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform RightHand = ____animator.GetBoneTransform(HumanBodyBones.RightHand);

        int leftid = LeftHand.GetInstanceID();
        int rightid = RightHand.GetInstanceID();

        if (!LimbGrabber.Grabbers.ContainsKey(leftid))
        {
            if (LimbGrabber.Debug.Value) MelonLogger.Msg("Created new Grabber");
            LimbGrabber.Grabbers.Add(leftid, new LimbGrabber.Grabber(LeftHand, false, -1));
        }
        if (!LimbGrabber.Grabbers.ContainsKey(rightid))
        {
            if (LimbGrabber.Debug.Value) MelonLogger.Msg("Created new Grabber");
            LimbGrabber.Grabbers.Add(rightid, new LimbGrabber.Grabber(RightHand, false, -1));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "Update")]
    public static void UpdateGrabber(PlayerDescriptor ____playerDescriptor, PlayerAvatarMovementData ____playerAvatarMovementDataCurrent, float ____distance, Animator ____animator)
    {
        if (____distance > 10 || !Friends.FriendsWith(____playerDescriptor.ownerId) && LimbGrabber.Friend.Value || ____animator == null) return;

        Transform LeftHand = ____animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform RightHand = ____animator.GetBoneTransform(HumanBodyBones.RightHand);

        int leftid = LeftHand.GetInstanceID();
        int rightid = RightHand.GetInstanceID();

        if (!grabbing.ContainsKey(leftid)) grabbing.Add(leftid, false);
        if (!grabbing.ContainsKey(rightid)) grabbing.Add(rightid, false);

        if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureLeft == 1 && !grabbing[leftid] || ____playerAvatarMovementDataCurrent.LeftMiddleCurl > 0.5 && !grabbing[leftid])
        {
            LimbGrabber.Grab(leftid, LeftHand);
            grabbing[leftid] = true;
        }
        else if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureLeft != 1 && ____playerAvatarMovementDataCurrent.LeftMiddleCurl < 0.5 && grabbing[leftid])
        {
            LimbGrabber.Release(leftid);
            grabbing[leftid] = false;
        }
        if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureRight == 1 && !grabbing[rightid] || ____playerAvatarMovementDataCurrent.RightMiddleCurl > 0.5 && !grabbing[rightid])
        {
            LimbGrabber.Grab(rightid, RightHand);
            grabbing[rightid] = true;
        }
        else if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureRight != 1 && ____playerAvatarMovementDataCurrent.RightMiddleCurl < 0.5 && grabbing[rightid])
        {
            LimbGrabber.Release(rightid);
            grabbing[rightid] = false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), "Calibrate")]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatar")]
    public static void LimbSetup()
    {
        LimbGrabber.Limb limb = new LimbGrabber.Limb();
        Animator animator = PlayerSetup.Instance._animator;
        IKSolverVR solver = PlayerSetup.Instance._avatar.GetComponent<VRIK>().solver;
        LimbGrabber.IKSolver = solver;

        LimbGrabber.tracking[0] = BodySystem.TrackingLeftArmEnabled;
        LimbGrabber.tracking[1] = BodySystem.TrackingLeftLegEnabled;
        LimbGrabber.tracking[2] = BodySystem.TrackingRightArmEnabled;
        LimbGrabber.tracking[3] = BodySystem.TrackingRightLegEnabled;
        LimbGrabber.tracking[4] = solver.spine.positionWeight != 0;
        LimbGrabber.tracking[5] = solver.spine.pelvisPositionWeight != 0;

        LimbGrabber.Limbs[0].limb = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        LimbGrabber.Limbs[0].PreviousTarget = solver.leftArm.target;
        LimbGrabber.Limbs[1].limb = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        LimbGrabber.Limbs[1].PreviousTarget = solver.leftLeg.target;
        LimbGrabber.Limbs[2].limb = animator.GetBoneTransform(HumanBodyBones.RightHand);
        LimbGrabber.Limbs[2].PreviousTarget = solver.rightArm.target;
        LimbGrabber.Limbs[3].limb = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        LimbGrabber.Limbs[3].PreviousTarget = solver.rightLeg.target;
        LimbGrabber.Limbs[4].limb = animator.GetBoneTransform(HumanBodyBones.Head);
        LimbGrabber.Limbs[4].PreviousTarget = solver.spine.headTarget;
        LimbGrabber.Limbs[5].limb = animator.GetBoneTransform(HumanBodyBones.Hips);
        LimbGrabber.Limbs[5].PreviousTarget = solver.spine.pelvisTarget;
        LimbGrabber.Initialized = true;
    }
}
