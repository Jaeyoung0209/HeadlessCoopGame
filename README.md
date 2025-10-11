
https://github.com/user-attachments/assets/7fd778f9-2ece-45d2-979c-96cfa4aea83a
## Second-person perspective co-op horror

4 player horror co-op game where the camera is a carryable object, and all players share the same perspective.

The basic movement animations of each player looks like this so far:
https://github.com/user-attachments/assets/39228e3c-5047-4bae-a6ac-9a0d98ac1425


The player's view will not be in the perspective of the player themselves, but from this shared camera object that a player can carry and move around. In the perspective of the player who is carrying the camera, the game essentially becomes first-person perspective.
https://github.com/user-attachments/assets/9dee1948-bc78-4f25-85d3-f710c246ae51

To make the game a bit forgiving for players that go off screen, I added an echolocation mechanic. When a player activates this mode, they will be in first-person perspective, but will not be able to see any object other than the camera directly. Instead, they can send off an echo to get a sense of what their environment looks like.
https://github.com/user-attachments/assets/553e2a81-2071-4a35-9dcf-b6ba4ef4f2cf

The procedural level generation currently only has 1 generic room pool, as for now, but this is what it looks like so far.
https://github.com/user-attachments/assets/4034596c-7628-4cf0-b2c0-b3b408c7b610
