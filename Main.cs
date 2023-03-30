using System.Collections.Generic;
using System;
using UnityEngine;
using MelonLoader;
using HarmonyLib;
using RootMotion.FinalIK;
using ABI_RC.Core.Player;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Systems.IK.SubSystems;

[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonInfo(typeof(Koneko.LimbGrabber), "LimbGrabber", "1.0.0", "Exterrata")]

namespace Koneko;
public class LimbGrabber : MelonMod
{
    public static readonly MelonPreferences_Category Category = MelonPreferences.CreateCategory("LimbGrabber");
    public static readonly MelonPreferences_Entry<bool> Enabled = Category.CreateEntry<bool>("Enabled", true);
    public static readonly MelonPreferences_Entry<bool> EnableHands = Category.CreateEntry<bool>("EnableHands", true);
    public static readonly MelonPreferences_Entry<bool> EnableFeet = Category.CreateEntry<bool>("EnableFeet", true);
    public static readonly MelonPreferences_Entry<bool> EnableHead = Category.CreateEntry<bool>("EnableHead", true);
    public static readonly MelonPreferences_Entry<bool> EnableHip = Category.CreateEntry<bool>("EnableHip", true);
    public static readonly MelonPreferences_Entry<bool> EnableRoot = Category.CreateEntry<bool>("EnableRoot", true);
    public static readonly MelonPreferences_Entry<bool> PreserveMomentum = Category.CreateEntry<bool>("PreserveMomentum", true);
    public static readonly MelonPreferences_Entry<bool> CameraFollow = Category.CreateEntry<bool>("CameraFollowHead", false);
    public static readonly MelonPreferences_Entry<bool> Friend = Category.CreateEntry<bool>("FriendsOnly", true);
    public static readonly MelonPreferences_Entry<bool> Debug = Category.CreateEntry<bool>("Debug", false);
    public static readonly MelonPreferences_Entry<float> Distance = Category.CreateEntry<float>("Distance", 0.15f);

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
    public static Transform PlayerLocal;

    public static IKSolverVR IKSolver;
    public static bool Initialized;
    public int count = 0;

    public struct Limb
    {
        public Transform limb;
        public Transform Parent;
        public Transform Target;
        public Transform PreviousTarget;
        public Quaternion RotationOffset;
        public Vector3 PositionOffset;
        public bool Grabbed;
    }

    public class Grabber
    {
        public Transform grabber;
        public int limb;
        public Grabber(Transform grabber, bool grabbing, int limb)
        {
            this.grabber = grabber;
            this.limb = limb;
        }
    }

    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("Starting");
        tracking = new bool[6];
        Grabbers = new Dictionary<int, Grabber>();
        enabled = new MelonPreferences_Entry<bool>[7] {
            EnableHands,
            EnableFeet,
            EnableHands,
            EnableFeet,
            EnableHead,
            EnableHip,
            EnableRoot
        };
        Patches.grabbing = new Dictionary<int, bool>();
        HarmonyInstance.PatchAll(typeof(Patches));
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        if (buildIndex == 3)
        {
            Limbs = new Limb[6];
            PlayerLocal = GameObject.Find("_PLAYERLOCAL").transform;
            for (int i = 0; i < Limbs.Length; i++)
            {
                var limb = new GameObject("LimbGrabberTarget").transform;
                Limbs[i].Target = limb;
                limb.parent = PlayerLocal;
            }
        }
    }

    public override void OnUpdate()
    {
        if (!Initialized || !Enabled.Value) return;
        if (count == 1000)
        {
            count = 0;
            List<int> remove = new List<int>();
            foreach (KeyValuePair<int, Grabber> grabber in Grabbers)
            {
                if (grabber.Value.grabber == null) remove.Add(grabber.Key);
            }
            foreach (int key in remove)
            {
                Grabbers.Remove(key);
            }
        }
        count++;
        for (int i = 0; i < Limbs.Length; i++)
        {
            if (Limbs[i].Grabbed && Limbs[i].Parent != null)
            {
                Vector3 offset = Limbs[i].Parent.rotation * Limbs[i].PositionOffset;
                Limbs[i].Target.position = Limbs[i].Parent.position + offset;
                Limbs[i].Target.rotation = Limbs[i].Parent.rotation * Limbs[i].RotationOffset;
            }
        }
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
            if (!enabled[closest].Value) return;
            Grabbers[id].limb = closest;
            if (Debug.Value) MelonLogger.Msg("limb " + Limbs[closest].limb.name + " was grabbed by " + parent.name);
            Limbs[closest].PositionOffset = Quaternion.Inverse(parent.rotation) * (Limbs[closest].limb.position - parent.position);
            Limbs[closest].RotationOffset = Quaternion.Inverse(parent.rotation) * Limbs[closest].limb.rotation;
            Limbs[closest].Parent = parent;
            Limbs[closest].Grabbed = true;
            SetTarget(closest, Limbs[closest].Target);
            SetTracking(closest, true);
        }
    }

    public static void Release(int id)
    {
        int grabber = Grabbers[id].limb;
        if (grabber == -1) return;
        if (Debug.Value) MelonLogger.Msg("limb " + Limbs[grabber].limb.name + " was released by " + Grabbers[id].grabber.name);
        Limbs[grabber].Grabbed = false;
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

        if(!grabbing.ContainsKey(leftid)) grabbing.Add(leftid, false);
        if(!grabbing.ContainsKey(rightid)) grabbing.Add(rightid, false);

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