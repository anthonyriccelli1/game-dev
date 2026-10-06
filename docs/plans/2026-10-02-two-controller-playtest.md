# Two-controller implementation plan

1. Correct `LocalCoop` device assignment so first Start belongs to Player 1 and second Start joins Player 2; preserve reconnect and keyboard behavior.
2. Make the title/pause overlay operate with Player 1's pad and clearly show pad controls.
3. Route Player 1 pad into restaurant modal and placement handling, maintain UI selection across rebuilds, and give controller access to phone/map and overhead cursors.
4. Build the Windows player from the saved scene. Run isolated scripted acceptance with virtual gamepads where practical, then document exact two-controller playtest steps and hardware-test limit.
