using UnityEngine;
using UnityEngine.UI;

namespace HeadSoccer
{
    /// <summary>
    /// Pre-match board and the small in-game scoreboard. Built at runtime so it
    /// works even without rebuilding the scene, and it does not use TextMeshPro.
    /// </summary>
    public class MatchScreens : MonoBehaviour
    {
        private GameObject lobby;
        private GameObject hud;
        private Text hudScore;
        private Text hudTime;
        private Text recordsLabel;
        private Image leftSuperFill;
        private Image rightSuperFill;
        private Text leftSuperLabel;
        private Text rightSuperLabel;
        private GameManager game;

        private void Start()
        {
            game = GameManager.Instance;
            HideOldHud();
            BuildLobby();
            BuildHud();
            SetHudVisible(false);

            if (game != null)
            {
                game.StateChanged += OnState;
                game.ScoreChanged += OnScore;
                game.MatchEnded += OnEnded;
            }
        }

        private void OnDestroy()
        {
            if (game == null) return;
            game.StateChanged -= OnState;
            game.ScoreChanged -= OnScore;
            game.MatchEnded -= OnEnded;
        }

        private void Update()
        {
            if (game == null || hudTime == null || !hud.activeSelf) return;
            int seconds = Mathf.Max(0, Mathf.CeilToInt(game.TimeRemaining));
            hudTime.text = $"{seconds / 60}:{seconds % 60:00}";
            RefreshSuper(game.LeftPlayer, leftSuperFill, leftSuperLabel);
            RefreshSuper(game.RightPlayer, rightSuperFill, rightSuperLabel);
        }

        private static void RefreshSuper(PlayerController player, Image fill, Text label)
        {
            if (player == null || fill == null || label == null) return;

            if (player.SpecialUsed)
            {
                fill.fillAmount = 1f;
                fill.color = new Color(0.25f, 0.25f, 0.28f, 0.9f);
                label.text = "USED";
                return;
            }

            fill.fillAmount = player.SpecialCharge;
            if (player.SpecialReady)
            {
                fill.color = new Color(1f, 0.82f, 0.15f, 1f);
                label.text = "READY";
            }
            else
            {
                fill.color = new Color(0.35f, 0.75f, 1f, 0.95f);
                label.text = "SUPER";
            }
        }

        private void OnState(MatchState state)
        {
            bool inLobby = state == MatchState.Menu;
            if (lobby != null) lobby.SetActive(inLobby);
            SetHudVisible(!inLobby && state != MatchState.MatchOver);
        }

        private void OnScore(int left, int right)
        {
            if (hudScore != null)
                hudScore.text = $"P1   {left}   -   {right}   P2";
        }

        private void OnEnded(int winner)
        {
            SetHudVisible(false);
            if (recordsLabel != null)
                recordsLabel.text = MatchRecords.Summary();
        }

        // ---------------------------------------------------------------- build

        private void BuildLobby()
        {
            Canvas canvas = OverlayCanvas("LobbyCanvas", 20);
            lobby = Panel(canvas.transform, "Lobby", new Color(0f, 0.08f, 0.05f, 0.82f), fullScreen: true);

            Label(lobby.transform, "Title", "HEAD SOCCER", 64, new Vector2(0f, 210f), new Vector2(900f, 90f));
            Label(lobby.transform, "Sub", "1 minute  ·  Super once  ·  P1 Tab+Shift  ·  P2 Up+Down", 20,
                new Vector2(0f, 148f), new Vector2(980f, 40f));

            recordsLabel = Label(lobby.transform, "Records", MatchRecords.Summary(), 24,
                new Vector2(0f, 70f), new Vector2(700f, 80f));

            Button(lobby.transform, "PlayCpu", "PLAY  vs  CPU", new Vector2(0f, -20f), new Vector2(360f, 70f), () =>
            {
                MatchSettings.Mode = GameMode.OnePlayerVsCPU;
                game?.LaunchMatch();
            });

            Button(lobby.transform, "PlayTwo", "PLAY  2 PLAYERS", new Vector2(0f, -110f), new Vector2(360f, 70f), () =>
            {
                MatchSettings.Mode = GameMode.TwoPlayers;
                game?.LaunchMatch();
            });
        }

        private void BuildHud()
        {
            Canvas canvas = OverlayCanvas("HudCanvas", 15);
            hud = Panel(canvas.transform, "Scoreboard", new Color(0.05f, 0.07f, 0.1f, 0.72f), fullScreen: false);
            var rect = hud.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -12f);
            rect.sizeDelta = new Vector2(520f, 92f);

            hudScore = Label(hud.transform, "Score", "P1   0   -   0   P2", 28, new Vector2(0f, 18f), new Vector2(400f, 36f));
            hudTime = Label(hud.transform, "Time", "1:00", 20, new Vector2(0f, -8f), new Vector2(200f, 24f));
            leftSuperFill = SuperBar(hud.transform, "P1Super", new Vector2(-170f, -32f), out leftSuperLabel);
            rightSuperFill = SuperBar(hud.transform, "P2Super", new Vector2(170f, -32f), out rightSuperLabel);
        }

        private static Image SuperBar(Transform parent, string name, Vector2 anchored, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = new Vector2(140f, 14f);
            go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.9f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillGo.GetComponent<Image>();
            fill.color = new Color(0.35f, 0.75f, 1f, 0.95f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;

            label = Label(go.transform, "Cap", "SUPER", 12, new Vector2(0f, 12f), new Vector2(140f, 18f));
            return fill;
        }

        private void SetHudVisible(bool visible)
        {
            if (hud != null) hud.SetActive(visible);
        }

        private static void HideOldHud()
        {
            string[] names = { "LeftScore", "RightScore", "Dash", "Timer", "P1Label", "P2Label", "ControlsHint" };
            foreach (string name in names)
            {
                Transform found = GameObject.Find(name)?.transform;
                if (found != null) found.gameObject.SetActive(false);
            }
        }

        private static Canvas OverlayCanvas(string name, int sort)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sort;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static GameObject Panel(Transform parent, string name, Color color, bool fullScreen)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (fullScreen)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static Text Label(Transform parent, string name, string content, int size,
                                  Vector2 anchored, Vector2 dim)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = dim;

            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.font = BuiltinFont();
            text.raycastTarget = false;
            return text;
        }

        private static void Button(Transform parent, string name, string label, Vector2 anchored,
                                   Vector2 dim, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchored;
            rect.sizeDelta = dim;
            go.GetComponent<Image>().color = new Color(0.1f, 0.14f, 0.18f, 0.95f);
            go.GetComponent<Button>().onClick.AddListener(onClick);
            Label(go.transform, "Label", label, 28, Vector2.zero, dim);
        }

        private static Font BuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
