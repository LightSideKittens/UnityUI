# TextMeshPro Compatibility Stub

This folder ships a **minimal, no-op `Unity.TextMeshPro` assembly** so that third-party packages
which reference TextMeshPro keep compiling against this **TMP-free fork** of `com.unity.ugui`.

## Why this exists

Since uGUI 2.0, TextMeshPro is bundled *inside* `com.unity.ugui`. This fork removes the real
TextMeshPro. The problem: many packages assume **"uGUI present ⇒ TMP present"** and switch on their
TMP code with `#if (UNITY_2023_2_OR_NEWER && PACKAGE_UGUI)`. That condition fires because the
`com.unity.ugui` package is still here — so the TMP code compiles, then fails with
`CS0246: The type or namespace name 'TMPro' could not be found`.

This stub supplies just enough of the `TMPro` API surface, as **inert types**, for those packages to
compile. Nothing here has runtime behaviour: if TMP features are actually exercised they simply do
nothing. That is intentional — the fork's text engine is **UniText**, not TextMeshPro.

## Packages this currently unblocks

- **`com.unity.localization`** — `LocalizedTmpFont`, `TrackedTmpDropdown`, and the
  `CONTEXT/TextMeshProUGUI|TMP_Dropdown/Localize` editor menus.

## How to extend

When another package fails with `... 'SomeType' ... namespace 'TMPro'`, add that type (or member) to
`TextMeshProStub.cs`. Keep additions **inert**: auto-properties and empty method bodies. Mirror the
real TMP type's base class only as far as the compiler requires (e.g. font assets derive from
`ScriptableObject`, components from `MonoBehaviour`). Components carry `[AddComponentMenu("")]` so the
fake types stay out of the Add Component menu.

> Do **not** add real TextMeshPro behaviour here. If a project genuinely needs TextMeshPro, it should
> use the stock `com.unity.ugui` instead of this fork.
