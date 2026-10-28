using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class PlayerAnimationSetup
{
    private const string AnimationFolder = "Assets/Animations/Player";
    private const string ControllerPath = AnimationFolder + "/PlayerAnimator.controller";
    private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
    private const string MushroomAnimationFolder = "Assets/Animations/Mushroom";
    private const string MushroomControllerPath = MushroomAnimationFolder + "/MushroomAnimator.controller";
    private const string MushroomPrefabPath = "Assets/Prefabs/Mushroom.prefab";
    private const string MainLevelPath = "Assets/Scenes/MainLevel.unity";
    private const string LevelOnePath = "Assets/Scenes/Level 1.unity";
    private const string LevelTwoPath = "Assets/Scenes/Level 2.unity";
    private const string EndLevelPath = "Assets/Scenes/EndLevel.unity";

    static PlayerAnimationSetup()
    {
        EditorApplication.delayCall += EnsurePlayerAnimations;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += EnsurePlayerAnimations;
        }
    }

    [MenuItem("Tools/Setup Player, Mushroom, and Levels")]
    private static void EnsurePlayerAnimations()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        EnsureFolder();

        AnimationClip idle = EnsureSpriteClip("PlayerIdle", 6f, true, new[]
        {
            ("Assets/Sprites/Player/player_idle_0.png", 0f),
            ("Assets/Sprites/Player/player_idle_1.png", 1f / 6f)
        });

        AnimationClip run = EnsureSpriteClip("PlayerRun", 12f, true, new[]
        {
            ("Assets/Sprites/Player/player_run_0.png", 0f),
            ("Assets/Sprites/Player/player_run_1.png", 1f / 12f),
            ("Assets/Sprites/Player/player_run_2.png", 2f / 12f),
            ("Assets/Sprites/Player/player_run_3.png", 3f / 12f),
            ("Assets/Sprites/Player/player_run_4.png", 4f / 12f),
            ("Assets/Sprites/Player/player_run_5.png", 5f / 12f)
        });

        AnimationClip jumpUp = EnsureSpriteClip("PlayerJumpUp", 12f, false, new[]
        {
            ("Assets/Sprites/Player/player_jump_0.png", 0f),
            ("Assets/Sprites/Player/player_jump_0.png", 1f / 12f)
        });

        AnimationClip jumpDown = EnsureSpriteClip("PlayerJumpDown", 12f, false, new[]
        {
            ("Assets/Sprites/Player/player_jump_1.png", 0f),
            ("Assets/Sprites/Player/player_jump_1.png", 1f / 12f)
        });

        AnimatorController controller = EnsureController(idle, run, jumpUp, jumpDown);
        AssignController(controller);
        EnsureMushroomAnimations();
        EnsureProgressionScenes();
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }

        if (!AssetDatabase.IsValidFolder(AnimationFolder))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Player");
        }
    }

    private static AnimationClip EnsureSpriteClip(
        string clipName,
        float frameRate,
        bool loop,
        (string path, float time)[] frames)
    {
        string clipPath = AnimationFolder + "/" + clipName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip != null)
        {
            return clip;
        }

        clip = new AnimationClip { name = clipName, frameRate = frameRate };
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frames[i].path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Could not load animation sprite at {frames[i].path}.");
            }

            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = frames[i].time,
                value = sprite
            };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            "Graphic",
            typeof(SpriteRenderer),
            "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static AnimatorController EnsureController(
        AnimationClip idle,
        AnimationClip run,
        AnimationClip jumpUp,
        AnimationClip jumpDown)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller != null)
        {
            return controller;
        }

        controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("isGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("velocityX", AnimatorControllerParameterType.Int);
        controller.AddParameter("velocityY", AnimatorControllerParameterType.Int);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("PlayerIdle");
        AnimatorState runState = stateMachine.AddState("PlayerRun");
        AnimatorState jumpUpState = stateMachine.AddState("PlayerJumpUp");
        AnimatorState jumpDownState = stateMachine.AddState("PlayerJumpDown");
        idleState.motion = idle;
        runState.motion = run;
        jumpUpState.motion = jumpUp;
        jumpDownState.motion = jumpDown;
        stateMachine.defaultState = idleState;

        AddAnyStateTransition(stateMachine, idleState,
            (AnimatorConditionMode.If, "isGrounded", 0f),
            (AnimatorConditionMode.Equals, "velocityX", 0f));
        AddAnyStateTransition(stateMachine, runState,
            (AnimatorConditionMode.If, "isGrounded", 0f),
            (AnimatorConditionMode.NotEqual, "velocityX", 0f));
        AddAnyStateTransition(stateMachine, jumpUpState,
            (AnimatorConditionMode.IfNot, "isGrounded", 0f),
            (AnimatorConditionMode.Greater, "velocityY", 0f));
        AddAnyStateTransition(stateMachine, jumpDownState,
            (AnimatorConditionMode.IfNot, "isGrounded", 0f),
            (AnimatorConditionMode.Less, "velocityY", 1f));

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    private static void AddAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        params (AnimatorConditionMode mode, string parameter, float threshold)[] conditions)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(destination);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;
        foreach (var condition in conditions)
        {
            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
        }
    }

    private static void AssignController(AnimatorController controller)
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Animator animator = prefabContents.GetComponent<Animator>();
            if (animator == null)
            {
                animator = prefabContents.AddComponent<Animator>();
            }

            bool prefabChanged = false;
            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                prefabChanged = true;
            }

            PlayerController playerController = prefabContents.GetComponent<PlayerController>();
            SerializedObject serializedPlayerController = new SerializedObject(playerController);
            SerializedProperty animatorProperty = serializedPlayerController.FindProperty("animator");
            if (animatorProperty.objectReferenceValue != animator)
            {
                animatorProperty.objectReferenceValue = animator;
                serializedPlayerController.ApplyModifiedPropertiesWithoutUndo();
                prefabChanged = true;
            }

            if (prefabChanged)
            {
                PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void EnsureMushroomAnimations()
    {
        if (!AssetDatabase.IsValidFolder(MushroomAnimationFolder))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Mushroom");
        }

        AnimationClip idle = EnsureSpriteClip(
            MushroomAnimationFolder,
            "MushroomIdle",
            6f,
            true,
            new[]
            {
                ("Assets/Sprites/Mushroom/mushroom_0.png", 0f),
                ("Assets/Sprites/Mushroom/mushroom_1.png", 1f / 6f),
                ("Assets/Sprites/Mushroom/mushroom_2.png", 2f / 6f)
            });

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MushroomControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(MushroomControllerPath);
        }

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = null;
        foreach (ChildAnimatorState childState in stateMachine.states)
        {
            if (childState.state.name == "MushroomIdle")
            {
                idleState = childState.state;
                break;
            }
        }

        if (idleState == null)
        {
            idleState = stateMachine.AddState("MushroomIdle");
        }

        idleState.motion = idle;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssignMushroomController(controller);
    }

    private static AnimationClip EnsureSpriteClip(
        string folder,
        string clipName,
        float frameRate,
        bool loop,
        (string path, float time)[] frames)
    {
        string clipPath = folder + "/" + clipName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip != null)
        {
            return clip;
        }

        clip = new AnimationClip { name = clipName, frameRate = frameRate };
        ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(frames[i].path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Could not load animation sprite at {frames[i].path}.");
            }

            keyframes[i] = new ObjectReferenceKeyframe
            {
                time = frames[i].time,
                value = sprite
            };
        }

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
            "Graphic",
            typeof(SpriteRenderer),
            "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        AssetDatabase.CreateAsset(clip, clipPath);
        return clip;
    }

    private static void AssignMushroomController(AnimatorController controller)
    {
        GameObject prefabContents = PrefabUtility.LoadPrefabContents(MushroomPrefabPath);
        try
        {
            Animator animator = prefabContents.GetComponent<Animator>();
            if (animator == null)
            {
                animator = prefabContents.AddComponent<Animator>();
            }

            if (animator.runtimeAnimatorController != controller)
            {
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(prefabContents, MushroomPrefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabContents);
        }
    }

    private static void EnsureProgressionScenes()
    {
        CopySceneIfMissing(MainLevelPath, LevelOnePath);
        CopySceneIfMissing(LevelOnePath, LevelTwoPath);

        Scene levelTwo = SceneManager.GetSceneByPath(LevelTwoPath);
        bool closeLevelTwoAfterSetup = !levelTwo.IsValid() || !levelTwo.isLoaded;
        if (closeLevelTwoAfterSetup)
        {
            levelTwo = EditorSceneManager.OpenScene(LevelTwoPath, OpenSceneMode.Additive);
        }

        try
        {
            AddLevelTwoChallenges(levelTwo);
            EditorSceneManager.SaveScene(levelTwo);
        }
        finally
        {
            if (closeLevelTwoAfterSetup)
            {
                EditorSceneManager.CloseScene(levelTwo, true);
            }
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(EndLevelPath) == null)
        {
            CreateEndLevelScene();
        }

        SetBuildScenes();
    }

    private static void CopySceneIfMissing(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destinationPath) == null &&
            !AssetDatabase.CopyAsset(sourcePath, destinationPath))
        {
            throw new InvalidOperationException($"Could not copy scene from {sourcePath} to {destinationPath}.");
        }
    }

    private static void AddLevelTwoChallenges(Scene scene)
    {
        GameObject mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MushroomPrefabPath);
        if (mushroomPrefab == null)
        {
            throw new InvalidOperationException($"Could not load mushroom prefab at {MushroomPrefabPath}.");
        }

        AddMushroomIfMissing(scene, mushroomPrefab, "Level 2 Mushroom 1", new Vector3(10f, 1.056f, 0f));
        AddMushroomIfMissing(scene, mushroomPrefab, "Level 2 Mushroom 2", new Vector3(14f, 1.056f, 0f));
        AddMushroomIfMissing(scene, mushroomPrefab, "Level 2 Mushroom 3", new Vector3(18f, 1.056f, 0f));
        AddMushroomIfMissing(scene, mushroomPrefab, "Level 2 Mushroom 4", new Vector3(22f, 1.056f, 0f));

        GameObject ground = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "Ground")
                {
                    ground = child.gameObject;
                    break;
                }
            }

            if (ground != null)
            {
                break;
            }
        }

        if (ground == null)
        {
            throw new InvalidOperationException("Could not find the Ground object in Level 2.");
        }

        SpriteRenderer groundRenderer = ground.GetComponent<SpriteRenderer>();
        if (groundRenderer == null || groundRenderer.sprite == null)
        {
            throw new InvalidOperationException("The Level 2 Ground needs a sprite to create matching platforms.");
        }

        AddPlatformIfMissing(scene, "Level 2 Ground Extension", ground.layer, groundRenderer.sprite,
            new Vector3(16f, 0f, 0f), new Vector2(12f, 1f));
        AddPlatformIfMissing(scene, "Level 2 Raised Platform", ground.layer, groundRenderer.sprite,
            new Vector3(8f, 1.5f, 0f), new Vector2(3f, 0.5f));
    }

    private static void AddMushroomIfMissing(Scene scene, GameObject prefab, string name, Vector3 position)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return;
            }
        }

        GameObject mushroom = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        mushroom.name = name;
        mushroom.transform.position = position;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void AddPlatformIfMissing(
        Scene scene,
        string name,
        int layer,
        Sprite sprite,
        Vector3 position,
        Vector2 size)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name)
            {
                return;
            }
        }

        GameObject platform = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
        SceneManager.MoveGameObjectToScene(platform, scene);
        platform.layer = layer;
        platform.transform.position = position;

        SpriteRenderer renderer = platform.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = size;

        BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
        collider.size = size;
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void CreateEndLevelScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.18f, 0.28f);
            camera.orthographic = true;

            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800f, 600f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject textObject = new GameObject(
                "Victory Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            SceneManager.MoveGameObjectToScene(textObject, scene);
            textObject.transform.SetParent(canvas.transform, false);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = "CONGRATULATIONS!\nYOU BEAT THE GAME!";
            text.fontSize = 48;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            EditorSceneManager.SaveScene(scene, EndLevelPath);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void SetBuildScenes()
    {
        EditorBuildSettings.scenes = new[]
        {
            BuildScene(LevelOnePath),
            BuildScene(LevelTwoPath),
            BuildScene(EndLevelPath)
        };
    }

    private static EditorBuildSettingsScene BuildScene(string path)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid))
        {
            throw new InvalidOperationException($"Could not find a scene asset at {path}.");
        }

        return new EditorBuildSettingsScene(path, true);
    }
}
