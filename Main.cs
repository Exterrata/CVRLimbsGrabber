using System.Collections.Generic;
using System;
using UnityEngine;
using MelonLoader;
using HarmonyLib;
using RootMotion.FinalIK;
using ABI_RC.Core.Player;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Systems.IK;
using ABI.CCK.Components;
using System.Linq;

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonInfo(typeof(Koneko.LimbGrabber), "LimbGrabber", "1.0.0", "Exterrata")]

namespace Koneko;
public class LimbGrabber : MelonMod
{
    public static readonly MelonPreferences_Category Category = MelonPreferences.CreateCategory("LimbGrabber");
    public static readonly MelonPreferences_Entry<bool> Enabled = Category.CreateEntry<bool>("Enabled", true);
    public static readonly MelonPreferences_Entry<bool> Friend = Category.CreateEntry<bool>("FriendsOnly", true);
    public static readonly MelonPreferences_Entry<bool> EnableLeftHand = Category.CreateEntry<bool>("EnableLeftHand", true);
    public static readonly MelonPreferences_Entry<bool> EnableLeftFoot = Category.CreateEntry<bool>("EnableLeftFoot", true);
    public static readonly MelonPreferences_Entry<bool> EnableRightHand = Category.CreateEntry<bool>("EnableRightHand", true);
    public static readonly MelonPreferences_Entry<bool> EnableRightFoot = Category.CreateEntry<bool>("EnableRightFoot", true);
    public static readonly MelonPreferences_Entry<bool> EnableHead = Category.CreateEntry<bool>("EnableHead", true);
    public static readonly MelonPreferences_Entry<bool> EnableHip = Category.CreateEntry<bool>("EnableHip", true);
    public static readonly MelonPreferences_Entry<bool> EnableRoot = Category.CreateEntry<bool>("EnableRoot", true);
    public static readonly MelonPreferences_Entry<float> Distance = Category.CreateEntry<float>("Distance", 0.15f);
    public static readonly MelonPreferences_Entry<bool> Debug = Category.CreateEntry<bool>("Debug", true);

//  LeftHand = 0
//  LeftFoot = 1
//  RightHand = 2
//  RightFoot = 3
//  Head = 4
//  Hip = 5
//  Root = 6
    public static readonly string[] LimbNames = { "LeftHand", "LeftFoot", "RightHand", "RightFoot", "Head", "Hip", "Root" };
    public static MelonPreferences_Entry<bool>[] enabled;
    public static bool[] tracking;
    public static Limb[] Limbs;
    public static Dictionary<int, Grabber> Grabbers;

    public static IKSolverVR IKSolver;
    public static bool Initialized;
    public int count = 0;

    public struct Limb
    {
        public Transform limb;
        public Transform Target;
        public Transform PreviousTarget;
    }

