using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
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
    /// Builds the whole playable project from the art in Assets/Art: the tuning asset,
    /// the character roster, the prefabs (Player, Ball, Goal and the pooled effects)
    /// and both scenes, assembled from instances of those prefabs and fully wired.
    /// Everything it makes is a normal asset afterwards, so it can be hand edited.
    /// Run it once from the menu "Head Soccer / Build Everything".
    /// </summary>
    public static class HeadSoccerBuilder
    {
        private const string ArtFolder = "Assets/Art/Generated";
        private const string SpriteFolder = "Assets/Art";
        private const string UIFolder = "Assets/UI";
        private const string FontFolder = "Assets/Fonts";
        private const string DataFolder = "Assets/Data";
        private const string PrefabFolder = "Assets/Prefabs";
        private const string SceneFolder = "Assets/Scenes";
        private const string ConfigPath = DataFolder + "/GameConfig.asset";
        private const string RosterPath = DataFolder + "/CharacterRoster.asset";
        private const string BallMaterialPath = DataFolder + "/BallMaterial.physicsMaterial2D";
        private const string FontAssetPath = FontFolder + "/Oswald-Bold SDF.asset";
        private const string AtlasPath = SpriteFolder + "/HeadSoccer.spriteatlasv2";
        private const string IconPath = "Assets/Branding/icon.png";
        private const string CharactersFolder = SpriteFolder + "/Characters";
        private const string MatchScenePath = SceneFolder + "/Match.unity";
        private const string MenuScenePath = SceneFolder + "/Menu.unity";

        // --- pitch geometry, all in world units, camera is orthographic size 5 -------
        private const float GroundTopY = -3.4f;
        private const float CeilingY = 5.0f;
        private const float WallInnerX = 8.9f;
        private const float GoalLineX = 7.7f;
        private const float BallRadius = 0.28f;
        private const float PlayerRootY = -2.4f;
        private const float PlayerSpawnX = 4f;
        private const float PlayerHeight = 2.15f;
        // The player drawing is centred 0.05 above its root, so the top of the head is here.
        private const float PlayerHeadTopY = PlayerRootY + 0.05f + PlayerHeight * 0.5f;
        // GDD: the crossbar sits at one and a half times the players' head height, so a
        // header still needs a real jump but a lob can still be kept out.
        private const float GoalMouthTopY = GroundTopY + (PlayerHeadTopY - GroundTopY) * 1.5f;
        // The advertising board stands in front of the first row of the crowd, the way
        // stadium LED boards hide the spectators' legs. Big enough to read from the sofa.
        private const float AdBoardHeight = 1.15f;
        private const float AdBoardY = -1.15f + AdBoardHeight * 0.5f;   // bottom edge on the grass line

        // --- palette ---------------------------------------------------------------
        private static readonly Color PanelDark = new Color(0.07f, 0.09f, 0.14f, 0.92f);
        private static readonly Color PanelCard = new Color(0.10f, 0.13f, 0.20f, 0.95f);
        private static readonly Color Accent = new Color(1f, 0.82f, 0.2f, 1f);
        private static readonly Color RedTeam = new Color(1f, 0.42f, 0.38f, 1f);
        private static readonly Color BlueTeam = new Color(0.42f, 0.66f, 1f, 1f);
        private static readonly Color ButtonGreen = new Color(0.20f, 0.72f, 0.36f, 1f);
        private static readonly Color ButtonNeutral = new Color(0.18f, 0.22f, 0.32f, 1f);
        private static readonly Color ButtonRed = new Color(0.80f, 0.28f, 0.28f, 1f);
        private static readonly Color TextDim = new Color(1f, 1f, 1f, 0.6f);

        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite roundedSprite;
        private static Sprite playerRedSprite;
        private static Sprite playerBlueSprite;
        private static Sprite ballSprite;
        private static Sprite goalSprite;
        private static Sprite stadiumSprite;
        private static Sprite adBoardSprite;
        private static TMP_FontAsset font;

        [MenuItem("Head Soccer/Build Everything", priority = 0)]
        public static void BuildEverything()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ImportGameSprites();
            GenerateArt();
            CreateSpriteAtlas();
            ApplyAppIcon();
            CreateFontAsset();
            GameConfig config = CreateOrLoadConfig();
            CreateBallMaterial(config);
            CreateRoster();
            CreateEffectPrefab("KickSpark", new Color(1f, 0.85f, 0.3f), 12, 0.35f, 4f, 0.12f);
            CreateEffectPrefab("GoalConfetti", new Color(0.4f, 0.9f, 1f), 60, 1.4f, 8f, 0.18f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Gameplay prefabs come after the data assets they reference are on disk.
            CreateGameplayPrefabs();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            BuildMatchScene();
            BuildMenuScene();
            RegisterScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorSceneManager.OpenScene(MenuScenePath);
            Debug.Log("Head Soccer: project built. Press Play to start from the menu.");
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
            roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{UIFolder}/RoundedPanel.png");
            playerRedSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharactersFolder}/{Characters[0].file}.png");
            playerBlueSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharactersFolder}/{Characters[1].file}.png");
            ballSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/ball.png");
            goalSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/goal.png");
            stadiumSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/stadium.png");
            adBoardSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/adboard.png");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            if (playerRedSprite == null || ballSprite == null || goalSprite == null || stadiumSprite == null)
                Debug.LogWarning("Head Soccer: some sprites in Assets/Art are missing; placeholders will be used.");
        }

        private static void ImportGameSprites()
        {
            // Max texture size per sprite. The source PNGs are 1024px+ but the ball is
            // drawn at about 40 px, so shipping it at 1024 would only waste phone memory.
            var files = new List<(string path, int maxSize)>
            {
                ($"{SpriteFolder}/ball.png", 256), ($"{SpriteFolder}/goal.png", 1024),
                ($"{SpriteFolder}/stadium.png", 2048), ($"{UIFolder}/RoundedPanel.png", 128)
            };
            files.Add(($"{SpriteFolder}/adboard.png", 1024));
            foreach (CharacterArt character in Characters)
            {
                files.Add(($"{CharactersFolder}/{character.file}.png", 1024));
                files.Add(($"{CharactersFolder}/{character.file}_kick.png", 1024));
                files.Add(($"{CharactersFolder}/{character.file}_celebrate.png", 1024));
            }

            foreach ((string path, int maxSize) in files)
            {
                if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path))) continue;

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = maxSize;

                if (path.EndsWith("RoundedPanel.png"))
                {
                    // 9-slice so the panel corners stay crisp at any size.
                    importer.spriteBorder = new Vector4(30f, 30f, 30f, 30f);
                }

                importer.SaveAndReimport();
            }
        }

        /// <summary>Uses Assets/Branding/icon.png as the app icon on every platform.</summary>
        private static void ApplyAppIcon()
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), IconPath))) return;

            if (AssetImporter.GetAtPath(IconPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 1024;
                importer.SaveAndReimport();
            }

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return;

            // The "Unknown" target is the default icon, inherited by Android and Windows.
            int slots = PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown, IconKind.Any).Length;
            var icons = new Texture2D[Mathf.Max(1, slots)];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, icons, IconKind.Any);
        }

        /// <summary>
        /// One atlas for every sprite the game draws, so the pitch, goals, ball and both
        /// characters render in a single batch (GDD section 6, technical art rules).
        /// </summary>
        private static void CreateSpriteAtlas()
        {
            SpriteAtlasAsset atlas = SpriteAtlasAsset.Load(AtlasPath) ?? new SpriteAtlasAsset();

            var artFolder = AssetDatabase.LoadAssetAtPath<Object>(SpriteFolder);
            var uiFolder = AssetDatabase.LoadAssetAtPath<Object>(UIFolder);
            // Remove then add, so rerunning the builder never lists a folder twice.
            atlas.Remove(new[] { artFolder, uiFolder });
            atlas.Add(new[] { artFolder, uiFolder });
            SpriteAtlasAsset.Save(atlas, AtlasPath);
            AssetDatabase.ImportAsset(AtlasPath);

            // Packing and texture settings live on the importer in Sprite Atlas V2.
            var importer = AssetImporter.GetAtPath(AtlasPath) as SpriteAtlasImporter;
            if (importer == null) return;

            importer.includeInBuild = true;

            SpriteAtlasPackingSettings packing = importer.packingSettings;
            packing.enableRotation = false;       // flipped goal art must stay upright in the atlas
            packing.enableTightPacking = false;
            packing.padding = 4;
            importer.packingSettings = packing;

            SpriteAtlasTextureSettings texture = importer.textureSettings;
            texture.filterMode = FilterMode.Bilinear;
            texture.generateMipMaps = false;
            texture.sRGB = true;
            importer.textureSettings = texture;

            // Four characters with two poses each need more than one 2048 page.
            TextureImporterPlatformSettings platform = importer.GetPlatformSettings("DefaultTexturePlatform");
            platform.maxTextureSize = 4096;
            importer.SetPlatformSettings(platform);

            importer.SaveAndReimport();
        }

        // ================================================================== assets

        private static void CreateFontAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath) != null) return;

            var ttf = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/Oswald-Bold.ttf");
            if (ttf == null)
            {
                Debug.LogWarning("Head Soccer: Assets/Fonts/Oswald-Bold.ttf not found, TMP default font will be used.");
                return;
            }

            try
            {
                // Dynamic SDF atlas: glyphs are added as the game prints them, so no
                // character set has to be listed up front.
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(ttf, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                asset.name = "Oswald-Bold SDF";

                AssetDatabase.CreateAsset(asset, FontAssetPath);
                asset.material.name = asset.name + " Material";
                asset.atlasTexture.name = asset.name + " Atlas";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(FontAssetPath);
            }
            catch (Exception error)
            {
                Debug.LogWarning($"Head Soccer: could not create the Oswald font asset ({error.Message}). " +
                                 "Create it by hand: Window > TextMeshPro > Font Asset Creator, then rerun the build.");
            }
        }

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

        /// <summary>
        /// The selectable characters. Each has an idle and a kick drawing in
        /// Assets/Art/Characters, all drawn facing right. Stats are multipliers on GameConfig.
        /// </summary>
        private struct CharacterArt
        {
            public string file, name, hint;
            public CelebrationStyle celebration;
            public float speed, jump, power;

            public CharacterArt(string file, string name, string hint, CelebrationStyle celebration,
                                float speed, float jump, float power)
            {
                this.file = file; this.name = name; this.hint = hint; this.celebration = celebration;
                this.speed = speed; this.jump = jump; this.power = power;
            }
        }

        private static readonly CharacterArt[] Characters =
        {
            new CharacterArt("yossi", "Yossi", "Tough and confident, never gives up", CelebrationStyle.KissBadge, 1.0f, 1.05f, 1.1f),
            new CharacterArt("david", "David", "Classy and precise, plays with style", CelebrationStyle.Heart, 1.05f, 1.0f, 1.0f),
            new CharacterArt("kim", "Kim", "Fast and focused, samurai balance", CelebrationStyle.Bow, 1.2f, 1.15f, 0.85f),
            new CharacterArt("mikel", "Mikel", "Strong and full of energy", CelebrationStyle.Flip, 1.1f, 0.95f, 1.2f),
            new CharacterArt("noa", "Noa", "Fearless captain, lights up the pitch", CelebrationStyle.Cheer, 1.15f, 1.1f, 0.95f),
            new CharacterArt("anna", "Anna", "Ice cool, slides into every goal", CelebrationStyle.KneeSlide, 1.05f, 1.2f, 1.0f)
        };

        /// <summary>
        /// Re-imports the character art and refreshes the roster asset only, leaving
        /// the scenes and prefabs untouched. Use after adding a character to the table.
        /// </summary>
        [MenuItem("Head Soccer/Refresh Character Roster", priority = 22)]
        public static void RefreshRoster()
        {
            ImportGameSprites();
            CreateRoster();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Head Soccer: roster refreshed with {Characters.Length} characters.");
        }

        /// <summary>Creates or refreshes the roster asset from the Characters table.</summary>
        private static CharacterRoster CreateRoster()
        {
            EnsureFolder(DataFolder);
            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath);
            bool isNew = roster == null;
            if (isNew) roster = ScriptableObject.CreateInstance<CharacterRoster>();

            var serialized = new SerializedObject(roster);
            SerializedProperty list = serialized.FindProperty("characters");
            list.arraySize = Characters.Length;

            for (int i = 0; i < Characters.Length; i++)
            {
                CharacterArt art = Characters[i];
                var idle = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharactersFolder}/{art.file}.png");
                var kick = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharactersFolder}/{art.file}_kick.png");
                var celebrate = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharactersFolder}/{art.file}_celebrate.png");
                if (idle == null) Debug.LogWarning($"Head Soccer: missing {CharactersFolder}/{art.file}.png");

                SerializedProperty element = list.GetArrayElementAtIndex(i);
                FillCharacter(element, art.name, art.hint, idle, kick, true, Color.white, art.speed, art.jump, art.power);
                element.FindPropertyRelative("celebration").objectReferenceValue = celebrate;
                element.FindPropertyRelative("celebrationStyle").enumValueIndex = (int)art.celebration;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (isNew) AssetDatabase.CreateAsset(roster, RosterPath);
            else EditorUtility.SetDirty(roster);
            return roster;
        }

        private static void FillCharacter(SerializedProperty element, string name, string hint, Sprite portrait,
                                          Sprite kickPose, bool facesRight, Color tint, float speed, float jump, float power)
        {
            element.FindPropertyRelative("displayName").stringValue = name;
            element.FindPropertyRelative("statHint").stringValue = hint;
            element.FindPropertyRelative("portrait").objectReferenceValue = portrait;
            element.FindPropertyRelative("kickPose").objectReferenceValue = kickPose;
            element.FindPropertyRelative("facesRight").boolValue = facesRight;
            element.FindPropertyRelative("tint").colorValue = tint;
            element.FindPropertyRelative("speed").floatValue = speed;
            element.FindPropertyRelative("jump").floatValue = jump;
            element.FindPropertyRelative("power").floatValue = power;
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
            var spark = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/KickSpark.prefab");
            var confetti = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/GoalConfetti.prefab");
            LoadArt();

            Camera camera = CreateCamera(new Color(0.05f, 0.12f, 0.08f));
            camera.gameObject.AddComponent<CameraShake>();
            CreateGlobalLight();

            // ----- pitch ------------------------------------------------------
            var pitch = new GameObject("Pitch").transform;

            if (stadiumSprite != null)
                CreateFittedSprite("Stadium", pitch, stadiumSprite, new Vector2(0f, 0.35f), 10.4f, "Background");

            // Scrolling advertising board along the front of the stands, as in a real ground.
            if (adBoardSprite != null)
            {
                var board = new GameObject("AdBoard");
                board.transform.SetParent(pitch);
                board.transform.position = new Vector3(0f, AdBoardY, 0f);
                var ads = board.AddComponent<AdBoard>();
                Set(ads, "banner", adBoardSprite);
                Set(ads, "boardWidth", 18.5f);
                Set(ads, "boardHeight", AdBoardHeight);
                Set(ads, "scrollSpeed", -0.6f);
                Set(ads, "sortingLayer", "Pitch");
                Set(ads, "sortingOrder", 5);
            }

            // Invisible colliders: the stadium painting already shows the ground and walls.
            HideRenderer(CreateSolid("Ground", pitch, new Vector2(0f, GroundTopY - 4f), new Vector2(60f, 8f),
                        Color.white, "Pitch", "Ground"));
            HideRenderer(CreateSolid("Ceiling", pitch, new Vector2(0f, CeilingY + 4f), new Vector2(60f, 8f),
                        Color.white, "Pitch", "Wall"));
            HideRenderer(CreateSolid("WallLeft", pitch, new Vector2(-WallInnerX - 10f, 1f), new Vector2(20f, 30f),
                        Color.white, "Pitch", "Wall"));
            HideRenderer(CreateSolid("WallRight", pitch, new Vector2(WallInnerX + 10f, 1f), new Vector2(20f, 30f),
                        Color.white, "Pitch", "Wall"));

            // ----- goals, ball, players: instances of the prefabs in Assets/Prefabs -----
            // One Goal prefab serves both ends: the right-hand goal is the same prefab
            // mirrored by its X scale, which flips the net art and the colliders together.
            PlaceGoal(Side.Left, pitch);
            PlaceGoal(Side.Right, pitch);

            GameObject ballObject = InstantiatePrefab("Ball", null);
            ballObject.transform.position = new Vector3(0f, 2f, 0f);
            var ball = ballObject.GetComponent<BallController>();

            GameObject leftPlayerObject = PlacePlayer(Side.Left);
            GameObject rightPlayerObject = PlacePlayer(Side.Right);

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
            WireAudio(audioObject.AddComponent<AudioManager>());

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

        // ------------------------------------------------------------------
        // Gameplay prefabs. Player, Ball and Goal are built once here, saved to
        // Assets/Prefabs, and the match scene is assembled from instances of them.
        // Fix a prefab and every instance follows; the scene only holds positions,
        // the side each instance plays for, and the mirror for the right-hand goal.
        // ------------------------------------------------------------------

        [MenuItem("Head Soccer/Rebuild Gameplay Prefabs", priority = 21)]
        public static void CreateGameplayPrefabs()
        {
            EnsureFolder(PrefabFolder);
            LoadArt();

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath);
            var ballMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(BallMaterialPath);

            CreateGoalPrefab();
            CreateBallPrefab(config, ballMaterial);
            CreatePlayerPrefab(config, roster);
        }

        private static GameObject SavePrefab(GameObject root, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject InstantiatePrefab(string name, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{name}.prefab");
            if (prefab == null)
                throw new System.InvalidOperationException($"Head Soccer: missing prefab {PrefabFolder}/{name}.prefab. Run Build Everything.");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) instance.transform.SetParent(parent, false);
            return instance;
        }

        /// <summary>
        /// The goal as it stands on the LEFT: root on the goal line at ground level, net
        /// receding to the left, mouth open toward the pitch. The right-hand goal is this
        /// prefab with X scale -1.
        /// </summary>
        private static void CreateGoalPrefab()
        {
            float depth = WallInnerX - GoalLineX;
            float mouthHeight = GoalMouthTopY - GroundTopY;

            var goal = new GameObject("Goal");

            if (goalSprite != null)
            {
                // goal.png is a side view with the mouth open to the RIGHT and the front
                // post on its right edge. The net is squeezed a little in depth so the
                // front post lands on the goal line and the back stays on screen.
                const float depthSqueeze = 0.8f;
                float artHeight = mouthHeight + 0.15f;
                float artWidth = artHeight * (goalSprite.bounds.size.x / goalSprite.bounds.size.y) * depthSqueeze;

                GameObject art = CreateFittedSprite("NetArt", goal.transform, goalSprite,
                    new Vector2(0.03f - artWidth * 0.5f, artHeight * 0.5f), artHeight, "Goals");
                Vector3 scale = art.transform.localScale;
                art.transform.localScale = new Vector3(Mathf.Abs(scale.x) * depthSqueeze, scale.y, 1f);
            }
            else
            {
                CreateSprite("Net", goal.transform, squareSprite, new Vector2(-depth * 0.5f, mouthHeight * 0.5f),
                    new Vector2(depth, mouthHeight), new Color(1f, 1f, 1f, 0.18f), "Goals");
            }

            HideRenderer(CreateSolid("Crossbar", goal.transform, new Vector2(-depth * 0.5f, mouthHeight + 0.15f),
                new Vector2(depth + 0.1f, 0.3f), Color.white, "Goals", "Wall"));
            HideRenderer(CreateSolid("BackPost", goal.transform, new Vector2(-depth, mouthHeight * 0.5f),
                new Vector2(0.18f, mouthHeight), Color.white, "Goals", "Wall"));

            // Sits far enough inside the net that the ball has fully crossed the line
            // by the time its centre enters the trigger.
            var mouth = new GameObject("GoalMouth");
            mouth.transform.SetParent(goal.transform);
            mouth.transform.localPosition = new Vector3(-(depth + BallRadius) * 0.5f, mouthHeight * 0.5f, 0f);
            mouth.layer = LayerMask.NameToLayer("Goal");

            var trigger = mouth.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(depth - BallRadius, mouthHeight - 0.1f);

            var goalTrigger = mouth.AddComponent<GoalTrigger>();
            Set(goalTrigger, "goalOwner", Side.Left);

            SavePrefab(goal, "Goal");
        }

        private static void PlaceGoal(Side side, Transform parent)
        {
            float sign = side == Side.Left ? -1f : 1f;
            GameObject goal = InstantiatePrefab("Goal", parent);
            goal.name = $"Goal{side}";
            goal.transform.position = new Vector3(sign * GoalLineX, GroundTopY, 0f);
            // Mirror the whole goal for the right-hand side: art, posts and trigger together.
            goal.transform.localScale = new Vector3(-sign, 1f, 1f);
            Set(goal.GetComponentInChildren<GoalTrigger>(), "goalOwner", side);
        }

        private static void CreateBallPrefab(GameConfig config, PhysicsMaterial2D ballMaterial)
        {
            var ballObject = new GameObject("Ball");
            ballObject.layer = LayerMask.NameToLayer("Ball");
            ballObject.tag = "Ball";

            Sprite usedBall = ballSprite != null ? ballSprite : circleSprite;
            if (usedBall != null)
                CreateFittedSprite("Visual", ballObject.transform, usedBall, Vector2.zero, BallRadius * 2f, "Ball");

            var ballBody = ballObject.AddComponent<Rigidbody2D>();
            ballBody.mass = 0.9f;
            ballBody.angularDamping = 0.4f;

            var ballCollider = ballObject.AddComponent<CircleCollider2D>();
            ballCollider.radius = BallRadius;
            ballCollider.sharedMaterial = ballMaterial;

            var ball = ballObject.AddComponent<BallController>();
            Set(ball, "config", config);

            SavePrefab(ballObject, "Ball");
        }

        /// <summary>
        /// One Player prefab for both sides. It is built as Player 1 (left, attacking
        /// right); PlacePlayer overrides the side on the instance, and at runtime
        /// PlayerController swaps in the chosen character's drawings from the roster.
        /// </summary>
        private static void CreatePlayerPrefab(GameConfig config, CharacterRoster roster)
        {
            var root = new GameObject("Player");
            root.layer = LayerMask.NameToLayer("Player");

            if (circleSprite != null)
            {
                CreateSprite("Shadow", root.transform, circleSprite,
                    new Vector2(0f, GroundTopY + 0.06f - PlayerRootY),
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
            Set(controller, "side", Side.Left);
            Set(controller, "inputMode", InputMode.Auto);
            Set(controller, "groundCheck", groundCheck);
            Set(controller, "kickHitbox", kickHitbox);
            Set(controller, "roster", roster);
            Set(controller, "groundLayer",
                (1 << LayerMask.NameToLayer("Ground")) | (1 << LayerMask.NameToLayer("Wall")));

            if (playerRedSprite != null)
            {
                // Character PNGs are trimmed to their content, so this is the real body height,
                // matched to the capsule + head colliders (-1.0 .. 1.1).
                GameObject visual = CreateFittedSprite("Visual", root.transform, playerRedSprite,
                    new Vector2(0f, 0.05f), PlayerHeight, "Players");
                var pose = visual.AddComponent<PlayerVisual>();
                Set(pose, "player", controller);
                Set(pose, "spriteRenderer", visual.GetComponent<SpriteRenderer>());
                Set(pose, "spriteFacesRight", true);
                Set(controller, "visual", pose);
            }
            else
            {
                CreateSprite("Body", root.transform, squareSprite, new Vector2(0f, -0.4f),
                    new Vector2(0.9f, 1.2f), RedTeam, "Players");
                CreateSprite("Head", root.transform, circleSprite, new Vector2(0f, 0.55f),
                    Vector2.one * 1.1f, Color.white, "Players");
            }

            // A small "!" above the head, shown only while the Super is ready.
            var mark = new GameObject("SuperMark");
            mark.transform.SetParent(root.transform);
            mark.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var markText = mark.AddComponent<TextMeshPro>();
            markText.text = "!";
            markText.fontSize = 7f;
            markText.color = Accent;
            markText.alignment = TextAlignmentOptions.Center;
            markText.rectTransform.sizeDelta = new Vector2(1f, 1f);
            if (font != null) markText.font = font;
            markText.GetComponent<MeshRenderer>().sortingLayerName = "FX";
            var readySign = root.AddComponent<SuperReadySign>();
            Set(readySign, "player", controller);
            Set(readySign, "mark", mark.transform);
            mark.SetActive(false);

            SavePrefab(root, "Player");
        }

        private static GameObject PlacePlayer(Side side)
        {
            float sign = side == Side.Left ? -1f : 1f;
            GameObject player = InstantiatePrefab("Player", null);
            player.name = $"Player{(side == Side.Left ? 1 : 2)}";
            player.transform.position = new Vector3(sign * PlayerSpawnX, PlayerRootY, 0f);

            var controller = player.GetComponent<PlayerController>();
            Set(controller, "side", side);

            var pose = player.GetComponentInChildren<PlayerVisual>();
            if (pose != null) Set(pose, "spriteFacesRight", side == Side.Left);
            return player;
        }

        private static void WireAudio(AudioManager audio)
        {
            // Clips live in Assets/Audio; every field stays optional.
            TrySetClip(audio, "kick", "kick");
            TrySetClip(audio, "bounce", "bounce");
            TrySetClip(audio, "jump", "jump");
            TrySetClip(audio, "whistle", "whistle");
            TrySetClip(audio, "goal", "goal");
            TrySetClip(audio, "crowdCheer", "crowd_cheer");
            TrySetClip(audio, "countdownBeep", "beep");
            TrySetClip(audio, "special", "special");
            TrySetClip(audio, "uiClick", "ui_click");
            TrySetClip(audio, "musicLoop", "music_loop");
        }

        private static void TrySetClip(AudioManager audio, string field, string clipName)
        {
            string[] guids = AssetDatabase.FindAssets($"{clipName} t:AudioClip", new[] { "Assets/Audio" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) != clipName) continue;
                Set(audio, field, AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                return;
            }
        }

        // ================================================================== match UI

        private static void BuildMatchUI(Camera camera, GameObject leftPlayer, GameObject rightPlayer)
        {
            Canvas canvas = CreateCanvas(camera);
            var uiManager = canvas.gameObject.AddComponent<UIManager>();

            // Full screen tint that flashes in the scorer's colour for half a second.
            GameObject flash = CreateUIObject("GoalFlash", canvas.transform);
            Stretch(flash.GetComponent<RectTransform>());
            var flashImage = flash.AddComponent<Image>();
            flashImage.color = new Color(1f, 1f, 1f, 0f);
            flashImage.raycastTarget = false;
            flash.SetActive(false);

            // Everything the player has to see or touch lives inside the safe area, so a
            // notch or a gesture bar never covers the score or the kick button.
            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SafeAreaFitter>();
            Transform hud = safeArea.transform;

            // --- scoreboard, top centre ------------------------------------------
            GameObject board = CreatePanel(hud, "Scoreboard", PanelDark,
                new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(520f, 104f), new Vector2(0.5f, 1f));

            // Left card: red team.
            TextMeshProUGUI leftName = CreateText(board.transform, "P1Name", "P1", 18,
                new Vector2(0f, 1f), new Vector2(75f, -18f), new Vector2(130f, 24f), RedTeam);
            leftName.characterSpacing = 6f;
            TextMeshProUGUI leftScore = CreateText(board.transform, "P1Score", "0", 60,
                new Vector2(0f, 0.5f), new Vector2(75f, -4f), new Vector2(130f, 64f), Color.white);
            Image leftSuper = CreateMeter(board.transform, "P1Super", new Vector2(0f, 0f), new Vector2(75f, 10f),
                new Vector2(100f, 8f), RedTeam);

            // Right card: blue team.
            TextMeshProUGUI rightName = CreateText(board.transform, "P2Name", "P2", 18,
                new Vector2(1f, 1f), new Vector2(-75f, -18f), new Vector2(130f, 24f), BlueTeam);
            rightName.characterSpacing = 6f;
            TextMeshProUGUI rightScore = CreateText(board.transform, "P2Score", "0", 60,
                new Vector2(1f, 0.5f), new Vector2(-75f, -4f), new Vector2(130f, 64f), Color.white);
            Image rightSuper = CreateMeter(board.transform, "P2Super", new Vector2(1f, 0f), new Vector2(-75f, 10f),
                new Vector2(100f, 8f), BlueTeam);

            // Clock in the middle on its own accent card.
            GameObject clockCard = CreatePanel(board.transform, "ClockCard", Accent,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(150f, 64f), new Vector2(0.5f, 0.5f));
            TextMeshProUGUI timer = CreateText(clockCard.transform, "Timer", "1:30", 44,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 64f), new Color(0.08f, 0.09f, 0.12f));
            timer.characterSpacing = 2f;

            TextMeshProUGUI leftSuperLabel = CreateText(hud, "P1SuperLabel", "SUPER", 16,
                new Vector2(0.5f, 1f), new Vector2(-190f, -132f), new Vector2(160f, 24f), TextDim);
            leftSuperLabel.characterSpacing = 4f;
            TextMeshProUGUI rightSuperLabel = CreateText(hud, "P2SuperLabel", "SUPER", 16,
                new Vector2(0.5f, 1f), new Vector2(190f, -132f), new Vector2(160f, 24f), TextDim);
            rightSuperLabel.characterSpacing = 4f;

            // Big centred message: 3 2 1 GO! and GOAL!
            // A dark copy underneath acts as a drop shadow; TMP outlines need a material
            // instance that does not survive a scene save, so this is the safe way.
            CreateText(canvas.transform, "CountdownShadow", string.Empty, 150,
                new Vector2(0.5f, 0.5f), new Vector2(5f, 54f), new Vector2(900f, 220f), new Color(0f, 0f, 0f, 0.6f));
            TextMeshProUGUI countdown = CreateText(canvas.transform, "Countdown", string.Empty, 150,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(900f, 220f), Accent);

            // --- pause button, top right -------------------------------------------
            Button pauseButton = CreateButton(hud, "PauseButton", "II", 30, ButtonNeutral,
                new Vector2(1f, 1f), new Vector2(-48f, -44f), new Vector2(64f, 64f));
            UnityEventTools.AddVoidPersistentListener(pauseButton.onClick, uiManager.OnPauseButton);

            // --- pause overlay ------------------------------------------------------
            GameObject pausePanel = CreateOverlay(canvas.transform, "PausePanel");
            GameObject pauseCard = CreatePanel(pausePanel.transform, "Card", PanelCard,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(440f, 420f), new Vector2(0.5f, 0.5f));
            CreateText(pauseCard.transform, "PausedTitle", "PAUSED", 64,
                new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(400f, 90f), Accent);

            Button resume = CreateButton(pauseCard.transform, "ResumeButton", "RESUME", 30, ButtonGreen,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(320f, 66f));
            Button restart = CreateButton(pauseCard.transform, "RestartButton", "RESTART", 30, ButtonNeutral,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(320f, 66f));
            Button quit = CreateButton(pauseCard.transform, "QuitButton", "MAIN MENU", 30, ButtonRed,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -140f), new Vector2(320f, 66f));

            UnityEventTools.AddVoidPersistentListener(resume.onClick, uiManager.OnResumeButton);
            UnityEventTools.AddVoidPersistentListener(restart.onClick, uiManager.OnRestartButton);
            UnityEventTools.AddVoidPersistentListener(quit.onClick, uiManager.OnQuitToMenuButton);

            // --- match over overlay --------------------------------------------------
            GameObject overPanel = CreateOverlay(canvas.transform, "MatchOverPanel");
            GameObject overCard = CreatePanel(overPanel.transform, "Card", PanelCard,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 400f), new Vector2(0.5f, 0.5f));
            CreateText(overCard.transform, "FullTime", "FULL TIME", 22,
                new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(400f, 30f), TextDim).characterSpacing = 8f;
            TextMeshProUGUI winner = CreateText(overCard.transform, "WinnerText", "PLAYER 1 WINS!", 60,
                new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(620f, 90f), Accent);
            TextMeshProUGUI finalScore = CreateText(overCard.transform, "FinalScoreText", "0  -  0", 72,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(500f, 90f), Color.white);
            TextMeshProUGUI record = CreateText(overCard.transform, "RecordText", "Best win: none yet", 18,
                new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(600f, 26f), TextDim);

            Button rematch = CreateButton(overCard.transform, "RematchButton", "REMATCH", 30, ButtonGreen,
                new Vector2(0.5f, 0f), new Vector2(-150f, 60f), new Vector2(260f, 66f));
            Button toMenu = CreateButton(overCard.transform, "MenuButton", "MAIN MENU", 30, ButtonNeutral,
                new Vector2(0.5f, 0f), new Vector2(150f, 60f), new Vector2(260f, 66f));

            UnityEventTools.AddVoidPersistentListener(rematch.onClick, uiManager.OnRestartButton);
            UnityEventTools.AddVoidPersistentListener(toMenu.onClick, uiManager.OnQuitToMenuButton);

            // --- touch controls -------------------------------------------------------
            GameObject touchPanel = CreateUIObject("TouchControls", hud);
            Stretch(touchPanel.GetComponent<RectTransform>());
            var touchSource = touchPanel.AddComponent<TouchInputSource>();

            CreateHoldButton(touchPanel.transform, "TouchLeft", "<", touchSource, TouchAction.Left,
                new Vector2(0f, 0f), new Vector2(100f, 100f), new Vector2(140f, 140f));
            CreateHoldButton(touchPanel.transform, "TouchRight", ">", touchSource, TouchAction.Right,
                new Vector2(0f, 0f), new Vector2(260f, 100f), new Vector2(140f, 140f));
            CreateHoldButton(touchPanel.transform, "TouchJump", "JUMP", touchSource, TouchAction.Jump,
                new Vector2(1f, 0f), new Vector2(-260f, 100f), new Vector2(140f, 140f));
            CreateHoldButton(touchPanel.transform, "TouchKick", "KICK", touchSource, TouchAction.Kick,
                new Vector2(1f, 0f), new Vector2(-100f, 100f), new Vector2(140f, 140f));

            // Both characters can be driven from the same panel; only the one whose
            // input mode resolves to Touch actually listens to it.
            Set(leftPlayer.GetComponent<PlayerController>(), "touchSource", touchSource);
            Set(rightPlayer.GetComponent<PlayerController>(), "touchSource", touchSource);

            Set(uiManager, "leftScoreText", leftScore);
            Set(uiManager, "rightScoreText", rightScore);
            Set(uiManager, "leftNameText", leftName);
            Set(uiManager, "rightNameText", rightName);
            Set(uiManager, "timerText", timer);
            Set(uiManager, "countdownText", countdown);
            Set(uiManager, "leftSuperFill", leftSuper);
            Set(uiManager, "rightSuperFill", rightSuper);
            Set(uiManager, "leftSuperLabel", leftSuperLabel);
            Set(uiManager, "rightSuperLabel", rightSuperLabel);
            Set(uiManager, "goalFlash", flashImage);
            Set(uiManager, "pausePanel", pausePanel);
            Set(uiManager, "matchOverPanel", overPanel);
            Set(uiManager, "winnerText", winner);
            Set(uiManager, "finalScoreText", finalScore);
            Set(uiManager, "recordText", record);
            Set(uiManager, "touchControlsPanel", touchPanel);

            pausePanel.SetActive(false);
            overPanel.SetActive(false);

            CreateEventSystem();
        }

        // ================================================================== menu scene

        private static void BuildMenuScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            LoadArt();
            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath);

            Camera camera = CreateCamera(new Color(0.05f, 0.12f, 0.08f));
            CreateGlobalLight();

            if (stadiumSprite != null)
            {
                GameObject bg = CreateFittedSprite("Stadium", null, stadiumSprite, new Vector2(0f, 0.35f), 10.4f, "Background");
                bg.GetComponent<SpriteRenderer>().color = new Color(0.55f, 0.6f, 0.7f, 1f);   // dimmed behind the menu
            }

            var audioObject = new GameObject("AudioManager");
            WireAudio(audioObject.AddComponent<AudioManager>());

            Canvas canvas = CreateCanvas(camera);
            var menu = canvas.gameObject.AddComponent<MainMenuController>();
            Set(menu, "matchSceneName", "Match");

            GameObject safeArea = CreateUIObject("SafeArea", canvas.transform);
            Stretch(safeArea.GetComponent<RectTransform>());
            safeArea.AddComponent<SafeAreaFitter>();

            // --- main panel ------------------------------------------------------------
            GameObject mainPanel = CreateUIObject("MainPanel", safeArea.transform);
            Stretch(mainPanel.GetComponent<RectTransform>());

            CreateText(mainPanel.transform, "TitleShadow", "HEAD SOCCER", 112,
                new Vector2(0.5f, 1f), new Vector2(5f, -126f), new Vector2(1000f, 140f), new Color(0f, 0f, 0f, 0.6f))
                .characterSpacing = 6f;
            TextMeshProUGUI title = CreateText(mainPanel.transform, "Title", "HEAD SOCCER", 112,
                new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1000f, 140f), Accent);
            title.characterSpacing = 6f;
            CreateText(mainPanel.transform, "Subtitle", "ONE SCREEN.  TWO HEADS.  FIVE GOALS.", 22,
                new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(800f, 30f), TextDim).characterSpacing = 6f;

            if (playerRedSprite != null)
                CreateUIImage(mainPanel.transform, "HeroLeft", playerRedSprite,
                    new Vector2(0f, 0f), new Vector2(150f, 190f), new Vector2(260f, 350f));
            if (playerBlueSprite != null)
            {
                Image hero = CreateUIImage(mainPanel.transform, "HeroRight", playerBlueSprite,
                    new Vector2(1f, 0f), new Vector2(-150f, 190f), new Vector2(260f, 350f));
                hero.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            }

            Button play = CreateButton(mainPanel.transform, "PlayButton", "PLAY  VS  CPU", 34, ButtonGreen,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(400f, 76f));
            Button twoPlayers = CreateButton(mainPanel.transform, "TwoPlayerButton", "2 PLAYERS", 34, ButtonNeutral,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(400f, 76f));
            Button difficulty = CreateButton(mainPanel.transform, "DifficultyButton", "CPU: MEDIUM", 24, ButtonNeutral,
                new Vector2(0.5f, 0.5f), new Vector2(-105f, -165f), new Vector2(190f, 56f));
            Button audio = CreateButton(mainPanel.transform, "AudioButton", "SOUND: ON", 24, ButtonNeutral,
                new Vector2(0.5f, 0.5f), new Vector2(105f, -165f), new Vector2(190f, 56f));

            UnityEventTools.AddVoidPersistentListener(play.onClick, menu.PlayVsCPU);
            UnityEventTools.AddVoidPersistentListener(twoPlayers.onClick, menu.PlayTwoPlayers);
            UnityEventTools.AddVoidPersistentListener(difficulty.onClick, menu.CycleDifficulty);
            UnityEventTools.AddVoidPersistentListener(audio.onClick, menu.ToggleAudio);

            Set(menu, "difficultyLabel", difficulty.GetComponentInChildren<TextMeshProUGUI>());
            Set(menu, "audioLabel", audio.GetComponentInChildren<TextMeshProUGUI>());

            CreateText(mainPanel.transform, "Hint",
                "P1   A / D move    W jump    SPACE kick          P2   ARROWS move    UP jump    RIGHT CTRL kick",
                18, new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(1200f, 30f), TextDim);

            // --- character select panel ----------------------------------------------
            GameObject characterPanel = CreateUIObject("CharacterPanel", safeArea.transform);
            Stretch(characterPanel.GetComponent<RectTransform>());
            var select = characterPanel.AddComponent<CharacterSelect>();
            Set(select, "roster", roster);

            CreateText(characterPanel.transform, "Title", "CHOOSE YOUR PLAYER", 64,
                new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(900f, 90f), Accent);

            GameObject card = CreatePanel(characterPanel.transform, "Card", PanelCard,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(760f, 420f), new Vector2(0.5f, 0.5f));

            Image portrait = CreateUIImage(card.transform, "Portrait", playerRedSprite,
                new Vector2(0f, 0.5f), new Vector2(190f, 0f), new Vector2(230f, 310f));
            // Loops each character's signature celebration while he is on the card.
            var celebration = portrait.gameObject.AddComponent<CelebrationLoop>();

            TextMeshProUGUI nameText = CreateText(card.transform, "Name", Characters[0].name.ToUpperInvariant(), 56,
                new Vector2(1f, 1f), new Vector2(-220f, -70f), new Vector2(360f, 70f), Color.white);
            nameText.alignment = TextAlignmentOptions.Left;
            TextMeshProUGUI statText = CreateText(card.transform, "StatHint", "Fast and bouncy", 20,
                new Vector2(1f, 1f), new Vector2(-220f, -122f), new Vector2(360f, 40f), TextDim);
            statText.alignment = TextAlignmentOptions.TopLeft;

            Image speedBar = CreateStatRow(card.transform, "SPEED", new Vector2(-220f, -190f), RedTeam);
            Image jumpBar = CreateStatRow(card.transform, "JUMP", new Vector2(-220f, -240f), Accent);
            Image powerBar = CreateStatRow(card.transform, "POWER", new Vector2(-220f, -290f), BlueTeam);

            Button prev = CreateButton(card.transform, "PrevButton", "<", 40, ButtonNeutral,
                new Vector2(0f, 0.5f), new Vector2(50f, 0f), new Vector2(60f, 90f));
            Button next = CreateButton(card.transform, "NextButton", ">", 40, ButtonNeutral,
                new Vector2(0f, 0.5f), new Vector2(330f, 0f), new Vector2(60f, 90f));
            UnityEventTools.AddVoidPersistentListener(prev.onClick, select.Previous);
            UnityEventTools.AddVoidPersistentListener(next.onClick, select.Next);

            Button confirm = CreateButton(characterPanel.transform, "ConfirmButton", "KICK OFF!", 34, ButtonGreen,
                new Vector2(0.5f, 0f), new Vector2(120f, 70f), new Vector2(300f, 72f));
            Button back = CreateButton(characterPanel.transform, "BackButton", "BACK", 28, ButtonNeutral,
                new Vector2(0.5f, 0f), new Vector2(-170f, 70f), new Vector2(200f, 72f));
            UnityEventTools.AddVoidPersistentListener(confirm.onClick, menu.StartMatch);
            UnityEventTools.AddVoidPersistentListener(back.onClick, menu.BackToMain);

            Set(select, "portrait", portrait);
            Set(select, "celebration", celebration);
            Set(select, "nameText", nameText);
            Set(select, "statText", statText);
            Set(select, "speedBar", speedBar);
            Set(select, "jumpBar", jumpBar);
            Set(select, "powerBar", powerBar);

            Set(menu, "mainPanel", mainPanel);
            Set(menu, "characterPanel", characterPanel);
            characterPanel.SetActive(false);

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static Image CreateStatRow(Transform parent, string label, Vector2 position, Color color)
        {
            TextMeshProUGUI text = CreateText(parent, label + "Label", label, 18,
                new Vector2(1f, 1f), position + new Vector2(-130f, 0f), new Vector2(100f, 26f), TextDim);
            text.alignment = TextAlignmentOptions.Left;
            text.characterSpacing = 4f;
            return CreateMeter(parent, label + "Bar", new Vector2(1f, 1f), position + new Vector2(20f, 0f),
                new Vector2(200f, 12f), color);
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
                                                  Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = CreateUIObject(name, parent);
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            var text = go.AddComponent<TextMeshProUGUI>();
            if (font != null) text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        /// <summary>A rounded rectangle. Pivot lets the scoreboard hang from the top edge.</summary>
        private static GameObject CreatePanel(Transform parent, string name, Color color, Vector2 anchor,
                                              Vector2 anchoredPosition, Vector2 size, Vector2 pivot)
        {
            GameObject go = CreateUIObject(name, parent);
            var rect = go.GetComponent<RectTransform>();
            Place(rect, anchor, anchoredPosition, size);
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;

            var image = go.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static Image CreateUIImage(Transform parent, string name, Sprite sprite, Vector2 anchor,
                                           Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPosition, size);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>A thin track with a horizontal fill bar. Returns the fill Image.</summary>
        private static Image CreateMeter(Transform parent, string name, Vector2 anchor, Vector2 anchoredPosition,
                                         Vector2 size, Color fillColor)
        {
            GameObject track = CreateUIObject(name, parent);
            Place(track.GetComponent<RectTransform>(), anchor, anchoredPosition, size);
            var trackImage = track.AddComponent<Image>();
            trackImage.sprite = roundedSprite;
            trackImage.type = Image.Type.Sliced;
            trackImage.color = new Color(1f, 1f, 1f, 0.15f);
            trackImage.raycastTarget = false;

            GameObject fill = CreateUIObject("Fill", track.transform);
            Stretch(fill.GetComponent<RectTransform>());
            var fillImage = fill.AddComponent<Image>();
            fillImage.sprite = roundedSprite;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 0f;
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;
            return fillImage;
        }

        private static Button CreateButton(Transform parent, string name, string label, int fontSize, Color color,
                                           Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = CreateUIObject(name, parent);
            Place(go.GetComponent<RectTransform>(), anchor, anchoredPosition, size);

            var image = go.AddComponent<Image>();
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = color;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, fontSize,
                new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);
            text.characterSpacing = 3f;
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
            image.sprite = roundedSprite;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.22f);

            var hold = go.AddComponent<HoldButton>();
            Set(hold, "target", source);
            Set(hold, "action", action);

            TextMeshProUGUI text = CreateText(go.transform, "Label", label, 30,
                new Vector2(0.5f, 0.5f), Vector2.zero, size, Color.white);
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
