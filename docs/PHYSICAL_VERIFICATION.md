# Physical verification

## Scripted checks

- Pure kitchen checks: 86 passed.
- Restaurant checks: 125 passed.
- City checks: 18 passed.
- Windows physical runtime: `PHYSICAL_RUNTIME_PASS 252` (process exit 0; `C:\dev\Ant-Dev\physical-runtime.log`).
- Windows resume: `PHYSICAL_RESUME_PASS 2` (process exit 0; `C:\dev\Ant-Dev\physical-resume.log`).
- Both runtime logs contain zero exception and error entries. The runtime log has two expected checks mentioning a failed-shift scenario; they are not failures of the acceptance run.
- Evidence screenshots and save files are in `Builds/Windows/PhysicalEvidence`.

These are scripted acceptance runs against the Windows build. They do not constitute human play testing. No physical gamepad was detected, so controller and fun verification remain outstanding.

## Runtime notes

Unity logged RenderTexture cleanup warnings during screenshot capture and warnings that some URP post-processing shaders were stripped or unsupported. Both processes exited successfully and emitted their expected pass markers.
