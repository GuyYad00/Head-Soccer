# Head Soccer

A 1v1 arcade Head Soccer game built in Unity 6 (URP, 2D). Two big-headed characters on one screen, arcade ball physics, 90-second matches to five goals, and a Super shot each player can fire once per match. Runs on Windows and as an Android APK with touch controls.

Final project for the Unity course. The approved design document is in [`Docs/GDD.md`](Docs/GDD.md). Decisions we made while planning and while playing each other are in [`Docs/design-decisions.md`](Docs/design-decisions.md). The Unity project is in [`Head-Soccer-main/`](Head-Soccer-main/).

## Screenshots

| Main menu | Character select |
|---|---|
| ![Main menu](Docs/images/screen-menu.png) | ![Character select](Docs/images/screen-character.png) |

| Match | Match over |
|---|---|
| ![Match](Docs/images/screen-match.png) | ![Match over](Docs/images/screen-matchover.png) |

## How to play

- **Goal:** put the ball in the other net. First to 5 goals, or the higher score when the 90-second clock hits zero, wins.
- **Kick:** no two kicks are the same. The ball leaves the foot at a random angle, a flat drive one time and a lob the next, and a kick on the run hits harder than a kick standing still.
- **Super:** staying on the ball fills your Super meter (under your score). When it reads SUPER READY, your next kick is a boosted shot with slow motion. One per match.

| Action | Player 1 | Player 2 | Gamepad | Touch |
|---|---|---|---|---|
| Move | A / D | Left / Right | Left stick / D-pad | ◄ ► buttons |
| Jump | W | Up | South (A / Cross) | JUMP |
| Kick / Super | Space | Right Ctrl | West or East | KICK |
| Pause | Esc | Esc | | II button |

The first gamepad drives Player 1, the second drives Player 2.

## The players

![Yossi, David, Kim and Mikel](Docs/images/characters.png)

The roster is our own art, drawn for this project and inspired by the makers of the game we set out to build. We wanted the players to feel different from each other, so we picked four countries and built one character around each of them. Each one has an idle drawing, a kick drawing and his own goal celebration, and each one plays a little differently in speed, jump and power.

| | | | Celebration |
|---|---|---|---|
| **Yossi** | Israel | Tough and confident, never gives up | Kisses the badge |
| **David** | England | Classy and precise, plays with style | Hands make a heart |
| **Kim** | Japan | Fast and focused, samurai balance | Bows |
| **Mikel** | Nigeria | Strong and full of energy | Backflip |

You pick yours on the character select screen before kickoff. The CPU plays the next one in the list.

## Select character window

![Choose your player: Yossi kisses the badge, David makes a heart, Kim bows, Mikel backflips](Docs/images/character-select.png)

This is the screen before kickoff. The portrait in the middle is alive: the player stands for a moment, hops into his own goal celebration, holds it, and drops back to idle, then does it again.

| | Celebration |
|---|---|
| **Yossi** | Kneels and kisses the badge on his shirt |
| **David** | Beats a heart with his hands |
| **Kim** | Bows to you, the player, not to the side |
| **Mikel** | Crouches, backflips and lands on his feet |

Arrows on the card change the character, the bars show speed, jump and power, and **KICK OFF!** starts the match. The celebration plays on this screen only. Once the match starts, the players go back to their idle and kick drawings.

## The stadium

Below the crowd runs an advertising board, the way every real ground has one. Football has always carried messages beyond the game itself, and we wanted the pitch to reflect that world rather than a sterile one. So the board shows real things: the shawarma place, the university this project was made for, the food app, the card company and the game everyone is waiting for.

## Run it

**Windows / editor**

1. Unity Hub → **Add** → `Head-Soccer-main` (Unity **6000.3.20f1**).
2. Open `Assets/Scenes/Menu.unity` and press **Play**.

**Android**

1. Install *Android Build Support* (with SDK, NDK and OpenJDK) for the same Unity version from Unity Hub.
2. In Unity: **Head Soccer → Build Android APK**. The APK is written to `Head-Soccer-main/Builds/Android/HeadSoccer.apk`.
3. Copy it to a phone and install. The game locks to landscape and shows on-screen buttons.

## Course concepts used

| Concept | Where |
|---|---|
| **Singleton** | `GameManager` is the only owner of match state, score and clock. `AudioManager`, `EffectsPool`, `UIManager` follow the same pattern. |
| **Object pool** | `EffectsPool` recycles a fixed set of kick sparks and goal confetti; nothing is instantiated during play. |
| **Coroutines** | 3-2-1 kickoff, goal celebration freeze, Super slow motion, kick hit-stop, score bump and goal flash in the HUD. |
| **Compile to mobile** | Android APK, touch controls, `SafeAreaFitter` for notches, `CameraFitter` keeps the whole pitch visible on any aspect ratio. |
| **ScriptableObjects** | `GameConfig` (all tuning numbers) and `CharacterRoster` (the selectable characters). |
| **Input System** | Keyboard, gamepad and touch behind one `IInputSource` interface, merged per player by `CompositeInputSource`. |
| **PlayerPrefs** | Chosen character, mute setting, best win and P1 win count. |

## Project layout

```
Head-Soccer-main/Assets
├── Art/        stadium, goal, ball, ad board and Characters/ (four players, idle + kick + celebration)
├── Audio/      synthesised SFX and the match music loop
├── Data/       GameConfig, CharacterRoster, ball physics material
├── Editor/     HeadSoccerBuilder (rebuilds both scenes), HeadSoccerBuildPipeline (one-click builds)
├── Fonts/      Oswald Bold (SIL Open Font License)
├── Prefabs/    pooled particle effects
├── Scenes/     Menu.unity, Match.unity
├── Scripts/
│   ├── Core/       GameManager, GameConfig, MatchState, MatchSettings, MatchRecords, CharacterRoster, CameraFitter
│   ├── Gameplay/   PlayerController, PlayerVisual, SpecialShot, KickHitbox, BallController, GoalTrigger, AIController
│   ├── Input/      IInputSource, Keyboard/Gamepad/Touch/Composite sources, HoldButton
│   ├── UI/         UIManager, MainMenuController, CharacterSelect, CelebrationLoop, SafeAreaFitter
│   ├── Audio/      AudioManager
│   └── Effects/    EffectsPool, CameraShake, SuperReadySign, AdBoard
└── UI/         RoundedPanel (9-sliced panel sprite)
```

## Design decisions

The write-up of why the kick, the boards, the celebrations and the goal height ended up the way they did is in [`Docs/design-decisions.md`](Docs/design-decisions.md). We add to it as we keep playing.

## Credits

- Design, code and art: Guy Yad Shalom and Tomer Yad Shalom.
- Font: [Oswald](https://fonts.google.com/specimen/Oswald) by Vernon Adams, SIL Open Font License 1.1.
- Sound effects and music were synthesised for this project; no third-party samples are used.
