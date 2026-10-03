# Head Soccer

A 1v1 arcade Head Soccer game built in Unity 6 (URP, 2D). Two big-headed characters on one screen, arcade ball physics, 90-second matches to five goals, and a Super shot each player can fire once per match. Runs on Windows and as an Android APK with touch controls.

Final project for the Unity course. The approved design document is in [`Docs/GDD.md`](Docs/GDD.md). Decisions we made while planning and while playing each other are in [`Docs/design-decisions.md`](Docs/design-decisions.md). The Unity project is in [`Head-Soccer-main/`](Head-Soccer-main/).

<table>
<tr>
<td width="50%" align="center">
<img src="Docs/images/mikel-breaks-5-4.gif" alt="Mikel and Yossi at 4-4. Mikel scores the goal that makes it 5-4" width="100%">
<br>
<sub>Mikel and Yossi, 4-4. The goal that breaks it.</sub>
</td>
<td width="50%" align="center">
<img src="Docs/images/noa-goal-in-the-rain.gif" alt="Noa scores in the rain against Anna" width="100%">
<br>
<sub>Noa scores in the rain against Anna.</sub>
</td>
</tr>
</table>

## Screenshots

All from the Windows build.

<table>
<tr>
<th width="50%">Main menu</th>
<th width="50%">Character select</th>
</tr>
<tr>
<td width="50%"><img src="Docs/images/exe-menu.png" alt="Main menu: PLAY VS CPU, 2 PLAYERS, CPU MEDIUM, SOUND ON, P1 KEYS WASD, ABOUT" width="100%"></td>
<td width="50%"><img src="Docs/images/exe-character.png" alt="Player 1 picks Anna. The portrait is mid knee slide, with speed, jump and power, and NEXT" width="100%"></td>
</tr>
<tr>
<th width="50%">Match</th>
<th width="50%">Rain</th>
</tr>
<tr>
<td width="50%"><img src="Docs/images/exe-match-kim-yossi.jpg" alt="Kim 1 against Yossi 0, Super ready, Japanese crowd on the left and Israeli crowd on the right" width="100%"></td>
<td width="50%"><img src="Docs/images/exe-rain-anna-david.jpg" alt="Rain: Anna 3 against David 1, Ukrainian crowd on the left and English crowd on the right, both Supers used" width="100%"></td>
</tr>
<tr>
<th width="50%">Game over</th>
<th width="50%">Pause</th>
</tr>
<tr>
<td width="50%" align="center"><img src="Docs/images/exe-gameover-noa-david.jpg" alt="Full time: Noa wins 5-2 against David, Israeli crowd on the left and English crowd on the right" height="236"></td>
<td width="50%" align="center"><img src="Docs/images/exe-pause.png" alt="Paused" height="236"></td>
</tr>
</table>

<p align="center"><sub>Clear, rain or snow is rolled at kickoff. Each half of the stands fills with that side's crowd. A glow around a player means that Super is ready.</sub></p>

## Supported platforms

Windows and Android. Both were built and played. Keys on Windows, buttons on the phone.

<table>
<tr>
<th width="50%">Windows</th>
<th width="50%">Android</th>
</tr>
<tr>
<td width="50%"><img src="Docs/images/exe-windows-match.jpg" alt="Windows match: Mikel and Kim in the rain, keyboard play, the commentator in the crowd" width="100%"></td>
<td width="50%"><img src="Docs/images/android-match.jpg" alt="Android match: on-screen move, JUMP and KICK, pause and sound in the corner" width="100%"></td>
</tr>
<tr>
<td width="50%">

**How to run it**

1. Unity Hub → **Add** → `Head-Soccer-main` (Unity **6000.3.20f1**).
2. Open `Assets/Scenes/Menu.unity` and press **Play**. Keys: WASD or arrows, from **P1 KEYS** on the menu.
3. Standalone: **Head Soccer → Build Windows (x64)**. Open `Head-Soccer-main/Builds/Windows/HeadSoccer.exe`.

</td>
<td width="50%">

**How to run it**

1. Download `HeadSoccer.apk` from the [latest release](../../releases/latest). It has been installed and played on an Android phone.
2. Copy it to the phone and open it. Allow install from that source. The warning that it is not from the Play Store is expected.
3. The game locks to landscape. Move, JUMP and KICK are on the screen.
4. To build it: install *Android Build Support* (SDK, NDK, OpenJDK) for the same Unity version, then **Head Soccer → Build Android APK**. The file is `Head-Soccer-main/Builds/Android/HeadSoccer.apk` (IL2CPP, ARM64, Android 7.1 and up). In the editor, Game view → **Simulator** shows the same buttons; the mouse acts as a finger.

</td>
</tr>
</table>


## How to play

