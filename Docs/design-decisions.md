# Design decisions

Guy Yad Shalom and Tomer Yad Shalom.

These are decisions we made while planning Head Soccer and while playing it against each other. Each one started from something that felt wrong in a real match, and the note says what we changed and why. We will keep adding entries as we play.

The numbers live in `GameConfig` (`Head-Soccer-main/Assets/Data/GameConfig.asset`) unless a note says otherwise.

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
