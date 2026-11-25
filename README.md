# Screen Draw

Draw on a window while it keeps running.

Press a hotkey and a transparent ink layer appears over the window you were just using. Annotate what is on screen, then clear it and carry on. Nothing is captured, nothing is intercepted, and the window never loses focus.

This is intended as an annotation-first application. Screensharing on teams/discord/something else and need an easy way to point out something or add extra info? Use this tool to do so seamlessly without losing focus of windows.

## Quick start

Download the latest build from [Releases](https://github.com/Tesquo/screen-draw/releases), extract the zip, run `ScreenDraw.exe`, then press `Ctrl + Alt + D` over any window.

## Status

Early. `0.3.x` contains the overlay, painting, highlighter/eraser tools, toolbar keys and snapshots, packaged as a self contained folder.

## Limitations

- Exclusive-fullscreen games cannot be overlaid without hooking. Use borderless windowed.
- If you share a single *window* in Discord or OBS, a topmost overlay will not appear in the share. The display needs to be shared instead.
- Any always-on-top overlay can upset aggressive anti-cheat software. This tool never touches another process, but you understand that you use this program at your own risk.
  
## Controls

| Input | Action |
| --- | --- |
| `Ctrl + Alt + D` | Arm the overlay over the focused window, or disarm it |
| Left drag | Ink |
| Mouse wheel | Brush size |
| `1`–`6` | Black, Yellow, White, Red, Cyan, Green |
| `E` | Eraser / pen (Toggle)|
| `H` | Highlighter (Toggle)|
| `[` / `]` | Brush size (keyboard shortcut, press and hold) |
| Middle click | Undo |
| Right click | Clear |
| `Space` | Snapshot: save a PNG and copy it to the clipboard |
| `Esc` | Finish and close the overlay |

The pointer becomes a ring sized to the brush and tinted with the current colour. The system cursor is hidden while it is over the overlay.

The overlay never takes focus, so the tool keys are registered as global hotkeys only while it is armed, and released on dismiss. Choosing a tool shows a one-line label of the current colour and width.

The overlay is `WS_EX_NOACTIVATE`, so the window underneath keeps keyboard focus the whole time.

If `Ctrl + Alt + D` is already taken by another application, Screen Draw falls back to `Ctrl + Shift + D` and then `Ctrl + Alt + Q`. The window shows which one it got, and the tray menu and the window both have a "Draw on last window" action that needs no hotkey at all.

## Snapshots

`Space` takes the annotated frame without the overlay's border, label or brush ring — saves it as a PNG in `Pictures\Screen Draw`, and copies it to the clipboard. 
- This folder can be changed to whatever you want.

This is designed in a way so that you can step through stages of annotation, without losing the canvas.

It is a copy of the screen region rather than of the window, so anything sitting on top of the window at that moment is in the picture.

## Requirements

- Windows 10 19041 or newer, x64. The overlay is designed for windowed or borderless-windowed applications.
- .NET 10 SDK (if compiling), or the self-contained build.

## Build

```powershell
dotnet build
dotnet run --project src/ScreenDraw.App
```

To produce a release:
```
.\scripts\release.ps1
```

### Command line

| Flag | Purpose |
| --- | --- |
| `--self-test` | Exercises the ink model, the layered window, hotkey registration and hotkey delivery, then exits. Writes `%TEMP%\screendraw-selftest.txt` and a rendered preview PNG. |
| `--bench [WxH]` | Measures the render and present cost per frame, defaulting to 2560x1440. Writes `%TEMP%\screendraw-bench.txt`. |
| `--arm` | Starts in the tray and arms the overlay after two seconds. |

## Troubleshooting

Everything meaningful is logged to `%LOCALAPPDATA%\ScreenDraw\screen-draw.log`, including which hotkey was registered, what window each arm targeted, and any interop failure. The window has an **Open log** button.

- **Hotkey does nothing.** Check the log for the hotkey line. If nothing was available, use the tray menu.
- **Overlay flashes and disappears.** Something failed after creation, logs name it.
- **Nothing is drawn but the window keeps focus.** Look for `arm:` lines — if there is no `arm:` line for your keypress, the hotkey was never delivered.
- **I have a problem that's not listed or it's not working for me at all.** Open an issue [here](https://github.com/Tesquo/screen-draw/issues) describing your problem.

## How it works

- A topmost, tool-windowed, layered window is created over the target window's DWM frame bounds, and painted with a premultiplied 32-bit DIB through `UpdateLayeredWindow`.
- Layered windows are hit-tested against their alpha channel, so fully transparent pixels would be click-through. The frame keeps a minimum alpha of 1 across its whole area: invisible to the eye, clickable to the mouse.
- Ink is a vector list of strokes. Committed strokes are rasterised once into an offscreen buffer and only the in-progress stroke is redrawn each frame.
- Global hotkeys go through `RegisterHotKey` on a message-only window. Everything the overlay needs — `Esc`, the tool keys, `Space` — is registered on arm and released on dismiss, so nothing is stolen outside drawing mode.
- A snapshot re-renders the frame with its chrome suppressed, waits for DWM to compose it, then copies the region with `BitBlt` and `CAPTUREBLT`, which includes layered windows. The ink lands in the picture; the border, label and ring do not.
- There is no hooking, no injection and no process interaction of any kind. This is due to not wanting to interfere with anti-cheats or other programs that might have issues with an overlay like this.

## Antivirus and trust

Screen Draw is unsigned, and it does three things behavioural scanners are built to dislike: it registers global hotkeys, reads key state while a width key is held, and copies the screen. Any detections are false positives based on these heuristics.

This is open source, so you are welcome to check these statements:

- No network code exists in the project. Nothing is uploaded, phoned home or auto-updated.
- No installer, service, driver or admin rights. The manifest requests `asInvoker`.
- No injection and no hooks. The overlay is an ordinary layered window built from documented Win32 calls.
- `ScreenDraw.exe --self-test` exercises the ink, the overlay, the hotkeys and the snapshot path, and writes only to `%TEMP%`.
- Build it yourself and compare the hash with the release.

## License

MIT. See [LICENSE](LICENSE).
