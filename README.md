# LightSide uGUI Fork

Unity UI without bundled TextMeshPro. Canvas rendering, layout, controls, events and masks through the standard uGUI API. Free, without an account or access token.

The fork uses the official Unity UI codebase with the bundled TextMeshPro implementation removed. GameObjects, RectTransforms, prefabs and Canvas components retain the standard Unity workflow.

- **Canvas rendering.** Screen-space overlays, camera-space UI and world-space UI.
- **Controls.** Buttons, toggles, sliders, scrollbars, scrolling and dropdowns.
- **Layout.** Layout groups, content fitters, scaling and masking.
- **Events.** Pointer input, selection, navigation and event dispatch.

## Installation and text

The [LightSide Hub](https://github.com/LightSideKittens/LightSideEcosystem#lightside-hub) installs the fork without an account or access token. It occupies the `com.unity.ugui` package slot.

UniText supplies the ecosystem's text engine. Legacy uGUI Text and InputField remain in the package.

The [compatibility assembly](Runtime/TextMeshPro-CompatibilityStub/README.md) resolves selected third-party TMP references at compile time. **It does not render TMP text or implement TMP widgets.** Existing TMP content requires migration.

## LightSide integration

UniText and UniShapes share Core's paints. Together with UniLottie, they use common shaders, GPU infrastructure and batching above Unity's Canvas components. Those shared rendering systems also support the official uGUI package.

## Documentation and support

- [Capabilities and integration](Documentation~/ProductCapabilities.md)
- [Unity UI manual](https://docs.unity3d.com/Manual/UISystem.html)
- [Package documentation](Documentation~/TableOfContents.md)
- [License](LICENSE.md)

[Discord](https://discord.gg/ynRHp3wRmb) is the preferred channel for questions, bug reports, help and discussion.
