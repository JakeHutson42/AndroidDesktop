# Android Desktop — Graphite theme design matrix

Design concept, 7 October 2026. This document is the implementation source of truth; generated artwork is a visual reference and may approximate text, spacing, and colors. Implemented across the WPF application. See IMPLEMENTATION.md for verification and deliberate adaptations from the generated artwork.

## Theme tokens

| WPF resource key | Value | Use |
|---|---|---|
| BackgroundBrush | #15181D | Window and canvas surround |
| PanelBrush | #1C2026 | Library, automation, header |
| CardBrush | #232830 | Inputs, secondary controls |
| BorderBrush | #353C46 | 1 DIP dividers and control edges |
| TextPrimaryBrush | #F2F4F7 | Headings and body |
| TextSecondaryBrush | #B5BEC9 | Labels and details |
| TextMutedBrush | #A4AFBC | Nonessential metadata; raised after selected-row contrast validation |
| AccentBrush | #8AB4F8 | Focus, links, selected indicators |
| PrimaryButtonBrush | #A9C7FF | Primary action surface |
| PrimaryButtonTextBrush | #172235 | Text on primary action |
| HoverBrush | #2D3540 | Secondary hover |
| PressedBrush | #394657 | Secondary pressed |
| SelectionBrush | #28384E | Selected APK or navigation item |
| SuccessBrush | #8DCEA8 | Connected/running indicator plus label |
| WarningBrush | #F0C47A | Recoverable warning plus explanation |
| ErrorBrush | #F29B9B | Error message and destructive confirmation |
| ViewportBrush | #101216 | Letterbox area outside Android pixels |

Use opaque solid brushes. No neon outlines, glow, acrylic, or decorative gradients in application chrome. Guest Android content retains its own colors.

## Typography and dimensions

All sizes below are device-independent pixels (DIPs), not physical pixels. Honor Windows DPI and user text scaling.

| Element | Specification | Behavior |
|---|---|---|
| Font | Segoe UI; normal 400, semibold 600 | Use installed Windows font; no external font download |
| Page heading | 20 / 28 line height, semibold | One per page |
| Panel heading | 18 / 26, semibold | Library and Automation |
| Body / button / input | 14 / 20 | Main readable text; never shrink to fit |
| Supporting text | 13 / 18 | Package/version metadata; wrap or truncate with tooltip |
| Status text | 13 / 18 | Concise state plus accessible label |
| Spacing scale | 4, 8, 12, 16, 24, 32 | 16 outer content padding; 8 control gap; 24 section separation |
| Button / input height | Minimum 36 | Allow growth for text scaling; 44 minimum in touch density |
| Button padding | 12 horizontal, 8 vertical | Size to label; equal heights in groups |
| Button / input radius | 4 | Custom WPF ControlTemplate required for Button corners |
| Panel radius | 6 | Modest rounding; square shared dividers |
| Border thickness | 1 | Enable UseLayoutRounding and SnapsToDevicePixels |
| Icon | 18–20, consistent 1.5–2 stroke | Vector assets; 36 minimum hit target; tooltip + AutomationProperties.Name |
| Focus indicator | 2 accent outline with 2 offset | Visible keyboard focus, never color-only selection |
| Navigation height | Minimum 56 | Under standard Windows title bar |
| Footer height | Minimum 32 | Grow for scaled text |

## Layout and component matrix

| Component | Geometry | Contents and behavior |
|---|---|---|
| Window | Default 1440 × 900; retain 850 × 600 minimum | Standard Windows title bar and snap/maximize behavior; restore valid saved bounds within working area |
| Top navigation | Device, Setup, Settings; profile selector on right | Reuse existing pages and profile selection rules; switching remains subject to device stop requirements |
| APK library | 244 wide expanded; minimum 220 | Open APK, drop target, scrollable list, selected metadata and Launch; one selection, explicit selected indicator |
| Emulator area | Flexible remaining width/height | Fit guest aspect ratio with letterboxing; never crop, stretch, or scale surrounding UI with guest |
| Device heading | Auto height | Active profile, Android version/resolution when known, Start or Stop according to state |
| Device toolbar | 60 column including 12 gutter and scrollbar allowance, 36 buttons, 8 gap | Back, Home, Rotate, Volume −/+, Mute, Fullscreen; tooltips and keyboard equivalents from existing bindings |
| Automation panel | 280 wide; vertically scrollable | Record/Open, playback, loops, delay; advanced variations collapsed by default; clear active state |
| Footer | Full width | Display connection and audio state; F11 hint; show failures in actionable message region, not just footer |
| Setup page | Form sections with 24 gaps; content max width 880 | Device profiles, dependency paths, guest resources, backups/recovery; preserve existing safeguards |
| Settings page | Form sections; content max width 880 | Theme, preferences, diagnostics; settings labels and explanatory text remain visible |

