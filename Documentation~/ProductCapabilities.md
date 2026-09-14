# uGUI Fork product capabilities

Current source reference: 2026-09-14. This document describes the UnityUI checkout and its current working-tree changes. It does not establish a public release version. The ecosystem currently presents this product as Coming Soon.

## Product position

**Keep the Canvas workflow. Build around UniText.**

Hub summary: **Familiar Unity UI, tailored for UniText projects.**

Expanded description: LightSide's fork of Unity UI retains the GameObject, RectTransform, Canvas and component workflow while removing the real bundled TextMeshPro implementation. It adds targeted layout, scrolling, event and graphics changes for projects that use UniText as their text engine.

This is a specialized uGUI distribution, not a new universal renderer or a complete replacement for every upstream contract. Its most direct benefit is preserving a familiar Unity authoring model while choosing the text stack explicitly.

## Current scope and ownership

The package identity remains `com.unity.ugui`, and the main assembly remains `UnityEngine.UI`. It occupies the package slot of the official Unity UI implementation; it is not installed beside another copy of that same package. [package.json](../package.json) currently retains upstream-style display text and TextMeshPro keywords that do not accurately describe this fork.

[Runtime/UGUI](../Runtime/UGUI/) and [Editor/UGUI](../Editor/UGUI/) retain familiar controls and tools: Graphic/MaskableGraphic, Image/RawImage, buttons, toggles, sliders, scrollbars, ScrollRect, layout groups, fitters, CanvasScaler, masking, raycasters, EventSystem and input modules. The traditional legacy Text/InputField code also remains; removal of TextMeshPro is not removal of every non-UniText text class.

