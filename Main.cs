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

    /*
    LeftHand = 0
    LeftFoot = 1
    RightHand = 2
    RightFoot = 3
    Head = 4
    Hip = 5
    Root = 6
    */
    public static MelonPreferences_Entry<bool>[] enabled;
    public static bool[] tracking;

    public static Limb[] Limbs;

    public static List<Grabber> Grabbers;

    public static IKSolverVR IKSolver;

    public static bool Initialized;

    public static readonly string[] LimbNames = { "LeftHand", "LeftFoot", "RightHand", "RightFoot", "Head", "Hip", "Root" };

    public struct Limb
    {
        public Transform limb;
        public Transform Target;
        public Transform PreviousTarget;
    }
    public class Grabber
    {
        public Transform transform;
        public Animator animator;
        public int parameter;
        public bool Friend;
        public bool WasGrabbing;
        public int Limb;
    }

    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Starting");
        Limbs = new Limb[7];
        Grabbers = new List<Grabber>();
        tracking = new bool[7];
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
        for (int i = 0; i < Grabbers.Count; i++)
        {
            if (Grabbers[i].transform == null) 
            {
                Grabbers.Remove(Grabbers[i]);
                continue;
            }
            if (Friend.Value && !Grabbers[i].Friend) continue;
            if ((int)Grabbers[i].animator.GetFloat(Grabbers[i].parameter) == 1 && !Grabbers[i].WasGrabbing)
            {
                if (Debug.Value) MelonLogger.Msg("grab was detected");
                int closest = 0;
                float distance = float.PositiveInfinity;
                for (int j = 0; j < 6; j++)
                {
                    float dist = Vector3.Distance(Grabbers[i].transform.position, Limbs[j].limb.position);
                    if(dist < distance)
                    {
                        closest = j;
                        distance = dist;
                    }
                }
                if (distance < Distance.Value)
                {
                    if (Debug.Value) MelonLogger.Msg("limb " + Limbs[closest].limb.name + " was grabbed by " + Grabbers[i].transform.name);
                    Limbs[closest].Target.position = Limbs[closest].limb.position;
                    Limbs[closest].Target.rotation = Limbs[closest].limb.rotation;
                    Limbs[closest].Target.parent = Grabbers[i].transform;
                    Grabbers[i].Limb = closest;
                    SetTarget(closest, Limbs[closest].Target);
                    SetTracking(closest, true);
                }
                Grabbers[i].WasGrabbing = true;
            }
            else if ((int)Grabbers[i].animator.GetFloat(Grabbers[i].parameter) != 1 && Grabbers[i].WasGrabbing)
            {
                if (Grabbers[i].Limb != -1)
                {
                    int limb = Grabbers[i].Limb;
                    if (Debug.Value) MelonLogger.Msg("limb " + Limbs[limb].limb.name + " was released by " + Grabbers[i].transform.name);
                    Grabbers[i].Limb = -1;
                    SetTarget(limb, Limbs[limb].PreviousTarget);
                    if (!tracking[limb]) SetTracking(limb, false);
                }
                Grabbers[i].WasGrabbing = false;
            }
        }
    }

    public void SetTarget(int index, Transform Target)
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

    public void SetTracking(int index, bool value)
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
    [HarmonyPatch(typeof(PuppetMaster), "AvatarInstantiated")]
    public static void GrabberSetup(PlayerDescriptor ____playerDescriptor, Animator ____animator)
    {
        LimbGrabber.Grabber grabber = new LimbGrabber.Grabber();
        grabber.Friend = Friends.FriendsWith(____playerDescriptor.ownerId);
        grabber.animator = ____animator;
        grabber.Limb = -1;
        grabber.transform = ____animator.GetBoneTransform(HumanBodyBones.LeftHand);
        grabber.parameter = Animator.StringToHash("GestureLeft");
        LimbGrabber.Grabbers.Add(grabber);
        grabber = new LimbGrabber.Grabber();
        grabber.Friend = Friends.FriendsWith(____playerDescriptor.ownerId);
        grabber.animator = ____animator;
        grabber.Limb = -1;
        grabber.transform = ____animator.GetBoneTransform(HumanBodyBones.RightHand);
        grabber.parameter = Animator.StringToHash("GestureRight");
        LimbGrabber.Grabbers.Add(grabber);
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