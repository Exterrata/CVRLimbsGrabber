using UnityEngine;
using MelonLoader;
using HarmonyLib;
using System;
using System.Collections.Generic;
using RootMotion.FinalIK;
using ABI.CCK.Components;
using ABI_RC.Core.Player;
using ABI_RC.Systems.IK.SubSystems;
using ABI_RC.Core.Networking.IO.Social;

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
    public static readonly MelonPreferences_Entry<float> Distance = Category.CreateEntry<float>("Distance", 0.1f);

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

    public struct Limb
    {
        public Transform limb;
        public Transform Target;
        public Transform PreviousTarget;
        public Limb(Limb limb)
        {
            this.limb = limb.limb;
            Target = limb.Target;
            PreviousTarget = limb.PreviousTarget;
        }
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
            Limbs[i].limb = new GameObject().transform;
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
        if (!Initialized) return;
        for (int i = 0; i < Limbs.Length; i++)
        {
            if (Limbs[i].Target == null)
            {
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
            for (int j = 0; j < 6; j++)
            {
                if (enabled[j].Value)
                {
                    if ((int)Grabbers[i].animator.GetFloat(Grabbers[i].parameter) == 1 && !Grabbers[i].WasGrabbing)
                    {
                        if(j == 5) Grabbers[i].WasGrabbing = true;
                        //grab limb
                        if (Vector3.Distance(Grabbers[i].transform.position, Limbs[j].limb.position) < Distance.Value && Grabbers[i].Limb == -1)
                        {
                            Limbs[j].Target.position = Limbs[j].limb.position;
                            Limbs[j].Target.rotation = Limbs[j].limb.rotation;
                            Limbs[j].Target.parent = Grabbers[i].transform;
                            Grabbers[i].Limb = j;
                            SetTarget(j, Limbs[j].Target);
                            SetTracking(j, true);
                        }
                    } else if ((int)Grabbers[i].animator.GetFloat(Grabbers[i].parameter) != 1 && Grabbers[i].WasGrabbing)
                    {
                        //release limb
                        if (j == 5) Grabbers[i].WasGrabbing = false;
                        if (Grabbers[i].Limb == j)
                        {
                            Grabbers[i].Limb = -1;
                            SetTarget(j, Limbs[j].PreviousTarget);
                            if (!tracking[j]) SetTracking(j, false);
                        }
                    }
                }
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
    [HarmonyPatch(typeof(CVRAvatar), "Start")]
    public static void LimbSetup(CVRAvatar __instance)
    {
        if (__instance.gameObject.layer != 8) return;
        LimbGrabber.Limb limb = new LimbGrabber.Limb();
        Animator animator = __instance.gameObject.GetComponent<Animator>();
        IKSolverVR solver = __instance.gameObject.GetComponent<VRIK>().solver;
        LimbGrabber.IKSolver = solver;

        LimbGrabber.tracking[0] = BodySystem.TrackingLeftArmEnabled;
        LimbGrabber.tracking[1] = BodySystem.TrackingLeftLegEnabled;
        LimbGrabber.tracking[2] = BodySystem.TrackingRightArmEnabled;
        LimbGrabber.tracking[3] = BodySystem.TrackingRightLegEnabled;
        LimbGrabber.tracking[4] = solver.spine.positionWeight != 0;
        LimbGrabber.tracking[5] = solver.spine.pelvisPositionWeight != 0;

        limb.limb = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        limb.PreviousTarget = solver.leftArm.target;
        LimbGrabber.Limbs[0] = new LimbGrabber.Limb(limb);
        limb.limb = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        limb.PreviousTarget = solver.leftLeg.target;
        LimbGrabber.Limbs[1] = new LimbGrabber.Limb(limb);
        limb.limb = animator.GetBoneTransform(HumanBodyBones.RightHand);
        limb.PreviousTarget = solver.rightArm.target;
        LimbGrabber.Limbs[2] = new LimbGrabber.Limb(limb);
        limb.limb = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        limb.PreviousTarget = solver.rightLeg.target;
        LimbGrabber.Limbs[3] = new LimbGrabber.Limb(limb);
        limb.limb = animator.GetBoneTransform(HumanBodyBones.Head);
        limb.PreviousTarget = solver.spine.headTarget;
        LimbGrabber.Limbs[4] = new LimbGrabber.Limb(limb);
        limb.limb = animator.GetBoneTransform(HumanBodyBones.Hips);
        limb.PreviousTarget = solver.spine.pelvisTarget;
        LimbGrabber.Limbs[5] = new LimbGrabber.Limb(limb);
        LimbGrabber.Initialized = true;
    }
}