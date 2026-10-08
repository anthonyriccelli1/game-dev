# Dusk inspector warning: recipe knowledge gate

## Scope and acceptance

Anthony requested that dusk not introduce Zeeb or his sauce before the player knows the midnight recipe.

- Before `Game.State.Knows("midnight")`: show exactly `Food inspectors are out on patrol.`
- After recipe knowledge: retain the existing dusk warning about carrying Zeeb's sauce.
- Leave inspector spawning, night transitions, notification duration, inspection rules, and saves unchanged.

## Changes

`Assets/Scripts/InspectorPatrol.cs` now branches the dusk notification on `Game.State.Knows("midnight")`, the same recipe-knowledge check used for calling Zeeb. No save schema or progression logic changed.

`Tests/test_inspector_warning.py` checks the actual notification call's recipe-knowledge gate and both exact messages. It is explicitly a source regression check, not a C# behavior or Unity runtime test.

## Verification

- Regression check failed before the fix with `Dusk warning must branch on known midnight recipe`.
- After the fix: `PYTHONDONTWRITEBYTECODE=1 python3 -m unittest discover -s Tests -p 'test_*.py' -v` passed (1 test).
- `git diff --check` passed.
- Diff inspection confirmed the only production change is the notification argument; inspector spawning and the seven-second duration are unchanged.

## Limitations and next steps

No C# compiler or dotnet executable was found on PATH. Unity compilation, Windows builds, and gameplay were not tested. The Windows workflow requires a revision already available on GitHub; no push is authorized for this task, so no build request was submitted for these local edits.

Anthony's playtest: on an isolated new save, reach dusk and confirm the generic patrol message. Learn the midnight recipe, then reach the next dusk and confirm the sauce warning. Integration should run Unity compilation/runtime checks when the revision is available through the approved workflow.
