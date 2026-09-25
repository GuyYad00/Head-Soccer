using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace HeadSoccer
{
    /// <summary>
    /// Builds the on-pitch look from sprites and transforms at runtime:
    /// big-head characters with no photo frame, and side-view goals with a
    /// diamond net like a classic Head Soccer goal.
    /// </summary>
    public static class MatchLook
    {
        private static bool applied;
        private static Sprite whiteSprite;
        private static Sprite circleSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            applied = false;
            if (Object.FindFirstObjectByType<GameManager>() == null) return;
            Apply();
        }

        public static void Apply()
        {
            if (applied) return;
            applied = true;

            HideNamedRenderers(
                "Ground", "Ceiling", "WallLeft", "WallRight",
                "Background", "CentreLine", "CentreCircle");

            BuildVenue();
            DressGoals();
            DressBall();
            DressBigHead("Player1", new Color(0.92f, 0.28f, 0.28f), new Color(1f, 0.82f, 0.68f));
            DressBigHead("Player2", new Color(0.25f, 0.45f, 0.92f), new Color(0.92f, 0.75f, 0.58f));
            HideBlockingUi();
            EnsureLight();

            var hint = Object.FindFirstObjectByType<ControlsHint>();
            if (hint != null) Object.Destroy(hint.gameObject);

            if (Object.FindFirstObjectByType<MatchScreens>() == null)
                new GameObject("MatchScreens").AddComponent<MatchScreens>();
        }

        // ---------------------------------------------------------------- pitch & crowd

        private static void BuildVenue()
        {
            Transform old = FindByName("Venue");
            if (old != null) Object.Destroy(old.gameObject);

            var venue = new GameObject("Venue").transform;
            if (Camera.main != null)
                Camera.main.backgroundColor = new Color(0.12f, 0.22f, 0.38f);

            Quad("Sky", venue, new Vector2(0f, 1.6f), new Vector2(28f, 10f),
                new Color(0.18f, 0.38f, 0.62f), "Background", -5);

            Quad("StandBack", venue, new Vector2(0f, 0.35f), new Vector2(24f, 3.4f),
                new Color(0.18f, 0.2f, 0.24f), "Background", -4);
            Quad("StandRail", venue, new Vector2(0f, -1.12f), new Vector2(24f, 0.12f),
                new Color(0.55f, 0.55f, 0.58f), "Background", -2);
            Quad("Roof", venue, new Vector2(0f, 2.15f), new Vector2(24f, 0.28f),
                new Color(0.12f, 0.13f, 0.16f), "Background", -2);

            BuildCrowd(venue);
            BuildGrass(venue);

            Quad("CentreLine", venue, new Vector2(0f, -2.55f), new Vector2(0.07f, 1.7f),
                new Color(1f, 1f, 1f, 0.7f), "Pitch", 2);
        }

        private static void BuildGrass(Transform venue)
        {
            const float groundY = -3.4f;
            const float bottomY = -5.4f;
            float midY = (groundY + bottomY) * 0.5f;
            float height = groundY - bottomY;

            Color dark = new Color(0.20f, 0.55f, 0.24f);
            Color light = new Color(0.30f, 0.68f, 0.30f);

            Quad("GrassBase", venue, new Vector2(0f, midY), new Vector2(28f, height), dark, "Pitch", 0);

            const float stripe = 1.15f;
            int n = 0;
            for (float x = -12f; x <= 12f; x += stripe)
            {
                bool pale = n % 2 == 0;
                Quad($"Stripe{n}", venue, new Vector2(x, midY), new Vector2(stripe * 0.92f, height),
                    pale ? light : dark, "Pitch", 1);
                n++;
            }

            Quad("Touchline", venue, new Vector2(0f, groundY + 0.03f), new Vector2(22f, 0.05f),
                new Color(1f, 1f, 1f, 0.55f), "Pitch", 3);
        }

        private static readonly Color[] SkinTones =
        {
            new Color(1.00f, 0.84f, 0.70f),
            new Color(0.93f, 0.74f, 0.58f),
            new Color(0.76f, 0.55f, 0.40f),
            new Color(0.55f, 0.36f, 0.24f),
            new Color(0.36f, 0.22f, 0.16f)
        };

        private static readonly Color[] ShirtColors =
        {
            new Color(0.90f, 0.18f, 0.18f),
            new Color(0.18f, 0.38f, 0.88f),
            new Color(0.95f, 0.95f, 0.95f),
            new Color(0.12f, 0.12f, 0.14f),
            new Color(0.95f, 0.78f, 0.15f),
            new Color(0.15f, 0.65f, 0.30f),
            new Color(0.70f, 0.20f, 0.75f),
            new Color(0.95f, 0.45f, 0.12f)
        };

        private static readonly Color[] HairColors =
        {
            new Color(0.12f, 0.08f, 0.06f),
            new Color(0.35f, 0.20f, 0.10f),
            new Color(0.62f, 0.38f, 0.16f),
            new Color(0.82f, 0.68f, 0.28f),
            new Color(0.55f, 0.18f, 0.10f),
            new Color(0.75f, 0.75f, 0.78f)
        };

        private static void BuildCrowd(Transform venue)
        {
            var rng = new System.Random(2026);
            float[] rows = { -0.72f, 0.02f, 0.78f, 1.52f };
            int id = 0;

            foreach (float y in rows)
            {
                for (float x = -10.4f; x <= 10.4f; x += 0.46f)
                {
                    float jitterX = (float)(rng.NextDouble() - 0.5) * 0.14f;
                    float jitterY = (float)(rng.NextDouble() - 0.5) * 0.06f;
                    SpawnFan(venue, id, new Vector2(x + jitterX, y + jitterY), rng);
                    id++;
                }
            }
        }

        private static void SpawnFan(Transform venue, int id, Vector2 position, System.Random rng)
        {
            float scale = 0.82f + (float)rng.NextDouble() * 0.38f;
            int lean = rng.Next(3) - 1;

            var root = new GameObject($"Fan{id}");
            root.transform.SetParent(venue);
            root.transform.position = new Vector3(position.x, position.y, 0f);
            root.transform.localScale = new Vector3(scale, scale, 1f);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, lean * 6f);

            Color skin = SkinTones[rng.Next(SkinTones.Length)];
            Color shirt = ShirtColors[rng.Next(ShirtColors.Length)];
            Color hair = HairColors[rng.Next(HairColors.Length)];
            Color sleeve = Color.Lerp(shirt, Color.black, 0.15f);

            Piece("Torso", root.transform, new Vector2(0f, -0.02f), new Vector2(0.18f, 0.28f), shirt, "Background", -3);
            Circle("Head", root.transform, new Vector2(0f, 0.18f), 0.17f, skin, "Background", -2);

            double style = rng.NextDouble();
            if (style < 0.55)
            {
                Circle("Hair", root.transform, new Vector2(0f, 0.24f), 0.15f, hair, "Background", -3);
            }
            else if (style < 0.78)
            {
                Piece("Hat", root.transform, new Vector2(0f, 0.28f), new Vector2(0.20f, 0.07f), shirt, "Background", -1);
            }

            Transform left = Arm("LeftArm", root.transform, new Vector2(-0.09f, 0.06f), 1f, sleeve);
            Transform right = Arm("RightArm", root.transform, new Vector2(0.09f, 0.06f), -1f, sleeve);

            var wave = root.AddComponent<CrowdFan>();
            wave.leftArm = left;
            wave.rightArm = right;
            wave.phase = (float)rng.NextDouble() * Mathf.PI * 2f;
            wave.speed = 3.2f + (float)rng.NextDouble() * 2.4f;
            wave.amount = 18f + (float)rng.NextDouble() * 16f;
        }

        private static Transform Arm(string name, Transform parent, Vector2 shoulder, float side, Color color)
        {
            var pivot = new GameObject(name);
            pivot.transform.SetParent(parent, false);
            pivot.transform.localPosition = new Vector3(shoulder.x, shoulder.y, 0f);

            Piece("Limb", pivot.transform, new Vector2(side * 0.07f, 0.02f), new Vector2(0.14f, 0.045f), color, "Background", -2);
            return pivot.transform;
        }

        // ---------------------------------------------------------------- goals

        private static void DressGoals()
        {
            BuildSideGoal(FindByName("GoalLeft"), left: true);
            BuildSideGoal(FindByName("GoalRight"), left: false);
        }

        /// <summary>
        /// Side-view goal: white frame and a diamond net, assembled from bars.
        /// </summary>
        private static void BuildSideGoal(Transform goal, bool left)
        {
            if (goal == null) return;

            foreach (SpriteRenderer renderer in goal.GetComponentsInChildren<SpriteRenderer>())
                renderer.enabled = false;

            Transform old = goal.Find("GoalMesh");
            if (old != null) Object.Destroy(old.gameObject);
            Transform photo = goal.Find("GoalArt");
            if (photo != null) Object.Destroy(photo.gameObject);

            var mesh = new GameObject("GoalMesh");
            mesh.transform.SetParent(goal);
            mesh.transform.localPosition = Vector3.zero;
            mesh.transform.localScale = Vector3.one;

            float sign = left ? -1f : 1f;
            float frontX = sign * 7.62f;
            float backX = sign * 8.78f;
            float groundY = -3.40f;
            float topY = -1.18f;
            float midY = (groundY + topY) * 0.5f;
            float height = topY - groundY;
            float width = Mathf.Abs(backX - frontX);
            float centreX = (frontX + backX) * 0.5f;

            Color post = new Color(0.96f, 0.96f, 0.98f);
            Color net = new Color(0.93f, 0.93f, 0.95f, 0.92f);

            Bar("NetFill", mesh.transform, new Vector2(centreX, midY), new Vector2(width, height),
                new Color(0.75f, 0.88f, 0.78f, 0.35f), -2);

            DiamondNet(mesh.transform, Mathf.Min(frontX, backX), Mathf.Max(frontX, backX), groundY, topY, net);

            Bar("PostFront", mesh.transform, new Vector2(frontX, midY), new Vector2(0.13f, height + 0.08f), post, 3);
            Bar("PostBack", mesh.transform, new Vector2(backX, midY), new Vector2(0.13f, height + 0.08f), post, 3);
            Bar("Crossbar", mesh.transform, new Vector2(centreX, topY), new Vector2(width + 0.18f, 0.13f), post, 4);
            Bar("GroundBar", mesh.transform, new Vector2(centreX, groundY + 0.04f), new Vector2(width + 0.18f, 0.08f), post, 4);
        }

        private static void DiamondNet(Transform parent, float xMin, float xMax, float yMin, float yMax, Color color)
        {
            const float inset = 0.07f;
            xMin += inset;
            xMax -= inset;
            yMin += inset;
            yMax -= inset;

            const float cell = 0.26f;
            const float thickness = 0.04f;
            float arm = cell * 1.15f;
            int n = 0;

            for (float x = xMin + cell * 0.5f; x < xMax; x += cell)
            {
                for (float y = yMin + cell * 0.5f; y < yMax; y += cell)
                {
                    Bar($"NetA{n}", parent, new Vector2(x, y), new Vector2(arm, thickness), color, 0, 45f);
                    Bar($"NetB{n}", parent, new Vector2(x, y), new Vector2(arm, thickness), color, 0, -45f);
                    n++;
                }
            }
        }

        // ---------------------------------------------------------------- players

        private static void DressBigHead(string objectName, Color jersey, Color skin)
        {
            Transform root = FindByName(objectName);
            if (root == null) return;

            foreach (Transform child in root)
            {
                if (child.name is "Body" or "Head" or "Eye" or "Shadow" or "Visual")
                {
                    if (child.name == "Visual")
                    {
                        Object.Destroy(child.gameObject);
                        continue;
                    }

                    SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                    if (renderer != null) renderer.enabled = false;
                }
            }

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            // Tiny body so the silhouette still reads as a player, but the head dominates.
            Piece("Shirt", visual.transform, new Vector2(0f, -0.55f), new Vector2(0.42f, 0.55f), jersey, "Players", 0);
            Piece("Shorts", visual.transform, new Vector2(0f, -0.88f), new Vector2(0.38f, 0.22f), Color.white, "Players", 0);
            Piece("LegL", visual.transform, new Vector2(-0.1f, -1.05f), new Vector2(0.1f, 0.22f), skin, "Players", 0);
            Piece("LegR", visual.transform, new Vector2(0.1f, -1.05f), new Vector2(0.1f, 0.22f), skin, "Players", 0);

            Circle("Head", visual.transform, new Vector2(0f, 0.55f), 1.28f, skin, "Players", 2);
            Circle("Hair", visual.transform, new Vector2(-0.02f, 0.78f), 1.05f, jersey, "Players", 1);
            Circle("EyeWhite", visual.transform, new Vector2(0.22f, 0.62f), 0.28f, Color.white, "Players", 3);
            Circle("Pupil", visual.transform, new Vector2(0.28f, 0.62f), 0.14f, new Color(0.12f, 0.1f, 0.1f), "Players", 4);
            Circle("Brow", visual.transform, new Vector2(0.22f, 0.78f), 0.22f, new Color(0.15f, 0.1f, 0.08f), "Players", 4);
            Piece("Mouth", visual.transform, new Vector2(0.18f, 0.38f), new Vector2(0.22f, 0.05f), new Color(0.55f, 0.2f, 0.2f), "Players", 4);

            var player = root.GetComponent<PlayerController>();
            var pose = visual.AddComponent<PlayerVisual>();
            SetPrivate(pose, "player", player);
            SetPrivate(pose, "spriteRenderer", visual.GetComponent<SpriteRenderer>());
            SetPrivate(pose, "spriteFacesRight", true);

            Transform oldCue = root.Find("SuperCue");
            if (oldCue != null) Object.Destroy(oldCue.gameObject);

            var cue = new GameObject("SuperCue");
            cue.transform.SetParent(root);
            cue.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            cue.SetActive(false);

            Circle("Glow", cue.transform, Vector2.zero, 0.55f, new Color(1f, 0.85f, 0.1f, 0.55f), "FX", 6);
            Piece("Bang", cue.transform, new Vector2(0f, 0.08f), new Vector2(0.1f, 0.32f), new Color(1f, 0.9f, 0.15f), "FX", 7);
            Circle("Dot", cue.transform, new Vector2(0f, -0.16f), 0.12f, new Color(1f, 0.9f, 0.15f), "FX", 7);

            var cueSign = root.GetComponent<SuperReadySign>() ?? root.gameObject.AddComponent<SuperReadySign>();
            cueSign.Setup(player, cue.transform);
        }

        // ---------------------------------------------------------------- ball

        private static void DressBall()
        {
            BallController ball = Object.FindFirstObjectByType<BallController>();
            if (ball == null) return;

            foreach (SpriteRenderer renderer in ball.GetComponentsInChildren<SpriteRenderer>())
                renderer.enabled = false;

            Transform old = ball.transform.Find("Visual");
            if (old != null) Object.Destroy(old.gameObject);

            Transform visual = Circle("Visual", ball.transform, Vector2.zero, 0.56f, Color.white, "Ball", 0).transform;
            Circle("Patch1", visual, new Vector2(-0.08f, 0.06f), 0.18f, Color.black, "Ball", 1);
            Circle("Patch2", visual, new Vector2(0.1f, -0.08f), 0.14f, Color.black, "Ball", 1);
        }

        // ---------------------------------------------------------------- helpers

        private static void HideNamedRenderers(params string[] names)
        {
            foreach (string name in names)
            {
                Transform found = FindByName(name);
                if (found == null) continue;
                foreach (SpriteRenderer renderer in found.GetComponentsInChildren<SpriteRenderer>())
                    renderer.enabled = false;
            }
        }

        private static void HideBlockingUi()
        {
            Transform pause = FindByName("PausePanel");
            if (pause != null) pause.gameObject.SetActive(false);
            Transform over = FindByName("MatchOverPanel");
            if (over != null) over.gameObject.SetActive(false);
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light2D>() != null) return;
            var lightObject = new GameObject("Global Light 2D");
            var light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        private static void Quad(string name, Transform parent, Vector2 position, Vector2 size,
                                 Color color, string layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = White();
            renderer.color = color;
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
        }

        private static void Bar(string name, Transform parent, Vector2 position, Vector2 size,
                                Color color, int order, float angle = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = White();
            renderer.color = color;
            renderer.sortingLayerName = "Goals";
            renderer.sortingOrder = order;
        }

        private static void Piece(string name, Transform parent, Vector2 local, Vector2 size,
                                  Color color, string layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(local.x, local.y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = White();
            renderer.color = color;
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
        }

        private static GameObject Circle(string name, Transform parent, Vector2 local, float diameter,
                                   Color color, string layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(local.x, local.y, 0f);
            go.transform.localScale = new Vector3(diameter, diameter, 1f);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Circle();
            renderer.color = color;
            renderer.sortingLayerName = layer;
            renderer.sortingOrder = order;
            return go;
        }

        private static Sprite White()
        {
            if (whiteSprite != null) return whiteSprite;
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 8f, 8f), new Vector2(0.5f, 0.5f), 8f);
            return whiteSprite;
        }

        private static Sprite Circle()
        {
            if (circleSprite != null) return circleSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f - 1f;
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                    float a = Mathf.Clamp01(r - d);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            texture.filterMode = FilterMode.Bilinear;
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            return circleSprite;
        }

        private static Transform FindByName(string name)
        {
            Transform[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform transform in all)
            {
                if (transform.name == name) return transform;
            }
            return null;
        }

        private static void SetPrivate(Object target, string field, object value)
        {
            var fieldInfo = target.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            fieldInfo?.SetValue(target, value);
        }
    }
}