Sample apps in the mockup illustrate row styling only. Do not populate the real library with fictional apps, device capabilities, or performance numbers.

## Adaptive layout

Breakpoints use available client width after Windows DPI scaling, not monitor resolution.

| Client width | Layout |
|---|---|
| 1280 and above | Library 244 + flexible viewport + toolbar 60 + Automation 280, 16 gutters |
| 1050–1279 | Library 220; Automation becomes a toggleable overlay drawer; viewport gets recovered width |
| 850–1049 | Library and Automation both become toggleable drawers; show at most one drawer; viewport and toolbar remain central |
| Large / ultrawide | Keep side panels fixed; use extra space for guest viewport; bound forms to 880; do not enlarge all typography |
| Short windows / high text scaling | Scroll library and automation independently; toolbar may scroll; header controls wrap predictably; essential actions remain reachable |

Drawers must overlay or occupy layout space outside the native viewport host. Verify WPF/WebView/native HWND airspace before implementing overlays; if overlap cannot render correctly, use a dedicated Grid column that displaces the viewport. Restore focus to the drawer toggle when closed. Fullscreen shows guest and a discoverable compact exit/control strip.

## Interaction states

| State | Treatment |
|---|---|
| Default secondary | Card surface, border, primary text |
| Hover secondary | HoverBrush; no geometry shift |
| Pressed secondary | PressedBrush |
| Primary hover / pressed | #BBD3FF / #8FB6F5 with dark text |
| Disabled | Surface #20242A, text #77818E; disabled command and explanatory tooltip when useful |
| Selected | Selection surface + 3 DIP accent edge + semibold label; expose selection to accessibility APIs |
| Busy | Label names operation, small progress indicator; preserve Cancel where supported; prevent duplicate submission |
| Error | Inline plain-language explanation with recovery action; do not erase device/app data |
| Destructive | Stop is a secondary action; restore/delete/force termination use existing explicit confirmations with consequences |
| Motion | 100–150 ms color transitions only; respect reduced motion; no animated layout or pulsing borders |

Keyboard navigation follows visual order. Every icon has an accessible name. Verify normal text contrast ≥4.5:1 and interactive boundaries/focus ≥3:1 for actual rendered states before implementation sign-off. Status always combines text with color. Provide selected package/path details through tooltips or expandable details without forcing extremely small text.

## WPF implementation map and acceptance

1. Add semantic tokens in `src/AndroidDesktop/Resources/Themes/Colors.xaml`; migrate existing NeonBrush usages to BorderBrush or AccentBrush according to meaning. Keep existing theme choices working.
2. Build shared Button, ToggleButton, TextBox, ComboBox, TabItem, ListBoxItem and Expander templates in `Resources/Controls/Controls.xaml`; use DynamicResource for theme colors and focus states.
3. Restructure `MainWindow.xaml` into header, adaptive content Grid and status footer. Preserve commands, bindings, drag/drop handlers, viewport host ownership, and device lifecycle logic.
4. Group device actions and APK/automation UI according to the matrix. Advanced controls remain available; the concept does not authorize feature removal.
5. Validate at 850 × 600, 1180 × 800, 1440 × 900 and maximized ultrawide; Windows scaling 100%, 150%, 200%; increased text size; keyboard-only use; running/stopped/busy/error/empty-library states.
6. Check portrait/landscape aspect fit, click coordinate mapping after resizing/rotation, drawer/viewport airspace, audio and fullscreen controls. No clipped labels, overlapping actions, unreachable controls, or silent state changes.

The image was generated with the built-in image generation tool. Its exact prompt is saved beside this matrix in IMAGE_PROMPT.txt.