- **Goal:** first to 5, or the higher score when the 90-second clock hits zero.
- **Kick:** each kick leaves at a random angle, a flat drive or a lob, and a kick on the run hits harder.
- **Kickoff:** at the whistle, and again after every goal, the ball drops in the centre, bounces, and rolls to a stop.
- **Super:** stay on the ball until the meter reads SUPER READY. The next kick is a boosted shot in slow motion. One per match.

| Action | WASD layout (P1 default) | ARROWS layout (P2 default) | Gamepad | Touch |
|---|---|---|---|---|
| Move | A / D | Left / Right | Left stick / D-pad | ◄ ► buttons |
| Jump | W | Up | South (A / Cross) | JUMP |
| Kick / Super | Space | Right Ctrl | West or East | KICK |
| Pause | Esc | Esc | Start | II button |

Against the CPU, **P1 KEYS** on the main menu chooses WASD or the arrows, and the game remembers it. The first gamepad is Player 1, the second is Player 2.

## Characters & Commentators

![The six players on the select screen, each in their celebration](Docs/images/character-select.png)

| | | | Celebration |
|---|---|---|---|
| **Yossi** | Israel | Tough and confident, never gives up | Kisses the badge |
| **David** | England | Classy and precise, plays with style | Hands make a heart |
| **Kim** | Japan | Fast and focused, samurai balance | Bows |
| **Mikel** | Nigeria | Strong and full of energy | Backflip |
| **Noa** | Israel | Fearless captain, lights up the pitch | Arms up, cheering the stands |
| **Anna** | Ukraine | Ice cool, slides into every goal | Knee slide |

![The three commentators](Docs/images/commentators.jpg)

<table>
<tr>
<td width="140" align="center"><img src="Docs/images/commentator-cutin.png" alt="The commentator in the crowd above the goal, LIVE tag over his head" width="120"></td>
<td>One of the three commentators is drawn at random for each match. On every goal he pops up in the crowd above the goal that was just scored in, a red LIVE tag over his head, and shouts a call we generated with Gemini while the rest of the sound goes quiet under him (detailed in the credits below).</td>
</tr>
</table>

## Automated tests

`Head-Soccer-main/Assets/Tests/Editor` holds EditMode tests that run in the Unity Test Runner (**Window → General → Test Runner → EditMode → Run All**) without opening a scene. They cover the pure logic and, more usefully, the shipped assets:

| Suite | What it guards |
|---|---|
| `KickMathTests` | The running kick bonus: exactly 1 when standing, capped at 1 + bonus at full speed, never below 1 when running away; every kick angle in the configured range travels forward and never into the grass. |
| `BallEscapeTests` | A ball that sits still inside a goal for two seconds is pushed back onto the pitch. The right goal pushes left and the left goal pushes right, one rule mirrored by the ball's own x. A ball still on the pitch, still moving, or stuck for less than two seconds is left alone. |
| `SpecialShotTests` | The Super fills only from contact, never overfills, fires exactly once per match, cannot recharge after firing, and a rematch resets it. |
| `KeyboardLayoutTests` | Player two always gets the layout player one did not pick, and the choice survives in `PlayerPrefs`. |
| `MatchRecordsTests` | Biggest win and P1 win count: a smaller win keeps the bigger record, a draw records nothing. |
| `CharacterRosterTests` | The select arrows wrap at both ends; a fresh install is never a mirror match. |
| `HumanVictoryTests` | The victory anthem is earned only by beating the CPU, and the defeat theme only by losing to the CPU. A draw and any two-player result stay on the whistle. A loss to the CPU reads YOU LOST under GAME OVER; every other result still names the winner. |
| `ProjectAssetsTests` | The roster asset still has all six characters with all three drawings and sane stats (this exact asset once lost two of them on a re-save), the tuning asset can end a match, both scenes are in the build list, the commentator has three cut-outs and a call cut to six seconds, the advertising board ships as one wide strip, both crowd loops ship and are long enough to loop unnoticed, and the victory anthem and the defeat theme are each about ten seconds. |

Tests that touch `PlayerPrefs` run inside a sandbox that restores the player's real settings afterwards.

## Course concepts used

