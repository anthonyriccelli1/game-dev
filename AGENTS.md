# Project collaboration preferences

User preference (September 25, 2026): minimize expensive model usage.

- Use GPT-6 Astra as the conductor for scope, delegation, integration decisions, and final review. If the current task runs a different model, say so rather than claiming to change it.
- Delegate substantial coding and debugging to GPT-6 Sol; use GPT-6 Luna for bounded routine edits, tests, documentation, and straightforward checks. Reserve Astra implementation work for genuinely difficult problems or failed lower-cost attempts.
- Delegation is explicitly authorized. Use small, independent assignments with clear file ownership. Pass only necessary context (prefer fork_turns none). Avoid duplicate investigations and unnecessary agents.
- Finish a small runnable stage before expanding scope. Batch checks, reuse verified evidence, and do not repeat unchanged tests. Keep status updates concise.
- Preserve user saves; test with isolated saves. Distinguish scripted runtime checks from human play and physical controller verification.
