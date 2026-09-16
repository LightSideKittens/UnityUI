# LightSide uGUI Fork

The Unity Canvas UI foundation for LightSide, without bundled TextMeshPro. Its systems include Canvas rendering, layout, controls, events and masking through the standard uGUI API. UniText, UniShapes and UniLottie add their own content through this foundation. Free to browse and install without an account or access token.

The fork uses the official Unity UI codebase with the bundled TextMeshPro implementation removed. GameObjects, RectTransforms, prefabs and Canvas components retain the standard Unity workflow.

- **Canvas rendering.** Screen-space overlays, camera-space UI and world-space UI.
- **Controls.** Buttons, toggles, sliders, scrollbars, scrolling and dropdowns.
- **Layout.** Layout groups, content fitters, scaling and masking.
- **Events.** Pointer input, selection, navigation and event dispatch.

## Installation and text

The [LightSide Hub](https://github.com/LightSideKittens/LightSideEcosystem#lightside-hub) installs the fork without an account or access token. It occupies the `com.unity.ugui` package slot.

UniText supplies the ecosystem's text engine. Legacy uGUI Text and InputField remain in the package.

The [compatibility assembly](Runtime/TextMeshPro-CompatibilityStub/README.md) resolves selected third-party TMP references at compile time. **It does not render TMP text or implement TMP widgets.** Existing TMP content requires migration.

## Choosing a version

uGUI ships with the editor, and its sources change between editor releases. Each fork release mirrors one such source state, so a release belongs to the editor range it was taken from. The package manifest declares that range through `unity` and `unityRelease`, and the Hub installs the newest release the running editor supports.

| Fork line | uGUI | Unity |
| --- | --- | --- |
| 2.0.x | 2.0.0 | 6000.3 |
| 2.5.x | 2.5.0 | 6000.5 |
| 2.6.x | 2.6.0 | 6000.6 |

Patch numbers belong to this fork; `2.x.0` stays reserved for Unity's own version. Installing a release built for a newer editor produces compilation errors in `Runtime/UGUI`, because uGUI uses engine APIs that arrive with the editor it ships in.

## LightSide integration

UniText and UniShapes share Core's paints. Together with UniLottie, they use common shaders, GPU infrastructure and batching above Unity's Canvas components. Those shared rendering systems also support the official uGUI package.

## Documentation and support

- [Capabilities and integration](Documentation~/ProductCapabilities.md)
- [Unity UI manual](https://docs.unity3d.com/Manual/UISystem.html)
- [Package documentation](Documentation~/TableOfContents.md)
- [License](LICENSE.md)

[Discord](https://discord.gg/ynRHp3wRmb) is the preferred channel for questions, bug reports, help and discussion.