    public class Grabber
    {
        public Transform grabber;
        public bool grabbing;
        public int limb;
        public Grabber(Transform grabber, bool grabbing, int limb)
        {
            this.grabber = grabber;
            this.grabbing = grabbing;
            this.limb = limb;
        }
    }

    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Starting");
        Limbs = new Limb[7];
        tracking = new bool[7];
        Grabbers = new Dictionary<int, Grabber>();
        for (int i = 0;i < 7; i++)
        {
            Limbs[i].Target = new GameObject().transform;
        }
        enabled = new MelonPreferences_Entry<bool>[7] {
            EnableLeftHand,
            EnableLeftFoot,
            EnableRightHand,
            EnableRightFoot,
            EnableHead,
            EnableHip,
            EnableRoot
        };
        HarmonyInstance.PatchAll(typeof(Patches));
    }

    public override void OnUpdate()
    {
        if (!Initialized || !Enabled.Value) return;
        for (int i = 0; i < Limbs.Length; i++)
        {
            if (Limbs[i].Target == null)
            {
                if (Debug.Value) MelonLogger.Msg("Limb " + LimbNames[i] + " was destroyed. regenerating");
                Limbs[i].Target = new GameObject("LimbGrabberTarget").transform;
                SetTarget(i, Limbs[i].PreviousTarget);
                if (!tracking[i]) SetTracking(i, false);
            }
        }
        if (count == 1000)
        {
            List<int> remove = new List<int>();
            count = 0;
            for (int i = 0; i < Grabbers.Count; i++)
            {
                KeyValuePair<int, Grabber> grabber = Grabbers.ElementAt(i);
                if (grabber.Value.grabber == null)
                {
                    if (Debug.Value) MelonLogger.Msg("Grabber no long exists. removing");
                    Grabbers.Remove(grabber.Key);
                }
            }
        }
        count++;
    }

    public static void Grab(int id, Transform parent)
    {
        if (!Enabled.Value) return;
        if (Debug.Value) MelonLogger.Msg("grab was detected");
        int closest = 0;
        float distance = float.PositiveInfinity;
        for (int i = 0; i < 6; i++)
        {
            float dist = Vector3.Distance(parent.position, Limbs[i].limb.position);
            if (dist < distance)
            {
                distance = dist;
                closest = i;
            }
        }
        if (distance < Distance.Value)
        {
            Grabbers[id].limb = closest;

            if (Debug.Value) MelonLogger.Msg("limb " + Limbs[closest].limb.name + " was grabbed by " + parent.name);
            Limbs[closest].Target.position = Limbs[closest].limb.position;
            Limbs[closest].Target.rotation = Limbs[closest].limb.rotation;
            Limbs[closest].Target.parent = parent;
            SetTarget(closest, Limbs[closest].Target);
            SetTracking(closest, true);
        }
    }

    public static void Release(int id)
    {
        int grabber = Grabbers[id].limb;
        if (Debug.Value) MelonLogger.Msg("limb " + Limbs[grabber].limb.name + " was released by " + Grabbers[id].grabber.name);
        SetTarget(grabber, Limbs[grabber].PreviousTarget);
        if (!tracking[grabber]) SetTracking(grabber, false);
    }

    public static void SetTarget(int index, Transform Target)
    {
        switch (index)
        {
            case 0:
                IKSolver.leftArm.target = Target;
                break;
            case 1:
                IKSolver.leftLeg.target = Target;
                break;
            case 2:
                IKSolver.rightArm.target = Target;
                break;
            case 3:
                IKSolver.rightLeg.target = Target;
                break;
            case 4:
                IKSolver.spine.headTarget = Target;
                break;
            case 5:
                IKSolver.spine.pelvisTarget = Target;
                break;
        }
    }

    public static void SetTracking(int index, bool value)
    {
        switch (index)
        {
            case 0: 
                BodySystem.TrackingLeftArmEnabled = value;
                break;
            case 1:
                BodySystem.TrackingLeftLegEnabled = value;
                break;
            case 2:
                BodySystem.TrackingRightArmEnabled = value;
                break;
            case 3:
                BodySystem.TrackingRightLegEnabled = value;
                break;
            case 4:
                IKSolver.spine.positionWeight = value ? 1 : 0;
                break;
            case 5:
                IKSolver.spine.pelvisPositionWeight = value ? 1 : 0;
                break;
        }
    }
}
public class Patches
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PuppetMaster), "Update")]
    public static void UpdateGrabber(PlayerDescriptor ____playerDescriptor, PlayerAvatarMovementData ____playerAvatarMovementDataCurrent, float ____distance, Animator ____animator)
    {
        if (____distance > 10 || !Friends.FriendsWith(____playerDescriptor.ownerId) && LimbGrabber.Friend.Value) return;

        Transform LeftHand = ____animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform RightHand = ____animator.GetBoneTransform(HumanBodyBones.RightHand);

        int leftid = LeftHand.GetInstanceID();
        int rightid = RightHand.GetInstanceID();

        bool LeftExists = LimbGrabber.Grabbers.TryGetValue(leftid, out LimbGrabber.Grabber LeftGrab);
        if (!LeftExists)
        {
            if (LimbGrabber.Debug.Value) MelonLogger.Msg("Created new Grabber");
            LeftGrab = new LimbGrabber.Grabber(LeftHand, false, -1);
            LimbGrabber.Grabbers.Add(leftid, LeftGrab);
        }

        bool RightExists = LimbGrabber.Grabbers.TryGetValue(rightid, out LimbGrabber.Grabber RightGrab);
        if (!RightExists)
        {
            if (LimbGrabber.Debug.Value) MelonLogger.Msg("Created new Grabber");
            RightGrab = new LimbGrabber.Grabber(RightHand, false, -1);
            LimbGrabber.Grabbers.Add(rightid, RightGrab);
        }

        bool grabLeft = LeftGrab.grabbing;
        bool grabRight = RightGrab.grabbing;

        if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureLeft == 1 && !grabLeft || ____playerAvatarMovementDataCurrent.LeftMiddleCurl > 0.5 && !grabLeft)
        {
            LimbGrabber.Grab(leftid, LeftHand);
            grabLeft = true;
        } else if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureLeft != 1 && ____playerAvatarMovementDataCurrent.LeftMiddleCurl < 0.5 && grabLeft)
        {
            LimbGrabber.Release(leftid);
            grabLeft = false;
        }
        if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureRight == 1 && !grabRight || ____playerAvatarMovementDataCurrent.RightMiddleCurl > 0.5 && !grabRight)
        {
            LimbGrabber.Grab(rightid, RightHand);
            grabRight = true;
        } else if ((int)____playerAvatarMovementDataCurrent.AnimatorGestureRight != 1 && ____playerAvatarMovementDataCurrent.RightMiddleCurl < 0.5 && grabRight)
        {
            LimbGrabber.Release(rightid);
            grabRight = false;
        }

        LimbGrabber.Grabbers[leftid].grabbing = grabLeft;
        LimbGrabber.Grabbers[rightid].grabbing = grabRight;
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

        limb.limb = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        limb.PreviousTarget = solver.leftArm.target;
        LimbGrabber.Limbs[0] = limb;
        limb.limb = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        limb.PreviousTarget = solver.leftLeg.target;
        LimbGrabber.Limbs[1] = limb;
        limb.limb = animator.GetBoneTransform(HumanBodyBones.RightHand);
        limb.PreviousTarget = solver.rightArm.target;
        LimbGrabber.Limbs[2] = limb;
        limb.limb = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        limb.PreviousTarget = solver.rightLeg.target;
        LimbGrabber.Limbs[3] = limb;
        limb.limb = animator.GetBoneTransform(HumanBodyBones.Head);
        limb.PreviousTarget = solver.spine.headTarget;
        LimbGrabber.Limbs[4] = limb;
        limb.limb = animator.GetBoneTransform(HumanBodyBones.Hips);
        limb.PreviousTarget = solver.spine.pelvisTarget;
        LimbGrabber.Limbs[5] = limb;
        LimbGrabber.Initialized = true;
    }
}