This preserves the everyday scene/prefab/component interaction model documented by [Unity's uGUI manual](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/index.html). It does not prove binary, source or behavioral compatibility for all third-party code.

The package declares Unity UI/IMGUI engine-module dependencies, not LightSide Core. Core owns the ecosystem's shared shaders, paint atlases, GPU transport, world batching, interaction routing and editor design system. UniText, UniShapes and UniLottie consume those mechanisms. They must not be marketed as rendering features implemented by this fork. Core's uGUI integration does not make this fork a prerequisite for mixed LightSide batching.

## Verified fork additions and changes

| Area | Current implementation | Useful positioning |
| --- | --- | --- |
| TextMeshPro removal | Real TMP runtime/editor implementation is absent; [compatibility stubs](../Runtime/TextMeshPro-CompatibilityStub/TextMeshProStub.cs) keep a limited set of references resolvable | A uGUI distribution for projects deliberately using UniText |
| Scroll target helper | [ScrollRect.GetGoToPos / GoTo](../Runtime/UGUI/UI/Core/ScrollRect.cs) calculate and apply a content position toward a target RectTransform | Address a scroll target directly instead of duplicating the coordinate conversion at every call site |
| Partial grid alignment | [GridLayoutGroup.SetCellsAlongAxis](../Runtime/UGUI/UI/Core/Layout/GridLayoutGroup.cs) recalculates required space/start offset for an incomplete final row or column | Align partial groups through the existing layout component |
| UI lifecycle events | [CanvasUpdateRegistry.Updated](../Runtime/UGUI/UI/Core/CanvasUpdateRegistry.cs) and [EventSystem.Updated](../Runtime/UGUI/EventSystem/EventSystem.cs) expose completion points | Allow external systems to observe the cycle through a named hook |
| Composed event handlers | [IUIControlElement / ExecuteEvents.GetEventList](../Runtime/UGUI/EventSystem/ExecuteEvents.cs) add an object supplied by a component's UIControl property to event dispatch | Permit an attached adapter to supply a separate handler object |
| RawImage texture changes | [RawImage.texture](../Runtime/UGUI/UI/Core/RawImage.cs) marks material state dirty without also unconditionally marking vertices dirty; the backing texture is protected | Reduce a specific rebuild request and make subclass integration possible |
| Mesh-effect presence cache | [Graphic](../Runtime/UGUI/UI/Core/Graphic.cs) caches whether IMeshModifier components exist and skips their component scan when none are present | Avoid repeated discovery work for ordinary graphics without mesh effects |
| Deferred color-tween setup | Current Graphic working-tree changes create the color tween runner on first use instead of in every Graphic constructor | Avoid an unused allocation for graphics that never use cross-fades |
| Comparison tooling | [UI Stress Benchmark](../Samples~/UIStressBenchmark/README.md) builds repeatable UI scenarios and records timing, allocation and rendering counters | Provide a common measurement harness for comparing package choices |

These are specific implementation changes. They do not establish a broad throughput multiplier, memory reduction percentage or that all UI work is now allocation-free. Several concern integration or UX rather than speed.

## TextMeshPro compatibility is compile-time only

The existing [stub README](../Runtime/TextMeshPro-CompatibilityStub/README.md) is authoritative about intent: the package contains a minimal `Unity.TextMeshPro` assembly so selected third-party references can compile despite the absence of the real TMP engine. It currently identifies Unity Localization integration as a consumer.

The classes in [TextMeshProStub.cs](../Runtime/TextMeshPro-CompatibilityStub/TextMeshProStub.cs) are inert. Assigning text does not render it; font stubs hold no real font content; input/dropdown stubs do not implement those widgets. The stub does not make TMP scenes work and does not automatically convert them to UniText. Existing TMP components, assets and workflows require an explicit migration or continued use of the official package.

Safe public wording: **TextMeshPro-free, with limited compatibility stubs for third-party references.** Unsafe wording: TMP-compatible, complete drop-in replacement, or automatic UniText conversion.

## Compatibility and correctness observations

These differences matter to product claims and package adoption. They are not customer obligations that erase a defect.

1. **ICanvasElement differs from upstream.** The local [CanvasUpdateRegistry](../Runtime/UGUI/UI/Core/CanvasUpdateRegistry.cs) interface contains Rebuild and transform only. The [official source](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/CanvasUpdateRegistry.cs) additionally includes LayoutComplete, GraphicUpdateComplete and IsDestroyed. Consumers compiled against the complete upstream interface cannot be assumed compatible.
2. **Graphic rebuild phases differ.** The local registry's graphic phase loop uses an exclusive upper bound at LatePreRender, so it does not execute that phase. Completion and destroyed-element handling also differ. Do not describe reduced registry work as a semantics-preserving optimization without resolving these contracts.
3. **Event receiver eligibility differs.** The [official ExecuteEvents](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/EventSystem/ExecuteEvents.cs) filters Behaviour handlers by isActiveAndEnabled. The fork's current GetEventList selects interface matches on an active GameObject without that filter. Disabled components can therefore enter its handler list. The extension is real, but compatibility and correctness need separate treatment.
4. **RawImage has geometry-sensitive cases.** Removing a blanket vertices-dirty request is a narrower change than proving every texture swap geometry-independent. The inherited OnPopulateMesh path consults texture presence and texture sizing information; subclasses may also use the texture. Do not promise that arbitrary replacements remain correct without geometry invalidation.
5. **Scroll targeting is not a complete navigation system.** GoTo applies a position immediately; it provides neither tweening nor virtualization. Its bounding adjustments are an else-if chain, so simultaneous two-axis overflow does not receive independent clamps in the same call. Arbitrary rotation/scale correctness is not established by using two transformed corners.
6. **Grid alignment is a behavior change.** Recalculating the last row/column offset affects existing layouts. Corner direction, constraint mode and short final groups are part of the contract; a generic claim that all old layouts remain pixel-identical is false.
7. **Mesh-modifier cache invalidation is a current responsibility.** BaseMeshEffect's current working-tree edits notify SetMeshModifiersDirty on lifecycle/editor changes. The Graphic contract asks custom IMeshModifier implementations to invalidate the cache themselves. A cache that requires an existing legal caller to learn a new hidden requirement is not proof of transparent compatibility.
8. **Declared minimum version is not established compatibility.** package.json still says Unity 2019.2 while the code contains newer interoperability surfaces and version defines. Do not copy that field into a broad support promise without a compatibility inventory.

The observed risks belong with their source owners. They should be resolved before advertising unconditional replacement or performance superiority. This document records current behavior and does not change runtime code.

## Optimization and measurement boundaries

The inherited CanvasRenderer/Canvas pipeline remains. No Burst/Jobs rewrite of Runtime/UGUI is established by the checked source; a dense sample, a pooled list or a simplified queue does not mean the entire UI system is data-oriented. Native Canvas rebatching, layout propagation, dirty meshes and material splits still exist.

The benchmark sample distinguishes static idle, geometry churn, layout changes, transform movement, material changes, object churn and raycasting. It is useful infrastructure, but no measurements were produced for this capability reference. Its existence alone supports neither an FPS claim nor a universal zero-GC claim. In particular, observing no collection during a finite window is not proof that no allocations occurred.

Current uncommitted changes include Graphic's lazy color-tween setup, BaseMeshEffect invalidation, an assembly reference and benchmark additions. They are working-tree capabilities, not proof that a published package includes them. Historical commits and package.json values are not release authority.

## Positioning against official uGUI

Primary sources checked 2026-09-14:

- [Unity uGUI manual](https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/index.html): the familiar component workflow is inherited and should be credited as such.
- [Official uGUI repository](https://github.com/Unity-Technologies/uGUI): the upstream implementation is the comparison baseline, not an imagined alternative with no optimization or tooling.
- [Official RawImage](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/RawImage.cs): texture changes dirty geometry and materials upstream; this fork differs at a concrete setter.
- [Official GridLayoutGroup](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/GridLayoutGroup.cs): the fork adds final-group offset logic to inherited grid placement.
- [Official ExecuteEvents](https://github.com/Unity-Technologies/uGUI/blob/main/com.unity.ugui/Runtime/UGUI/EventSystem/ExecuteEvents.cs): current handler selection and enable-state contracts differ.

The strongest honest distinction is **a familiar Unity UI base tailored for a UniText-centered project**, with targeted extensions. Shared rendering, rich text, vector layers and animation remain the corresponding LightSide products' value. No claim of first-ever, fastest, full API parity or universal compatibility is supported by this inventory.