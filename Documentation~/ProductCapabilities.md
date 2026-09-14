# uGUI Fork capabilities

Unity UI without bundled TextMeshPro. Canvas rendering, layout, controls, events and masks through the standard uGUI API. Free, without an account or access token.

The fork uses the official Unity UI codebase with the bundled TextMeshPro runtime and editor removed. A small compatibility assembly resolves selected TMP references.

## Components

| System | Capabilities | Source |
| --- | --- | --- |
| Canvas | Screen-space overlay, camera-space and world-space UI | [Canvas](class-Canvas.md), [CanvasRenderer](class-CanvasRenderer.md) |
| RectTransform | Anchors, pivots, position, size and hierarchy-based layout | [RectTransform](class-RectTransform.md) |
| Graphics | Image, RawImage, Graphic and MaskableGraphic | [Runtime UI](../Runtime/UGUI/UI/Core/) |
| Controls | Buttons, toggles, sliders, scrollbars, ScrollRect and dropdowns | [Interaction components](UIInteractionComponents.md) |
| Layout | Horizontal, vertical and grid groups, layout elements and content fitters | [Automatic layout](UIAutoLayout.md), [layout source](../Runtime/UGUI/UI/Core/Layout/) |
| Scaling and masking | CanvasScaler, CanvasGroup, Mask and RectMask2D | [Canvas components](comp-CanvasComponents.md), [runtime source](../Runtime/UGUI/UI/Core/) |
| Events | Pointer events, selection, navigation, input modules and raycasters | [EventSystem](EventSystem.md), [event source](../Runtime/UGUI/EventSystem/) |
| Editor tools | Component inspectors, scene handles and prefab authoring | [Editor source](../Editor/UGUI/) |
| Legacy text | uGUI Text and InputField remain available | [Text](../Runtime/UGUI/UI/Core/Text.cs), [InputField](../Runtime/UGUI/UI/Core/InputField.cs) |

## Package and installation

The package name is `com.unity.ugui`; the main runtime assembly is `UnityEngine.UI`. The fork replaces the official package in that slot.

[LightSide Hub](https://github.com/LightSideKittens/LightSideEcosystem#lightside-hub) browses and installs the fork without an account or access token. The public registry owns availability. The Hub installs it under `LocalPackages/com.unity.ugui` and can restore Unity's package.

[Package metadata](../package.json) declares Unity UI, IMGUI, audio and physics engine-module dependencies. The [runtime assembly](../Runtime/UGUI/UnityEngine.UI.asmdef) contains the Unity UI implementation.

## TextMeshPro compatibility

The [compatibility assembly](../Runtime/TextMeshPro-CompatibilityStub/README.md) provides inert types for selected third-party references to `Unity.TextMeshPro`. Unity Localization is a documented consumer.

[TextMeshProStub](../Runtime/TextMeshPro-CompatibilityStub/TextMeshProStub.cs) contains no text rendering, font loading or widget implementation. TMP input fields and dropdowns do not become functional through the stub. Existing TMP components and assets require migration or the official package.

## Ecosystem integration

The fork has no direct dependency on LightSide Core. It supplies the Canvas, control, layout and event components used by the ecosystem's UI products.

UniText and UniShapes share Core's paint system. Together with UniLottie, they use shared materials, shaders, GPU transport and batching. These mechanisms belong to Core and its consumers, rather than to this fork. Mixed LightSide batching also supports the official uGUI package.

World rendering in UniText, UniShapes and UniLottie can use Core's world batcher without a Canvas.

## Rendering

The fork retains Unity's CanvasRenderer pipeline. Canvas rebuilding, layout propagation, geometry updates, clipping and material state determine its rendering work. Product-specific text, shape and animation optimizations belong to the corresponding LightSide packages.

This source uses the official ScrollRect, GridLayoutGroup, graphics and event implementations. The package's difference is the removal of the bundled TextMeshPro implementation and the compatibility assembly.

## References

- [Unity UI manual](https://docs.unity3d.com/Manual/UISystem.html)
- [Package documentation](TableOfContents.md)
- [Official uGUI repository](https://github.com/Unity-Technologies/uGUI)
- [License](../LICENSE.md)
