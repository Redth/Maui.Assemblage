# Native Transform Batching Experiment (Android-first)

## Goal
Evaluate whether batching realized-item transform updates in native Android code reduces .NET↔JNI overhead for `CollectionHostView` during high-frequency scrolling.

---

## What was implemented in the spike

### 1) `CollectionHostView` spike wiring
Spike-only additions were made to:
- `src/Maui.Assemblage/CollectionHostView.cs`

Key pieces:
- `UseNativeTransformBatching` bindable flag (default `false`)
- `EnablePerformanceDiagnostics` bindable flag
- Per-tick native batch buffers for:
  - target views
  - optional wrapped inner views
  - scale / rotationY / translationX / rotationX arrays
- Single JNI static call per update tick:
  - Java class: `com.mauiassemblage.BatchInterop`
  - method: `applyTransformsBatch(...)`

When batching was OFF:
- each realized item updated native properties directly (multiple per-item JNI calls)

When batching was ON:
- transforms were queued while positioning realized views
- one JNI call flushed the full tick batch

### 2) Android native helper
Added spike-only file:
- `src/Maui.Assemblage/AndroidNative/com/mauiassemblage/BatchInterop.java`

Function:
- Loops native-side through packed arrays and applies transforms to each target view
- Resets wrapped inner views to identity when provided

### 3) Android build inclusion
Spike-only csproj update:
- `src/Maui.Assemblage/Maui.Assemblage.csproj`
- added:
  - `<AndroidJavaSource Include="AndroidNative\**\*.java" />` for Android TFM

---

## Measurement method

Instrumentation used:
- `[ASSEMBLAGE-PERF]` debug logs from `CollectionHostView`
- sampled roughly every ~1s while actively scrolling

Primary fields:
- `updates/s`
- `avgUpdateMs`
- `positioned/update`
- `nativeBatches/update`
- `estNativePropSets/s`
- `estInteropCalls/update`
- `estInteropCalls/s`

Test pages:
- CoverFlow
- Wheel
- Social Feed
- Stress Test (50K mode)

---

## Measured results (OFF vs ON)

### Interop crossings (main outcome)
- **CoverFlow**: `~8.3` → `~1.0` interop calls/update (~88% reduction)
- **Wheel**: `~16.3` → `~1.0` (~94% reduction)
- **Social Feed**: `~14.5` → `~1.0` (~93% reduction)
- **Stress (50K)**: `~23.6` → `~1.0` (~96% reduction)

### Update time (`avgUpdateMs`)
- Mixed / not consistently improved in this implementation.
- In several runs, ON was similar; in others slightly worse or slightly better.

Interpretation:
- JNI crossing count dropped sharply (clear win),
- but overall frame/update time did **not** yet show consistent gains, likely due to packing/allocation overhead and unchanged native property-application work.

---

## Important implementation notes from spike

- JNI class/method caching must be handled carefully:
  - using `NewGlobalRef` + `DeleteLocalRef` incorrectly caused a runtime crash (`expected reference of kind Local but found Global`).
  - the working spike used `JNIEnv.FindClass(...)` class ref directly in this app context.
- Wrapper handling remained necessary:
  - transforms target outer wrapper view when present
  - inner platform view reset to identity

---

## Recommendation from experiment

- **Android**: keep as an experimental path only (feature-flagged), not default-on yet.
- **Apple native bridge**: defer for now (higher complexity; no proven need yet).
- If revisiting:
  1. eliminate per-tick allocations (buffer reuse / pooling),
  2. expand native batch scope only if profiling still indicates bridge overhead dominates,
  3. validate with frame-time/jank tools in addition to control-local timing.

---

## How to recreate later

1. Reintroduce the 3 files/changes above:
   - `CollectionHostView.cs` batching + diagnostics paths
   - `AndroidNative/.../BatchInterop.java`
   - Android `AndroidJavaSource` include in `Maui.Assemblage.csproj`
2. Deploy sample app to Android emulator/device.
3. Enable on test host view:
   - `EnablePerformanceDiagnostics = true`
   - toggle `UseNativeTransformBatching` OFF/ON
4. Scroll each target page and collect:
   - `adb logcat -d | grep ASSEMBLAGE-PERF`
5. Compare OFF vs ON for:
   - `estInteropCalls/update`
   - `avgUpdateMs`
   - `updates/s`

---

## Current repository state requested
After writing this document, spike code changes were reverted from the product code so behavior remains unchanged unless this experiment is re-applied later.
