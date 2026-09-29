# Design decisions

Guy Yad Shalom and Tomer Yad Shalom.

These are decisions we made while planning Head Soccer and while playing it against each other. Each one started from something that felt wrong in a real match, and the note says what we changed and why. We will keep adding entries as we play.

The numbers live in `GameConfig` (`Head-Soccer-main/Assets/Data/GameConfig.asset`) unless a note says otherwise.

## 30 September 2026

### The player picks the keys

Player one had WASD and Space, player two had the arrows and Right Ctrl, and that was that. Then we watched who sat where. Some people come from PC gaming and have WASD in their hands; others grew up on the arrows and reach for them without thinking. Handing someone the wrong set costs the first match, and the first match is the one that decides whether there is a second.

FIFA solved this years ago. Every player picks a preset, Classic, Alternate, and so on, and nobody is asked to relearn a habit. We took that idea and kept it to one button. On the main menu, P1 KEYS reads WASD by default; press it and player one gets the arrows and player two gets WASD. The reminder line at the bottom of the menu follows the choice, and `PlayerPrefs` remembers it. Our goal was the simplest player: whoever sits down should be comfortable before the whistle, not after.

### A kick shoves the rival back

Two players stand face to face, both kicking, the ball wedged between them. Both kicks fire, both impulses cancel, and the match freezes: no movement, both kick drawings stuck, nothing happening. It looked like a bug and, in effect, it was one.

We wanted a rule that reads like football rather than a patch. A kick that lands on the rival now shoves him a step back. It is small, on purpose, so it never becomes a way to fight instead of play, but it opens a gap and the ball moves again. And it is not random. The shove follows the run the kicker came in with, the same `kickMomentumBonus` that makes a running kick hit the ball harder. A standing kick nudges the rival slightly; a kick at full speed pushes him a little further. `kickPushback` is 4 units per second with a 2 unit lift so the feet leave the ground, fading at 14 per second, which slides the rival a bit over half a body width from a standing kick and about one and a half from a running one.

### Both sides get to choose

Player one picked a character and the right side got the next one in the list. Against the CPU that meant you never chose who you were up against, and in a 2 PLAYERS match your friend never chose at all.

Character Select now runs twice. First PLAYER 1: PICK YOUR PLAYER, then the right side: PLAYER 2: PICK YOUR PLAYER with a friend, or PICK THE CPU'S PLAYER when you play the computer. The second confirm is KICK OFF!. Both choices are saved, so a rematch keeps the same two players. A mirror match is allowed; if two people both want Kim, they both get Kim.

### Women in the roster

The original Head Soccer, as far as we remember it, had no women. The launch roster was men, aliens and monsters. We played it for years and never noticed until we sat down to draw our own players.

Our game is for everyone who plays football, and we do not think this game has a gender. So Noa and Anna join Yossi, David, Kim and Mikel on the same terms: their own idle, kick and celebration drawings, their own celebration motions (Noa cheers with both arms up, Anna slides in on her knees), their own speed, jump and power. We already said, about the advertising boards, that a game carries a message beyond the match. This is the same thought, and we would rather set this one right than repeat it.

One correction along the way: the first drawings of Noa and Anna stood facing the camera, while the four men stand in side view facing the rival. On the pitch that looked like the two of them were looking at us instead of at each other. Their idle drawings were redrawn in profile, facing right like the others, so the two players face each other on the pitch.

### A bigger ball

After many matches we kept feeling the ball was too small. It was easy to lose against the pitch for a moment, and a miss did not feel like anything. The radius went from 0.28 to 0.34 world units, about a fifth bigger. The ball now has weight: a shot that goes wide looks like a shot that went wide, and a header is a header. The goal mouth trigger scales with the radius, so nothing else needed to change.

### New music and a new goal sound

The first loop was 128 bpm and pleasant, and pleasant was the problem. A 90 second match to five goals is not pleasant, it is a sprint, and the music was behind it. The new loop runs at 150 bpm over eight bars: a four on the floor kick, claps on two and four, driving hi-hats, an octave bass and a lead hook that sounds like a chant from the stands, with a riser into the loop point so the return to bar one lands on the beat. The goal sound changed with it, from a short sting to a three note fanfare with a horn under it. Both are still synthesised from scratch, no samples. We wanted the sound to pull the player into the match, not sit beside it.

### Fix: Anna's knee slide left the card

Anna's celebration grew as she slid toward the viewer, and at full size she ran under the arrows on either side of the portrait. She now starts the slide a little smaller and grows back to her normal size, never past it, and the arrows sit further out from the portrait so a wide celebration frame has room.

## 27 September 2026

### The kick angle is random

Playing each other, every kick left the foot at the same angle. Rallies turned flat and you could see the next ball coming. We wanted each kick to be a surprise, so the ball now leaves the foot at a fresh random angle between 0 and 45 degrees: 0 is a flat drive along the ground, 45 is a lob. `KickHitbox` rolls it with `Random.Range` between `kickMinAngle` and `kickMaxAngle`, once per kick. A fixed upward bias used to do this job. It is gone.

### A running kick hits harder than a standing one

This one comes from playing real football, not from the game. A shot taken from a standing foot and a shot taken on the run are not the same thing. The run is part of the power. We wanted the game to carry that, so it feels honest rather than like every kick is identical.

`kickMomentumBonus` adds up to 50% impulse when the player is at full speed toward the goal. Standing still stays at the base `kickImpulse`. Running away from the ball never weakens the kick below that base.

### The advertising boards

There is no football without advertising. The boards and the game go together, and they are part of ordinary life: wherever you go, there is an ad. We put a scrolling board in front of the first row of the crowd for both reasons. It is a statement about that, and it is also what makes the ground look like a real one. A pitch with no boards looks like a practice field.

The brands on it are real ones from our own lives: the shawarma place, the college this project was made for, the food app, the card company, and the game everyone is waiting for.

### Celebrations on the character select screen

Football games today open with a lineup: the eleven who are starting, each with his own celebration, before a ball is kicked. We wanted that feeling for our four, so Yossi, David, Kim and Mikel would be people and not skins.

On the select screen each one stands, then loops his own celebration, then drops back to idle. Yossi kneels and kisses the badge on his shirt. David beats a heart with his hands. Kim bows toward the player, not off to the side. Mikel crouches, backflips and lands on his feet. `CelebrationLoop` does the motion. It plays on that screen only. Once the match starts they go back to the idle and kick drawings.

### How tall the goal should be

We played with the goals at several heights and kept correcting them.

Too tall, and goals arrived in a stream. The ball sailed in over the players and there was no way to reach it, so the match stopped being a contest. Too short, and the players stood taller than the crossbar, and getting the ball in at all became awkward.

The balance we settled on puts the crossbar at one and a half times the height of the players' heads. A header still needs a jump, and a save is still possible. That replaced an earlier pass where the goal was twice the head height, which was the "too tall" version.
