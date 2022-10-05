#if (UNITY_EDITOR)

using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEditor.Animations;

public class CreateAnimations : EditorWindow {
    private static readonly int IdleSpeed = 8;
    private static readonly int WalkSpeed = 8;

    [MenuItem("Tools/Create Animations")]
    public static void Run() {
        //GetWindow<CreateAnimations>("Create Animations");
        Create("Bat", false);
    }

    private void OnGUI() {
        if (GUILayout.Button("Refresh clips")) {
            DirectoryInfo[] infos = new DirectoryInfo($"Assets/Resources/Characters").GetDirectories();
            foreach (DirectoryInfo info in infos) {
                Create(info.Name, true);
            }
        }

        if (GUILayout.Button("Refresh animators (It will remove them for all objects!)")) {
            DirectoryInfo[] infos = new DirectoryInfo($"Assets/Resources/Characters").GetDirectories();
            foreach (DirectoryInfo info in infos) {
                Create(info.Name, false);
            }
        }
    }

    private static void Create(string name, bool skipAnimator) {
        AnimationClip idleClip = CreateIdleAnimation(name);
        AnimationClip walkClip = CreateWalkAnimation(name);

        if (skipAnimator) { return; }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>($"Assets/Animations/Characters/" + name + "/" + name + ".controller");

        if (controller == null) {
            controller = AnimatorController.CreateAnimatorControllerAtPath($"Assets/Animations/Characters/" + name + "/" + name + ".controller");
            SetupAnimatorController(controller, idleClip, walkClip);
            CreateOrReplaceAsset(controller, $"Assets/Animations/Characters/" + name + "/" + name + ".controller");
            AssetDatabase.SaveAssets();
        } else {
            Directory.CreateDirectory($"Assets/Tmp");
            controller = AnimatorController.CreateAnimatorControllerAtPath($"Assets/Tmp/" + name + "-tmp.controller");
            SetupAnimatorController(controller, idleClip, walkClip);
            CreateOrReplaceAsset(controller, $"Assets/Animations/Characters/" + name + "/" + name + ".controller");
            controller.name = name;
            AssetDatabase.SaveAssets();
        }
    }

    private static void SetupAnimatorController(AnimatorController controller, AnimationClip idleClip, AnimationClip walkClip) {
        controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);

        AnimatorState idleState = controller.AddMotion(idleClip);
        AnimatorState walkState = controller.AddMotion(walkClip);

        AnimatorStateTransition idleToWalkTransition = idleState.AddTransition(walkState);
        idleToWalkTransition.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
        idleToWalkTransition.hasExitTime = false;
        idleToWalkTransition.hasFixedDuration = true;
        idleToWalkTransition.duration = 0;

        AnimatorStateTransition walkToIdleTransition = walkState.AddTransition(idleState);
        walkToIdleTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
        walkToIdleTransition.hasExitTime = false;
        walkToIdleTransition.hasFixedDuration = true;
        walkToIdleTransition.duration = 0;
    }

    private static AnimationClip CreateIdleAnimation(string name) {
        AnimationClip clip = new() {
            name = name + "-Idle",
            wrapMode = WrapMode.Loop,
            frameRate = 60,
        };
        AnimationClipSettings clipSettings =  AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        EditorCurveBinding spriteBinding = new() {
            type = typeof(SpriteRenderer),
            path = "Sprites/Body",
            propertyName = "m_Sprite",
        };
        Sprite[] sprites = Resources.LoadAll<Sprite>("Characters/" + name + "/Idle");

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++) {
            keyFrames[i] = new ObjectReferenceKeyframe() {
                time = FrameToSeconds(clip, i * IdleSpeed),
                value = sprites[i]
            };
        }
        keyFrames[sprites.Length] = new ObjectReferenceKeyframe() {
            time = FrameToSeconds(clip, sprites.Length * IdleSpeed),
            value = sprites[0]
        };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

        Directory.CreateDirectory($"Assets/Animations/Characters/" + name + "/Clips");

        CreateOrReplaceAsset(clip, $"Assets/Animations/Characters/" + name + "/Clips/" + name + "-Idle.anim");
        AssetDatabase.SaveAssets();

        return clip;
    }

    private static AnimationClip CreateChargingAnimation(string name) {
        AnimationClip clip = new() {
            name = name + "-Charging",
            wrapMode = WrapMode.Loop,
            frameRate = 60,
        };
        AnimationClipSettings clipSettings =  AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        Keyframe[] keyFrames = new Keyframe[] {
            new Keyframe(FrameToSeconds(clip, 0), 0),
            new Keyframe(FrameToSeconds(clip, 2), 1),
            new Keyframe(FrameToSeconds(clip, 6), -1),
            new Keyframe(FrameToSeconds(clip, 8), 0),
        };
        clip.SetCurve("", typeof(RectTransform), "localPosition.x", new AnimationCurve(keyFrames));

        Directory.CreateDirectory($"Assets/Animations/Characters/" + name + "/Clips");

        CreateOrReplaceAsset(clip, $"Assets/Animations/Characters/" + name + "/Clips/" + name + "-Charging.anim");
        AssetDatabase.SaveAssets();

        return clip;
    }

    private static AnimationClip CreateWalkAnimation(string name) {
        AnimationClip clip = new() {
            name = name + "-Walk",
            wrapMode = WrapMode.Loop,
            frameRate = 60,
        };
        AnimationClipSettings clipSettings =  AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        EditorCurveBinding spriteBinding = new() {
            type = typeof(SpriteRenderer),
            path = "Sprites/Body",
            propertyName = "m_Sprite"
        };
        Sprite[] sprites = Resources.LoadAll<Sprite>("Characters/" + name + "/Walk");

        ObjectReferenceKeyframe[] keyFrames = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < sprites.Length; i++) {
            keyFrames[i] = new ObjectReferenceKeyframe() {
                time = FrameToSeconds(clip, i * WalkSpeed),
                value = sprites[i]
            };
        }
        keyFrames[sprites.Length] = new ObjectReferenceKeyframe() {
            time = FrameToSeconds(clip, sprites.Length * WalkSpeed),
            value = sprites[0]
        };
        AnimationUtility.SetObjectReferenceCurve(clip, spriteBinding, keyFrames);

        Directory.CreateDirectory($"Assets/Animations/Characters/" + name + "/Clips");

        CreateOrReplaceAsset(clip, $"Assets/Animations/Characters/" + name + "/Clips/" + name + "-Walk.anim");
        AssetDatabase.SaveAssets();

        return clip;
    }

    private static float FrameToSeconds(AnimationClip clip, float frame) {
        return frame / clip.frameRate;
    }

    private static T CreateOrReplaceAsset<T>(T asset, string path) where T : Object {
        T existingAsset = AssetDatabase.LoadAssetAtPath<T>(path);

        if (existingAsset == null) {
            AssetDatabase.CreateAsset(asset, path);
            existingAsset = asset;
        } else {
            EditorUtility.CopySerialized(asset, existingAsset);
        }

        return existingAsset;
    }

    private static void CopyAsset<T>(string src, string dst) where T : Object {
        T srcAsset = AssetDatabase.LoadAssetAtPath<T>(src);
        T existingAsset = AssetDatabase.LoadAssetAtPath<T>(dst);

        if (existingAsset == null) {
            AssetDatabase.CopyAsset(src, dst);
        } else {
            EditorUtility.CopySerialized(srcAsset, existingAsset);
        }
    }
}

#endif