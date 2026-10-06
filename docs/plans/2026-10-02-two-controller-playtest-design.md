# Two-controller local playtest

Player 1's first Start press claims the first controller; the second controller's Start press joins Player 2. Keyboard and mouse continue to work for Player 1. The existing split-screen cameras and station actions remain the basis of play.

The title/pause overlay needs focusable controller actions. A starts or resumes; the directional pad or left stick changes the selected action; B backs out of reset confirmation. While playing, Start pauses. Management panels use the existing Unity UI navigation and Player 1's controller, with a selected button whenever the panel is rebuilt. B closes a panel. Player 1 can use View/Select for the phone after the stand exists. In overhead furnishing placement, the left stick or directional pad moves a grid cursor, right shoulder rotates, A places, and B returns to the catalog. Surface finish painting uses the same cursor pattern.

The initial verification target is the startup and game loop in an isolated save: assign both pads, enter the game, walk and interact, open/close shared UI, place a furnishing, pause/resume. Automated tests may use virtual pads; actual controller feel and layout require a human pass with two physical devices. The normal build must preserve user saves and the other developer's current scene/art work.
