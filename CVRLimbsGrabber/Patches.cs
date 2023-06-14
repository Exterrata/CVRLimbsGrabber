using System.Collections.Generic;
using System;
using UnityEngine;
using HarmonyLib;
using RootMotion.FinalIK;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK.SubSystems;
using MelonLoader;

namespace Koneko;
public class Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "AvatarInstantiated")]
    public static void SetupGrabber(ref PlayerDescriptor ____playerDescriptor, ref PlayerAvatarMovementData ____playerAvatarMovementDataCurrent, ref Animator ____animator)
    {
        try
        {
            if (!____animator.isHuman) return;
            Transform LeftHand = ____animator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (LeftHand == null) return;
            GrabberComponent LeftGrabber = LeftHand.gameObject.AddComponent<GrabberComponent>();
            LeftGrabber.MovementData = ____playerAvatarMovementDataCurrent;
            LeftGrabber.PlayerDescriptor = ____playerDescriptor;
            LeftGrabber.grabber = 1;

            Transform RightHand = ____animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (RightHand == null) return;
            GrabberComponent RightGrabber = RightHand.gameObject.AddComponent<GrabberComponent>();
            RightGrabber.MovementData = ____playerAvatarMovementDataCurrent;
            RightGrabber.PlayerDescriptor = ____playerDescriptor;
            RightGrabber.grabber = 2;
        } catch (Exception e)
        {
            MelonLogger.Error(e);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BodySystem), "Calibrate")]
    [HarmonyPatch(typeof(PlayerSetup), "SetupAvatar")]
    public static void LimbSetup()
    {
        try
        {
            Animator animator = PlayerSetup.Instance._animator;
            VRIK vrik = PlayerSetup.Instance._avatar.GetComponent<VRIK>();
            if (vrik == null)
            {
                LimbGrabber.Initialized = false;
                return;
            }
            IKSolverVR solver = vrik.solver;
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
            LimbGrabber.Neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            LimbGrabber.Initialized = true;
        } catch (Exception e)
        {
            MelonLogger.Error(e);
        }
    }
}
