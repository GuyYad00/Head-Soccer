using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeadSoccer;
using Object = UnityEngine.Object;

namespace HeadSoccer.EditorTools
{
    /// <summary>
    /// Builds the whole playable project from code: placeholder art, the tuning asset,
    /// the pooled effect prefabs and both scenes, fully wired.
    /// Everything it makes is a normal asset afterwards, so it can be hand edited.
    /// </summary>
    public static class HeadSoccerBuilder
    {
        private const string ArtFolder = "Assets/Art/Generated";
        private const string SpriteFolder = "Assets/Art";
        private const string DataFolder = "Assets/Data";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string SceneFolder = "Assets/Scenes";
        private const string ConfigPath = DataFolder + "/GameConfig.asset";
        private const string BallMaterialPath = DataFolder + "/BallMaterial.physicsMaterial2D";
        private const string MatchScenePath = SceneFolder + "/Match.unity";
        private const string MenuScenePath = SceneFolder + "/Menu.unity";

        // --- pitch geometry, all in world units, camera is orthographic size 5 -------
        private const float GroundTopY = -3.4f;
        private const float CeilingY = 5.0f;
        private const float WallInnerX = 8.9f;
        private const float GoalLineX = 7.7f;
        private const float GoalMouthTopY = -1.2f;
        private const float BallRadius = 0.28f;
        private const float PlayerRootY = -2.4f;
        private const float PlayerSpawnX = 4f;

        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite playerRedSprite;
        private static Sprite playerBlueSprite;
        private static Sprite ballSprite;
        private static Sprite goalSprite;
        private static Sprite stadiumSprite;

        [MenuItem("Head Soccer/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ImportGameSprites();
            GenerateArt();
            CreateBallMaterial(CreateOrLoadConfig());
            CreateEffectPrefab("KickSpark", new Color(1f, 0.85f, 0.3f), 12, 0.35f, 4f, 0.12f);
            CreateEffectPrefab("GoalConfetti", new Color(0.4f, 0.9f, 1f), 60, 1.4f, 8f, 0.18f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildMatchScene();
            BuildMenuScene();
            RegisterScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(MatchScenePath);
            Debug.Log("Head Soccer: project built. Press Play, or open Assets/Scenes/Menu.unity to start from the menu.");
        }

        [MenuItem("Head Soccer/Regenerate Placeholder Art", priority = 20)]
        public static void GenerateArt()
        {
            EnsureFolder(ArtFolder);
            squareSprite = CreateSpriteAsset("Square", 128, false);
            circleSprite = CreateSpriteAsset("Circle", 128, true);
        }

        private static void LoadArt()
        {
            squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Square.png");
            circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Circle.png");
            playerRedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/player_red.png");
            playerBlueSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/player_blue.png");
            ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/ball.png");
            goalSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/goal.png");
            stadiumSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/stadium.png");
        }

        private static void ImportGameSprites()
        {
            EnsureFolder(SpriteFolder);
            string[] files =
            {
                "player_red.png", "player_blue.png", "ball.png", "goal.png", "stadium.png"
            };

            foreach (string file in files)
            {
                string path = $"{SpriteFolder}/{file}";
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path))) continue;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }
        }

        // ================================================================== assets

        private static Sprite CreateSpriteAsset(string name, int size, bool circle)
        {
            string assetPath = $"{ArtFolder}/{name}.png";
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float radius = size * 0.5f;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float alpha = 1f;
                    if (circle)
                    {
                        float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                          new Vector2(radius, radius));
                        // One pixel of soft edge so the circle is not jagged.
                        alpha = Mathf.Clamp01(radius - distance);
                    }
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            // Pixels per unit equal to the texture size makes the sprite exactly one
            // world unit, so transform scale reads directly as size in units.
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static GameConfig CreateOrLoadConfig()
        {
            EnsureFolder(DataFolder);
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config != null) return config;

