Demo 3: Character Control

This demo shows how to use two VirtuaSticks to control a character and camera, replicating a mobile-style dual-stick setup.

- The left stick controls character movement.
- The right stick controls camera rotation.

The demo includes UI buttons to:
- Enable or disable dynamic mode
- Restrict movement axis (Free / Horizontal / Vertical)
- Toggle debug information

Integration steps:
1. Add a VirtuaStick to your UI via UXML or use the VirtuaStickProcedural component.
2. Add the OnScreenVirtuaStick component to the same GameObject.
3. Set the correct binding path to match your InputAction.
4. In your controller script (player or camera), read and apply the corresponding action.

Use this demo as a reference for connecting VirtuaStick to gameplay systems using Unity's Input System.