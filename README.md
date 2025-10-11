## Second-person perspective co-op horror

4 player horror co-op game where the camera is a carryable object, and all players share the same perspective.

The basic movement animations of each player looks like this so far:


The player's view will not be in the perspective of the player themselves, but from this shared camera object that a player can carry and move around. In the perspective of the player who is carrying the camera, the game essentially becomes first-person perspective.


To make the game a bit forgiving for players that go off screen, I added an echolocation mechanic. When a player activates this mode, they will be in first-person perspective, but will not be able to see any object other than the camera directly. Instead, they can send off an echo to get a sense of what their environment looks like.


The procedural level generation currently only has 1 generic room pool as for now, but this is what it looks like so far.