            config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static PhysicsMaterial2D CreateBallMaterial(GameConfig config)
        {
            EnsureFolder(DataFolder);
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BallMaterialPath);
            if (material == null)
            {
                material = new PhysicsMaterial2D("BallMaterial");
                AssetDatabase.CreateAsset(material, BallMaterialPath);
            }
            material.bounciness = config.ballBounciness;
            material.friction = 0.05f;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreateEffectPrefab(string name, Color color, int burst,
                                                     float lifetime, float speed, float startSize)
        {
            EnsureFolder(PrefabFolder);
            var root = new GameObject(name);
            var particles = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = startSize;
            main.startColor = color;
            main.gravityModifier = 0.9f;
            main.maxParticles = Mathf.Max(burst * 2, 32);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, burst) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateParticleMaterial();
            renderer.sortingLayerName = "FX";

            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static Material CreateParticleMaterial()
        {
            string path = DataFolder + "/ParticleUnlit.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                            ?? Shader.Find("Sprites/Default");
            var material = new Material(shader);
            if (circleSprite != null) material.mainTexture = circleSprite.texture;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ================================================================== match scene

        private static void BuildMatchScene()
        {
            EnsureFolder(SceneFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Loaded only after the new scene exists. Creating a scene unloads unused
            // assets, and a ScriptableObject reference taken before that point is dead
            // by the time it is assigned, which serialises silently as null.
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var ballMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BallMaterialPath);
            var spark = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/KickSpark.prefab");
            var confetti = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/GoalConfetti.prefab");
            LoadArt();

            Camera camera = CreateCamera(new Color(0.05f, 0.12f, 0.08f));
            CreateGlobalLight();

            // ----- pitch ------------------------------------------------------
            var pitch = new GameObject("Pitch").transform;

            if (stadiumSprite != null)
                CreateFittedSprite("Stadium", pitch, stadiumSprite, new Vector2(0f, 0.35f), 10.4f, "Background");

            // Colliders stay, but the old coloured boxes are hidden so the stadium art shows through.
            HideRenderer(CreateSolid("Ground", pitch, new Vector2(0f, GroundTopY - 4f), new Vector2(60f, 8f),
                        new Color(0.22f, 0.55f, 0.25f), "Pitch", "Ground"));
            HideRenderer(CreateSolid("Ceiling", pitch, new Vector2(0f, CeilingY + 4f), new Vector2(60f, 8f),
                        new Color(0.1f, 0.1f, 0.14f), "Pitch", "Wall"));
            HideRenderer(CreateSolid("WallLeft", pitch, new Vector2(-WallInnerX - 10f, 1f), new Vector2(20f, 30f),
                        new Color(0.13f, 0.16f, 0.2f), "Pitch", "Wall"));
            HideRenderer(CreateSolid("WallRight", pitch, new Vector2(WallInnerX + 10f, 1f), new Vector2(20f, 30f),
                        new Color(0.13f, 0.16f, 0.2f), "Pitch", "Wall"));

            CreateFieldMarkings(pitch);

            CreateGoal(Side.Left, pitch);
            CreateGoal(Side.Right, pitch);

            // ----- ball -------------------------------------------------------
            var ballObject = new GameObject("Ball");
            ballObject.transform.position = new Vector3(0f, 2f, 0f);
            ballObject.layer = LayerMask.NameToLayer("Ball");
            ballObject.tag = "Ball";

            Sprite usedBall = ballSprite != null ? ballSprite : circleSprite;
            if (usedBall != null)
            {
                GameObject ballArt = CreateFittedSprite("Visual", ballObject.transform, usedBall,
                    ballObject.transform.position, BallRadius * 2f, "Ball");
                ballArt.transform.localPosition = Vector3.zero;
            }

            var ballBody = ballObject.AddComponent<Rigidbody2D>();
            ballBody.mass = 0.9f;
            ballBody.angularDamping = 0.4f;

            var ballCollider = ballObject.AddComponent<CircleCollider2D>();
            ballCollider.radius = BallRadius;
            ballCollider.sharedMaterial = ballMaterial;

            var ball = ballObject.AddComponent<BallController>();
            Set(ball, "config", config);

            // ----- players ----------------------------------------------------
            GameObject leftPlayerObject = CreatePlayer(Side.Left, config,
                new Color(0.95f, 0.35f, 0.3f), InputMode.Auto);
            GameObject rightPlayerObject = CreatePlayer(Side.Right, config,
                new Color(0.35f, 0.55f, 0.95f), InputMode.Auto);

            var leftPlayer = leftPlayerObject.GetComponent<PlayerController>();
            var rightPlayer = rightPlayerObject.GetComponent<PlayerController>();

            // ----- managers ---------------------------------------------------
            var managers = new GameObject("_Managers").transform;

            var gameManagerObject = new GameObject("GameManager");
            gameManagerObject.transform.SetParent(managers);
            var gameManager = gameManagerObject.AddComponent<GameManager>();
            Set(gameManager, "config", config);
            Set(gameManager, "ball", ball);
            Set(gameManager, "leftPlayer", leftPlayer);
            Set(gameManager, "rightPlayer", rightPlayer);
            Set(gameManager, "menuSceneName", "Menu");

            var audioObject = new GameObject("AudioManager");
            audioObject.transform.SetParent(managers);
            audioObject.AddComponent<AudioManager>();

            var effectsObject = new GameObject("EffectsPool");
            effectsObject.transform.SetParent(managers);
            var effects = effectsObject.AddComponent<EffectsPool>();
            Set(effects, "kickSparkPrefab", spark);
            Set(effects, "goalConfettiPrefab", confetti);
            Set(effects, "poolSizePerEffect", 20);

            // ----- UI ---------------------------------------------------------
            BuildMatchUI(camera, leftPlayerObject, rightPlayerObject);

            EditorSceneManager.SaveScene(scene, MatchScenePath);
        }

        private static void CreateFieldMarkings(Transform pitch)
        {
            float midY = GroundTopY + 1.15f;

            CreateSprite("CentreLine", pitch, squareSprite,
                new Vector2(0f, midY), new Vector2(0.06f, 2.3f),
                new Color(1f, 1f, 1f, 0.55f), "Pitch");

            if (circleSprite != null)
            {
                GameObject circle = CreateSprite("CentreCircle", pitch, circleSprite,
                    new Vector2(0f, GroundTopY + 0.85f), new Vector2(2.2f, 0.7f),
                    new Color(1f, 1f, 1f, 0.35f), "Pitch");
                var renderer = circle.GetComponent<SpriteRenderer>();
                renderer.drawMode = SpriteDrawMode.Simple;
            }
        }

        private static void CreateGoal(Side side, Transform parent)
        {
            float sign = side == Side.Left ? -1f : 1f;
            float centreX = sign * (WallInnerX + GoalLineX) * 0.5f;
            float depth = WallInnerX - GoalLineX;
            float mouthHeight = GoalMouthTopY - GroundTopY;
            float mouthCentreY = (GoalMouthTopY + GroundTopY) * 0.5f;

            var goal = new GameObject($"Goal{side}").transform;
            goal.SetParent(parent);

            if (goalSprite != null)
            {
                GameObject art = CreateFittedSprite("NetArt", goal, goalSprite,
                    new Vector2(sign * 8.15f, GroundTopY + 1.15f), 2.7f, "Goals");
                Vector3 scale = art.transform.localScale;
                // The painting opens toward the left. Flip it for the left-hand goal.
                if (side == Side.Left)
                    art.transform.localScale = new Vector3(-Mathf.Abs(scale.x), scale.y, 1f);
            }
            else
            {
                CreateSprite("Net", goal, squareSprite, new Vector2(centreX, mouthCentreY),
                    new Vector2(depth, mouthHeight), new Color(1f, 1f, 1f, 0.18f), "Goals");
            }

            HideRenderer(CreateSolid("Crossbar", goal, new Vector2(centreX, GoalMouthTopY + 0.15f),
                new Vector2(depth + 0.1f, 0.3f), Color.white, "Goals", "Wall"));
            HideRenderer(CreateSolid("BackPost", goal, new Vector2(sign * WallInnerX, mouthCentreY),
                new Vector2(0.18f, mouthHeight), Color.white, "Goals", "Wall"));

            // Sits far enough inside the net that the ball has fully crossed the line
            // by the time its centre enters the trigger.
            float triggerOuterX = sign * WallInnerX;
            float triggerInnerX = sign * (GoalLineX + BallRadius);
            var mouth = new GameObject("GoalMouth");
            mouth.transform.SetParent(goal);
            mouth.transform.position = new Vector3((triggerOuterX + triggerInnerX) * 0.5f, mouthCentreY, 0f);
            mouth.layer = LayerMask.NameToLayer("Goal");

            var trigger = mouth.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(Mathf.Abs(triggerOuterX - triggerInnerX), mouthHeight - 0.1f);

            var goalTrigger = mouth.AddComponent<GoalTrigger>();
            Set(goalTrigger, "goalOwner", side);
        }

        private static GameObject CreatePlayer(Side side, GameConfig config, Color color, InputMode mode)
        {
            float sign = side == Side.Left ? -1f : 1f;
            var root = new GameObject($"Player{(side == Side.Left ? 1 : 2)}");
            root.transform.position = new Vector3(sign * PlayerSpawnX, PlayerRootY, 0f);
            root.layer = LayerMask.NameToLayer("Player");

            if (circleSprite != null)
            {
                CreateSprite("Shadow", root.transform, circleSprite,
                    new Vector2(root.transform.position.x, GroundTopY + 0.06f),
                    new Vector2(1.15f, 0.28f), new Color(0f, 0f, 0f, 0.35f), "Pitch");
            }

            var body = root.AddComponent<Rigidbody2D>();
            body.mass = 3f;
            body.freezeRotation = true;

            var bodyCollider = root.AddComponent<CapsuleCollider2D>();
            bodyCollider.offset = new Vector2(0f, -0.4f);
            bodyCollider.size = new Vector2(0.9f, 1.2f);

            var headCollider = root.AddComponent<CircleCollider2D>();
            headCollider.offset = new Vector2(0f, 0.55f);
            headCollider.radius = 0.55f;

            var groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(root.transform);
            groundCheck.localPosition = new Vector3(0f, -1f, 0f);

            var kickPoint = new GameObject("KickHitbox");
            kickPoint.transform.SetParent(root.transform);
            kickPoint.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var kickHitbox = kickPoint.AddComponent<KickHitbox>();
            Set(kickHitbox, "config", config);
            Set(kickHitbox, "ballLayer", 1 << LayerMask.NameToLayer("Ball"));

            var controller = root.AddComponent<PlayerController>();
            Set(controller, "config", config);
            Set(controller, "side", side);
            Set(controller, "inputMode", mode);
            Set(controller, "groundCheck", groundCheck);
            Set(controller, "kickHitbox", kickHitbox);
            Set(controller, "groundLayer",
                (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Wall")));

            Sprite character = side == Side.Left ? playerRedSprite : playerBlueSprite;
            if (character != null)
            {
                GameObject visual = CreateFittedSprite("Visual", root.transform, character,
                    root.transform.position, 2.55f, "Players");
                visual.transform.localPosition = new Vector3(0f, 0.2f, 0f);
                var pose = visual.AddComponent<PlayerVisual>();
                Set(pose, "player", controller);
                Set(pose, "spriteRenderer", visual.GetComponent<SpriteRenderer>());
                Set(pose, "spriteFacesRight", side == Side.Left);
            }
            else
            {
                CreateSprite("Body", root.transform, squareSprite,
                    new Vector2(root.transform.position.x, root.transform.position.y - 0.4f),
                    new Vector2(0.9f, 1.2f), color, "Players");
                CreateSprite("Head", root.transform, circleSprite,
                    new Vector2(root.transform.position.x, root.transform.position.y + 0.55f),
                    Vector2.one * 1.1f, Color.Lerp(color, Color.white, 0.45f), "Players");
            }

            return root;
        }

        // ================================================================== UI

        private static void BuildMatchUI(Camera camera, GameObject leftPlayer, GameObject rightPlayer)
        {
            Canvas canvas = CreateCanvas(camera);
            var uiManager = canvas.gameObject.AddComponent<UIManager>();

            // Everything the player has to see or touch lives inside the safe area, so a
            // notch or a gesture bar never covers the score or the kick button.
            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SafeAreaFitter>();
            Transform hud = safeArea.transform;

            // --- scoreboard and clock, top centre -----------------------------
            TextMeshProUGUI leftScore = CreateText(hud, "LeftScore", "0", 72,
                new Vector2(0.5f, 1f), new Vector2(-190f, -70f), new Vector2(120f, 90f));
            TextMeshProUGUI rightScore = CreateText(hud, "RightScore", "0", 72,
                new Vector2(0.5f, 1f), new Vector2(190f, -70f), new Vector2(120f, 90f));
            CreateText(hud, "P1Label", "P1", 22,
                new Vector2(0.5f, 1f), new Vector2(-190f, -118f), new Vector2(120f, 32f));
            CreateText(hud, "P2Label", "P2", 22,
                new Vector2(0.5f, 1f), new Vector2(190f, -118f), new Vector2(120f, 32f));
            CreateText(hud, "Dash", ":", 60,
                new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(60f, 90f));
            TextMeshProUGUI timer = CreateText(hud, "Timer", "1:30", 40,
                new Vector2(0.5f, 1f), new Vector2(0f, -125f), new Vector2(240f, 60f));

            TextMeshProUGUI countdown = CreateText(canvas.transform, "Countdown", string.Empty, 130,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(700f, 200f));

            // --- pause button, top right --------------------------------------
            Button pauseButton = CreateButton(hud, "PauseButton", "II", 34,
                new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(70f, 70f));
            UnityEventTools.AddVoidPersistentListener(pauseButton.onClick, uiManager.OnPauseButton);

            // --- pause overlay -------------------------------------------------
            GameObject pausePanel = CreateOverlay(canvas.transform, "PausePanel");
            CreateText(pausePanel.transform, "PausedTitle", "PAUSED", 80,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(700f, 120f));

            Button resume = CreateButton(pausePanel.transform, "ResumeButton", "RESUME", 40,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(340f, 80f));
            Button restart = CreateButton(pausePanel.transform, "RestartButton", "RESTART", 40,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -70f), new Vector2(340f, 80f));
            Button quit = CreateButton(pausePanel.transform, "QuitButton", "QUIT TO MENU", 40,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(340f, 80f));

            UnityEventTools.AddVoidPersistentListener(resume.onClick, uiManager.OnResumeButton);
            UnityEventTools.AddVoidPersistentListener(restart.onClick, uiManager.OnRestartButton);
            UnityEventTools.AddVoidPersistentListener(quit.onClick, uiManager.OnQuitToMenuButton);

            // --- match over overlay --------------------------------------------
            GameObject overPanel = CreateOverlay(canvas.transform, "MatchOverPanel");
            TextMeshProUGUI winner = CreateText(overPanel.transform, "WinnerText", "PLAYER 1 WINS", 76,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 160f), new Vector2(900f, 120f));
            TextMeshProUGUI finalScore = CreateText(overPanel.transform, "FinalScoreText", "0 - 0", 56,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(500f, 90f));

            Button rematch = CreateButton(overPanel.transform, "RematchButton", "REMATCH", 42,
                new Vector2(0.5f, 0.5f), new Vector2(-180f, -80f), new Vector2(300f, 90f));
            Button toMenu = CreateButton(overPanel.transform, "MenuButton", "MENU", 42,
                new Vector2(0.5f, 0.5f), new Vector2(180f, -80f), new Vector2(300f, 90f));

            UnityEventTools.AddVoidPersistentListener(rematch.onClick, uiManager.OnRestartButton);
            UnityEventTools.AddVoidPersistentListener(toMenu.onClick, uiManager.OnQuitToMenuButton);

            // --- touch controls -------------------------------------------------
            GameObject touchPanel = CreateUIObject("TouchControls", hud);
            Stretch(touchPanel.GetComponent<RectTransform>());
            var touchSource = touchPanel.AddComponent<TouchInputSource>();

            CreateHoldButton(touchPanel.transform, "TouchLeft", "<", touchSource, TouchAction.Left,
                new Vector2(0f, 0f), new Vector2(110f, 100f), new Vector2(150f, 150f));
            CreateHoldButton(touchPanel.transform, "TouchRight", ">", touchSource, TouchAction.Right,
                new Vector2(0f, 0f), new Vector2(280f, 100f), new Vector2(150f, 150f));
            CreateHoldButton(touchPanel.transform, "TouchJump", "JUMP", touchSource, TouchAction.Jump,
                new Vector2(1f, 0f), new Vector2(-280f, 100f), new Vector2(150f, 150f));
            CreateHoldButton(touchPanel.transform, "TouchKick", "KICK", touchSource, TouchAction.Kick,
                new Vector2(1f, 0f), new Vector2(-110f, 100f), new Vector2(150f, 150f));

            // Both characters can be driven from the same panel; only the one whose
            // input mode resolves to Touch actually listens to it.
            Set(leftPlayer.GetComponent<PlayerController>(), "touchSource", touchSource);
            Set(rightPlayer.GetComponent<PlayerController>(), "touchSource", touchSource);

            Set(uiManager, "leftScoreText", leftScore);
            Set(uiManager, "rightScoreText", rightScore);
            Set(uiManager, "timerText", timer);
            Set(uiManager, "countdownText", countdown);
            Set(uiManager, "pausePanel", pausePanel);
            Set(uiManager, "matchOverPanel", overPanel);
            Set(uiManager, "winnerText", winner);
            Set(uiManager, "finalScoreText", finalScore);
            Set(uiManager, "touchControlsPanel", touchPanel);

            pausePanel.SetActive(false);
            overPanel.SetActive(false);

            CreateEventSystem();
        }

        private static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            LoadArt();
            Camera camera = CreateCamera(new Color(0.05f, 0.12f, 0.08f));
            CreateGlobalLight();

            if (stadiumSprite != null)
                CreateFittedSprite("Stadium", null, stadiumSprite, new Vector2(0f, 0.35f), 10.4f, "Background");

            var audioObject = new GameObject("AudioManager");
            audioObject.AddComponent<AudioManager>();

            Canvas canvas = CreateCanvas(camera);
            var menu = canvas.gameObject.AddComponent<MainMenuController>();
            Set(menu, "matchSceneName", "Match");

            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SafeAreaFitter>();

            CreateText(safeArea.transform, "Title", "HEAD SOCCER", 100,
                new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1000f, 160f));

