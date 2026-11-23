# Screen Draw

Draw on a window while it keeps running.

Press a hotkey and a transparent ink layer appears over the window you were just using. Annotate what is on screen, then clear it and carry on. Nothing is captured, nothing is intercepted, and the window never loses focus.

It is built for the case where you are sharing your screen in Discord, Teams or OBS and want to point at something: a route in OSRS, a pull in WoW, a line in a terminal.

## Status

Early. `0.1.x` is the overlay, the ink, the tool keys and snapshots. Settings, rebinding and a proper per-window capture come next; see the roadmap.

## Controls

| Input | Action |
| --- | --- |
| `Ctrl + Alt + D` | Arm the overlay over the focused window, or disarm it |
| Left drag | Ink |
| Mouse wheel | Brush size |
| `1`–`5` | Yellow, red, cyan, green, white |
| `E` / `B` | Eraser / pen |
| `[` / `]` | Thinner / wider brush — hold to keep going |
| Middle click | Undo |
| Right click | Clear |
| `Space` | Snapshot: save a PNG and copy it to the clipboard |
| `Esc` | Finish and close the overlay |

The pointer becomes a ring sized to the brush and tinted with the current colour — dashed while the eraser is selected — so the wheel gives immediate feedback. The system cursor is hidden while it is over the overlay.

The overlay never takes focus, so the tool keys are registered as global hotkeys only while it is armed, and released on dismiss. Choosing a tool shows a one-line label of the current colour and width.

The overlay is `WS_EX_NOACTIVATE`, so the window underneath keeps keyboard focus the whole time.

If `Ctrl + Alt + D` is already taken by another application, Screen Draw falls back to `Ctrl + Shift + D` and then `Ctrl + Alt + Q`. The window shows which one it got, and the tray menu and the window both have a "Draw on last window" action that needs no hotkey at all.

## Snapshots

`Space` takes the annotated frame — the window plus your ink, without the overlay's border, label or brush ring — saves it as a PNG in `Pictures\Screen Draw`, and copies it to the clipboard. Press it as often as you like; each press writes a new file.

It is a copy of the screen region rather than of the window, so anything sitting on top of the window at that moment is in the picture. A per-window grab that ignores occluding windows is M2.

## Requirements

- Windows 10 19041 or newer, x64. The overlay is designed for windowed or borderless-windowed applications.
- .NET 10 desktop runtime, or the self-contained build.

## Build

```powershell
dotnet build
dotnet run --project src/ScreenDraw.App
```

Release build:

```powershell
dotnet publish src/ScreenDraw.App -c Release -r win-x64 --self-contained
```

The app lives in the system tray. Closing the settings window hides it; use the tray menu to exit.

### Command line

| Flag | Purpose |
| --- | --- |
| `--self-test` | Exercises the ink model, the layered window, hotkey registration and hotkey delivery, then exits. Writes `%TEMP%\screendraw-selftest.txt` and a rendered preview PNG. |
| `--bench [WxH]` | Measures the render and present cost per frame, defaulting to 2560x1440. Writes `%TEMP%\screendraw-bench.txt`. |
| `--arm` | Starts in the tray and arms the overlay after two seconds. |

## Troubleshooting

Everything meaningful is logged to `%LOCALAPPDATA%\ScreenDraw\screen-draw.log`, including which hotkey was registered, what window each arm targeted, and any interop failure. The window has an **Open log** button.

- **Hotkey does nothing.** Check the log for the hotkey line. If nothing was available, use the tray menu.
- **Overlay flashes and disappears.** Something failed after creation; the log names it.
- **Nothing is drawn but the window keeps focus.** Look for `arm:` lines — if there is no `arm:` line for your keypress, the hotkey was never delivered.

## How it works

- A topmost, tool-windowed, layered window is created over the target window's DWM frame bounds, and painted with a premultiplied 32-bit DIB through `UpdateLayeredWindow`.
- Layered windows are hit-tested against their alpha channel, so fully transparent pixels would be click-through. The frame keeps a minimum alpha of 1 across its whole area: invisible to the eye, clickable to the mouse.
- Ink is a vector list of strokes. Committed strokes are rasterised once into an offscreen buffer and only the in-progress stroke is redrawn each frame.
- Global hotkeys go through `RegisterHotKey` on a message-only window. Everything the overlay needs — `Esc`, the tool keys, `Space` — is registered on arm and released on dismiss, so nothing is stolen outside drawing mode.
- A snapshot re-renders the frame with its chrome suppressed, waits for DWM to compose it, then copies the region with `BitBlt` and `CAPTUREBLT`, which includes layered windows. The ink lands in the picture; the border, label and ring do not.
- There is no hooking, no injection, no screen capture and no process interaction of any kind. That matters for anti-cheat: keep it that way.

## Roadmap

- **M1** Keyboard tool palette (colours, eraser, width), Direct2D + DirectComposition renderer, configurable hotkeys, window tracking while the overlay is armed.
- **M2** `Windows.Graphics.Capture` for per-window grabs that ignore occluding windows, and a configurable save folder.
- **M3** Persisted settings, per-monitor handling, note fade.
- **M4** Freeze-the-frame toggle, spotlight and zoom.

## Limitations

- Exclusive-fullscreen games cannot be overlaid without hooking. Use borderless windowed.
- If you share a single *window* in Discord or OBS, a topmost overlay will not appear in the share. Share the display instead.
- Any always-on-top overlay can upset aggressive anti-cheat software. This tool never touches another process, but you use it at your own risk.

## License

MIT. See [LICENSE](LICENSE).
