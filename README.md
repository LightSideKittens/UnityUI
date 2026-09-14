# LightSide uGUI Fork

**Keep the Canvas workflow. Build around UniText.**

LightSide's fork of Unity UI for projects that use UniText as their text engine. Keep familiar GameObjects, RectTransforms, prefabs and UI components, with targeted changes to scrolling, grid layout, event integration and graphics work.

- **Familiar authoring:** Canvas, controls, layout groups, masks and EventSystem remain the foundation.
- **Direct scroll targeting:** ScrollRect helpers expose a target position and immediate navigation to a RectTransform.
- **More integration points:** observe Canvas/EventSystem updates and expose composed event handlers.
- **Focused graphics changes:** cache mesh-effect presence and avoid work in selected paths; detailed scope and current limitations are documented.

## Text engine and compatibility

This fork occupies the `com.unity.ugui` package slot and removes the real bundled TextMeshPro implementation. Its small compatibility assembly lets selected third-party references compile; **it does not render TMP text or implement TMP widgets**. It does not convert existing TMP content automatically.

The familiar workflow is preserved, but this is not an unconditional drop-in replacement for every upstream API or behavior. See [capabilities and compatibility boundaries](Documentation~/ProductCapabilities.md) and the [TextMeshPro stub contract](Runtime/TextMeshPro-CompatibilityStub/README.md).

## Part of LightSide

UniText provides text. UniShapes provides layered vector graphics. UniLottie provides vector animation. LightSide Core owns their shared paints, shaders, GPU infrastructure and batching; those rendering features are not implemented by this uGUI fork.

Public availability is **Coming Soon**. Internal package metadata is not an installation recommendation.

[Explore the ecosystem](https://github.com/LightSideKittens/LightSideEcosystem) · [Discord](https://discord.gg/ynRHp3wRmb)

Discord is the preferred place for questions, bugs, help and discussion.