            Button play = CreateButton(safeArea.transform, "PlayButton", "PLAY  (1P vs CPU)", 42,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(460f, 90f));
            Button twoPlayers = CreateButton(safeArea.transform, "TwoPlayerButton", "2 PLAYERS", 42,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(460f, 90f));
            Button difficulty = CreateButton(safeArea.transform, "DifficultyButton", "CPU: MEDIUM", 34,
                new Vector2(0.5f, 0.5f), new Vector2(-130f, -180f), new Vector2(280f, 70f));
            Button audio = CreateButton(safeArea.transform, "AudioButton", "AUDIO: ON", 34,
                new Vector2(0.5f, 0.5f), new Vector2(170f, -180f), new Vector2(280f, 70f));

            UnityEventTools.AddVoidPersistentListener(play.onClick, menu.PlayVsCPU);
            UnityEventTools.AddVoidPersistentListener(twoPlayers.onClick, menu.PlayTwoPlayers);
            UnityEventTools.AddVoidPersistentListener(difficulty.onClick, menu.CycleDifficulty);
            UnityEventTools.AddVoidPersistentListener(audio.onClick, menu.ToggleAudio);

            Set(menu, "difficultyLabel", difficulty.GetComponentInChildren<TextMeshProUGUI>());
            Set(menu, "audioLabel", audio.GetComponentInChildren<TextMeshProUGUI>());

