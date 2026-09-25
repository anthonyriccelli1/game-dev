# Project collaboration preferences

User preference (September 25, 2026): minimize expensive model usage.

- The conductor and final approver is Claude, acting as orchestrator. Claude owns scope, task decomposition, agent/model selection, integration decisions, and acceptance against the user's requirements. If the current task runs a different model, say so rather than claiming to change it.
- The orchestrator chooses the appropriate model for each assignment based on difficulty, risk, and cost; there is no fixed model-to-task mapping. Use lower-cost models when capable, and escalate difficult or inadequate work as needed.
- Sub-agent completion and test reports are evidence, not final approval. The orchestrator must independently inspect the relevant changes and verification evidence, check whether the result meets the intended experience and quality, and request corrections where needed. Delegate execution, not final judgment. Do not declare work approved solely because a sub-agent reports success; distinguish verified acceptance from outstanding checks.
- Delegation is explicitly authorized. Use small, independent assignments with clear file ownership. Pass only necessary context (prefer fork_turns none). Avoid duplicate investigations and unnecessary agents.
- Finish a small runnable stage before expanding scope. Batch checks, reuse verified evidence, and do not repeat unchanged tests. Keep status updates concise.
- Preserve user saves; test with isolated saves. Distinguish scripted runtime checks from human play and physical controller verification.