| Concept | Where |
|---|---|
| **Singleton** | `GameManager` is the only owner of match state, score and clock. `AudioManager`, `EffectsPool`, `UIManager` follow the same pattern. |
| **Prefabs** | `Player`, `Ball`, `Goal`, `KickSpark` and `GoalConfetti` in `Assets/Prefabs`. Both players in the match are instances of the one `Player` prefab, and both goals of the one `Goal` prefab, mirrored by scale for the right-hand side. |
| **Object pool** | `EffectsPool` recycles a fixed set of kick sparks and goal confetti; nothing is instantiated during play. |
| **Coroutines** | 3-2-1 kickoff, goal celebration freeze, Super slow motion, kick hit-stop, score bump and goal flash in the HUD. |
| **Compile to mobile** | Android APK, touch controls, `SafeAreaFitter` for notches, `CameraFitter` keeps the whole pitch visible on any aspect ratio. |
| **ScriptableObjects** | `GameConfig` (all tuning numbers) and `CharacterRoster` (the selectable characters). |
| **Input System** | Keyboard, gamepad and touch behind one `IInputSource` interface, merged per player by `CompositeInputSource`. |
| **Strategy pattern** | `PlayerController` never knows who is driving it. A human (`KeyboardInputSource`, `GamepadInputSource`, `TouchInputSource`) and the computer (`AIController`) all implement the same `IInputSource`, so swapping a player for an AI is one line. |
| **Pub/Sub events** | `GameManager` raises `ScoreChanged`, `GoalScored`, `StateChanged`, `CountdownChanged` and `MatchEnded`. `UIManager` subscribes and updates the HUD, pause and match-over panels from those events, and `CommentatorCutIn` subscribes to `GoalScored` and `StateChanged` for the commentator; the manager never holds a reference to either. |
| **PlayerPrefs** | Both chosen characters, keyboard layout, mute setting, best win and P1 win count. |

## Project layout

```
Head-Soccer-main/Assets
├── Art/        stadium, goal, ball, ad board, Characters/ (six players, idle + kick + celebration), Commentators/
├── Audio/      synthesised SFX, the two crowd loops (menu, match), the commentator's goal call, and the win and loss songs
├── Data/       GameConfig, CharacterRoster, ball physics material
├── Editor/     HeadSoccerBuilder (rebuilds both scenes), HeadSoccerBuildPipeline (one-click builds)
├── Fonts/      Oswald Bold (SIL Open Font License)
├── Prefabs/    Player, Ball, Goal, and the pooled particle effects
├── Resources/  Crowds/ (five country crowd drawings, loaded by CrowdController)
├── Scenes/     Menu.unity, Match.unity
├── Scripts/
│   ├── Core/       GameManager, GameConfig, MatchState, MatchSettings, MatchRecords, CharacterRoster, CameraFitter
│   ├── Gameplay/   PlayerController, PlayerVisual, SpecialShot, KickHitbox, KickMath, BallController, GoalTrigger, AIController
│   ├── Input/      IInputSource, Keyboard/Gamepad/Touch/Composite sources, HoldButton
│   ├── UI/         UIManager, MainMenuController, CharacterSelect, CelebrationLoop, CommentatorCutIn, SafeAreaFitter
│   ├── Audio/      AudioManager
│   └── Effects/    EffectsPool, CameraShake, SuperReadySign, AdBoard, WeatherController, CrowdController
├── Tests/Editor/   EditMode tests (see Automated tests)
└── UI/         RoundedPanel (9-sliced panel sprite)
```

## Credits

- Design, code and art direction: Guy Yad Shalom and Tomer Yad Shalom. AI models assisted the code, the drawings and the voices. We reviewed the result and kept only what belonged in the game.
- Character and commentator drawings: made with ChatGPT for this project.
- Commentator goal call: generated with Gemini for this project, then cut to six seconds and loudness matched. It plays over every goal while the crowd and the effects duck under it, and it is cut the moment the whistle puts the ball back in play. The three commentator drawings share one call; the one who speaks is the one drawn for that match.
- Victory anthem, heard only when the player beats the CPU: generated with Gemini for this project, so the win feels like it mattered. The crowd and the effects go silent under it.
- Defeat theme, heard only when the player loses to the CPU: generated with Gemini for this project, so a loss is its own moment and not the anthem played sadly. The crowd and the effects go silent under it too. A two-player match and a draw keep the final whistle.
- Advertising board: one strip of local businesses that do not exist, Gal's barber, Itay's pizza, Or's car wash, a bakery and a bouncy castle rental. The businesses, their names and the drawing were made with ChatGPT for this project. The same strip shows every match.
- Font: [Oswald](https://fonts.google.com/specimen/Oswald) by Vernon Adams, SIL Open Font License 1.1.
- Sound effects and the goal fanfare were synthesised for this project.
- Match crowd: ["Football supporters in stadium"](https://freesound.org/people/devy32/sounds/606958/) by **devy32**, from [Freesound](https://freesound.org), licensed under [Creative Commons Attribution 4.0](https://creativecommons.org/licenses/by/4.0/). Cut into a seamless loop and loudness matched for the game; it plays under the match.
- Menu crowd: ["stadium crowd sing"](https://freesound.org/people/axelthecocker02/sounds/733614/) by **AxelTheCocker02**, from Freesound, released under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/) (no attribution required, credited anyway). Cut into a seamless loop; it plays under the main menu.