            CreateText(safeArea.transform, "Hint",
                "P1  A / D move   W jump   Space kick        P2  Arrows move   Up jump   RightCtrl kick",
                24, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(1200f, 50f));

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // ================================================================== helpers

        private static Camera CreateCamera(Color background)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<AudioListener>();

            var fitter = cameraObject.AddComponent<CameraFitter>();
            Set(fitter, "requiredHalfWidth", WallInnerX + 0.3f);
            Set(fitter, "minOrthographicSize", 5f);
            return camera;
        }

        private static void CreateGlobalLight()
        {
            // A URP 2D project renders sprites with the lit shader, so without a global
            // light every sprite in the scene is black.
            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
            light.color = Color.white;
        }

        private static GameObject CreateSprite(string name, Transform parent, Sprite sprite,
                                               Vector2 position, Vector2 scale, Color color,
                                               string sortingLayer)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = position;
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            return go;
        }

        private static GameObject CreateFittedSprite(string name, Transform parent, Sprite sprite,
                                                     Vector2 position, float worldHeight,
                                                     string sortingLayer)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            go.transform.position = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = sortingLayer;

            if (sprite != null && sprite.bounds.size.y > 0.001f)
            {
                float scale = worldHeight / sprite.bounds.size.y;
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }
            return go;
        }

        private static void HideRenderer(GameObject go)
        {
            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

        private static GameObject CreateSolid(string name, Transform parent, Vector2 position,
                                              Vector2 scale, Color color, string sortingLayer,
                                              string physicsLayer)
        {
            GameObject go = CreateSprite(name, parent, squareSprite, position, scale, color, sortingLayer);
            go.layer = LayerMask.NameToLayer(physicsLayer);

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;   // one unit local, scaled by the transform
            return go;
        }

        private static Canvas CreateCanvas(Camera camera)
        {
            var canvasObject = new GameObject("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5f;
            canvas.sortingLayerName = "UI";

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void CreateEventSystem()
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            // The project uses the new Input System only, so the old StandaloneInputModule
            // would never receive a click.
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content,
                                                  int fontSize, Vector2 anchor,
                                                  Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, int fontSize,
                                           Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize,
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Stretch(text.rectTransform);
            return button;
        }

        private static void CreateHoldButton(Transform parent, string name, string label,
                                             TouchInputSource source, TouchAction action,
                                             Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            var image = go.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.22f);

            var hold = go.AddComponent<HoldButton>();
            Set(hold, "target", source);
            Set(hold, "action", action);

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 30,
                new Vector2(0.5f, 0.5f), Vector2.zero, size);
            Stretch(text.rectTransform);
        }

        private static GameObject CreateOverlay(Transform parent, string name)
        {
            GameObject go = CreateUIObject(name, parent);
            Stretch(go.GetComponent<RectTransform>());
            var image = go.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);
            return go;
        }

        /// <summary>
        /// Writes a private [SerializeField] from the editor, which is how a generated
        /// scene can be wired without making every field public.
        /// </summary>
        private static void Set(Object target, string fieldName, object value)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"No serialized field '{fieldName}' on {target.GetType().Name}.");
                return;
            }

            switch (value)
            {
                case Object reference: property.objectReferenceValue = reference; break;
                case bool flag: property.boolValue = flag; break;
                case int number: property.intValue = number; break;
                case float number: property.floatValue = number; break;
                case string text: property.stringValue = text; break;
                case Enum enumValue: property.intValue = Convert.ToInt32(enumValue); break;
                default:
                    Debug.LogError($"Unsupported value type for '{fieldName}': {value?.GetType()}");
                    break;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Catch the silent failure where a reference is assigned but does not stick.
            if (value is Object expected && expected != null && property.objectReferenceValue == null)
                Debug.LogError($"Field '{fieldName}' on {target.GetType().Name} stayed null after assigning {expected.name}.");
        }

        private static void RegisterScenes()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(MatchScenePath, true)
            };
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
