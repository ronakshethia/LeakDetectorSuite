# LeakDetectorSuite

> **Runtime diagnostics toolkit for .NET and MAUI** — detect memory leaks, profile performance, and enforce thread-safety checks with lightweight core libraries and a separate MAUI integration package.

[![NuGet](https://img.shields.io/nuget/v/LeakDetectorSuite.Maui.svg)](https://www.nuget.org/packages/LeakDetectorSuite.Maui)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/dotnet-8_%7C_9_%7C_10-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![net10.0](https://img.shields.io/badge/net10.0-supported-brightgreen)](https://dotnet.microsoft.com/download/dotnet/10.0)

---

## 📦 Packages

| Package | Description | Target |
|---|---|---|
| `LeakDetectorSuite.Memory` | Core memory leak engine | `net8.0` `net9.0` `net10.0` |
| `LeakDetectorSuite.Maui` | MAUI integration & auto page tracking | `net10.0-android` `net10.0-ios` `net10.0-maccatalyst` `net10.0-windows10.0.19041.0` |
| `LeakDetectorSuite.Performance` | Execution time profiler with warnings | `net8.0` `net9.0` `net10.0` |
| `LeakDetectorSuite.Threading` | UI thread guard & blocking-call detector | `net8.0` `net9.0` `net10.0` |

---

## 1. Overview

**LeakDetectorSuite** is a lightweight, production-ready diagnostics library suite designed to surface common runtime problems in .NET applications — especially MAUI mobile apps — during development and testing:

- 🧠 **Memory Leaks**: Track objects with weak references. Compare snapshots before/after navigation to find objects that should have been collected.
- ⏱ **Performance**: Measure execution time of any sync or async operation. Get automatic warnings when operations exceed your threshold.
- 🧵 **Threading**: Guard UI operations, detect background-thread UI access, and warn about blocking async calls.

`LeakDetectorSuite.Memory`, `LeakDetectorSuite.Performance`, and `LeakDetectorSuite.Threading` ship without NuGet dependencies and target `net8.0`, `net9.0`, and `net10.0`. `LeakDetectorSuite.Maui` targets MAUI on .NET 10 and depends on `Microsoft.Maui.Controls` plus `LeakDetectorSuite.Memory`.

---

## 2. Installation

```bash
# Full MAUI integration (includes Memory engine)
dotnet add package LeakDetectorSuite.Maui

# Core memory engine only (non-MAUI .NET apps)
dotnet add package LeakDetectorSuite.Memory

# Performance profiler
dotnet add package LeakDetectorSuite.Performance

# Thread safety guards
dotnet add package LeakDetectorSuite.Threading
```

The published package IDs use the `LeakDetectorSuite.*` prefix. The code namespaces stay as `LeakDetector.*`, so your `using` statements do not change.

---

## 3. Quick Start — MAUI

In `MauiProgram.cs`:

```csharp
using LeakDetector.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseLeakDetector();   // ← add this line

        return builder.Build();
    }
}
```

Once registered, **LeakDetector automatically tracks every page and its `BindingContext`** when it appears, and logs a live-object summary every 5 seconds to the debug output.

---

## 4. Usage Examples

### Memory Leak Detection

```csharp
using LeakDetector.Memory;

// Take a snapshot before an action
var before = LeakTracker.Snapshot();

// … navigate, create objects, etc. …

// Take a snapshot after
var after = LeakTracker.Snapshot();

// Compare and print diff
var diff = LeakTracker.Compare(before, after);
Debug.WriteLine(diff);
// Output:
// [LeakDetector] Snapshot diff (Δ=+2):
//   [HomePage] Δ=+1  (after=1)
//   [HomePageViewModel] Δ=+1  (after=1)
```

**Manual object tracking:**

```csharp
// Track a specific object with an optional tag
LeakTracker.Track(myService, "MyService");

// Force a full GC, then inspect what's still alive
LeakTracker.ForceGC();
LeakTracker.LogAlive();

// Get a programmatic list of alive objects
var alive = LeakTracker.GetAliveObjects();
foreach (var (tag, obj) in alive)
    Debug.WriteLine($"{tag}: {obj}");
```

**Custom logger:**

```csharp
// Route messages to your preferred logger
LeakTracker.Logger = msg => MyLogger.Log(msg);
```

---

### Performance Profiling

```csharp
using LeakDetector.Performance;

// Sync measurement
Perf.Measure(() => SomeMethod(), "SomeMethod");
// Output: [Perf] SomeMethod: 42.17 ms

// Auto-warn when over threshold (default 100 ms)
Perf.Measure(() => Thread.Sleep(200), "SlowOp");
// Output: ⚠️  [Perf WARNING] SlowOp: 201.44 ms (threshold=100 ms)

// Return a value
var result = Perf.Measure<string>(() => ComputeName(), "ComputeName");

// Async variants
await Perf.MeasureAsync(async () => await FetchDataAsync(), "FetchData");
var data = await Perf.MeasureAsync<List<Item>>(async () => await LoadAsync(), "Load");

// Adjust warning threshold
Perf.WarnThresholdMs = 50;
```

---

### Thread Safety

```csharp
using LeakDetector.Threading;

// Assert we are on the UI thread (logs warning if we're not)
ThreadGuard.EnsureMainThread();

// Check for possible blocking calls in async-aware contexts
ThreadGuard.WarnIfBlockingCall();

// Throw instead of just log
ThreadGuard.ThrowOnViolation = true;
ThreadGuard.EnsureMainThread(); // throws InvalidOperationException on background threads

// Query directly
bool onMain = ThreadGuard.IsMainThread();
```

For non-MAUI targets, `ThreadGuard` does not guess which thread is the UI thread. Configure `ThreadGuard.MainThreadDetector` first if you want reliable UI-thread assertions in WPF, WinForms, or another desktop framework.

```csharp
ThreadGuard.MainThreadDetector = () => MyUiDispatcher.CheckAccess();
```

---

## 5. MAUI Automatic Tracking Behavior

When `UseLeakDetector()` is called:

1. **Page lifecycle hook**: `Application.PageAppearing` is subscribed. Every page that appears is automatically passed to `LeakTracker.Track(page, page.GetType().Name)`.

2. **ViewModel tracking**: If the page has a non-null `BindingContext`, it is also tracked automatically under its type name.

3. **Periodic logging**: Every **5 seconds** `LeakTracker.LogAlive()` fires and prints a live-object count to the debug output.

4. **Platform lifecycle**: Platform-specific lifecycle hooks (Android `OnCreate`, iOS `FinishedLaunching`, Windows `OnLaunched`) log a confirmation message.

---

## 6. Reading the Output

LeakDetector writes all messages to the debug output (Android logcat, Xcode console, Visual Studio Output window). This section explains every message type, what state it indicates, and what action to take.

---

### 6.1 Startup Messages

```
[LeakDetector] Registered. Tracking will begin once app launches.
```
**What it means:** `UseLeakDetector()` was called successfully inside `MauiProgram.CreateMauiApp()`.  
**Action needed:** None. This is a confirmation that the library is wired up correctly.

---

```
[LeakDetector] Android lifecycle hooked.
[LeakDetector] iOS/Mac lifecycle hooked.
[LeakDetector] Windows lifecycle hooked.
```
**What it means:** The platform-specific app lifecycle event fired (`OnCreate` / `FinishedLaunching` / `OnLaunched`) and LeakDetector has activated its host. Page tracking is now live.  
**Action needed:** None. If you do NOT see this line, `UseLeakDetector()` was not called or the MAUI workload is not installed.

---

### 6.2 Tracking Messages

```
[LeakDetector] Tracking: HomePage
[LeakDetector] Tracking: HomePageViewModel
```
**What it means:** The page `HomePage` appeared on screen. LeakDetector registered both the page and its `BindingContext` (the ViewModel) as weakly-tracked objects. They are now included in all future snapshots.  
**Action needed:** None. Every tracked entry is expected. If a type you expect to see is **missing**, the page's `BindingContext` was `null` when it appeared — wire up the ViewModel in the constructor before `InitializeComponent()`.

---

### 6.3 Periodic Live-Object Report (every 5 seconds)

```
[LeakDetector] ── Live Objects ──────────────────────
[LeakDetector]   HomePage: 1
[LeakDetector]   HomePageViewModel: 1
[LeakDetector] ──────────────────────────────────────
```

**What it means:** 5 seconds have passed. The GC has not collected `HomePage` or `HomePageViewModel` — they are still in memory. The number is the count of live instances for that type.

| Count | What it signals |
|---|---|
| `1` while page is visible | ✅ Normal — the page is currently on screen |
| `1` **after** navigating away + GC | ❌ **Leak** — something is still holding a reference |
| `2` for a page you only opened once | ❌ **Leak** — previous instance was never collected |
| `0` for any type | ✅ Collected — the GC reclaimed the object as expected |

**Tip:** Call `LeakTracker.ForceGC()` before reading this report in development. Otherwise, the GC may not have run yet and all objects will still show `1` even if they are unreachable.

---

### 6.4 Snapshot Diff — the most important output

Produced by `LeakTracker.Compare(before, after)`:

#### ✅ Clean navigation (no leaks)
```
[LeakDetector] No changes detected.
```
**What it means:** Exactly the same types and counts are alive in both snapshots. Every object created during navigation was properly collected.  
**Action needed:** None.

---

#### ⚠️ Objects grew — possible leak
```
[LeakDetector] Snapshot diff (Δ=+2):
  [HomePage] Δ=+1  (after=1)
  [HomePageViewModel] Δ=+1  (after=1)
```
**What it means, field by field:**

| Field | Meaning |
|---|---|
| `Δ=+2` (header) | Total net increase across all tracked types in this snapshot window |
| `[HomePage]` | The type name of the leaked object |
| `Δ=+1` | One more instance of `HomePage` is alive now than before |
| `(after=1)` | There is currently **1** live `HomePage` instance |

**What caused this:** Something is holding a strong reference to `HomePage` and its ViewModel. The most common causes:

1. **Static event subscription** — the page subscribed to a `static event` and never unsubscribed. The static event's delegate list holds a reference to the page forever.
   ```csharp
   // ❌ Common leak pattern
   SomeService.StaticEvent += OnSomething; // keeps 'this' alive
   
   // ✅ Fix: unsubscribe when the page is done
   protected override void OnDisappearing()
   {
       base.OnDisappearing();
       SomeService.StaticEvent -= OnSomething;
   }
   ```

2. **Long-lived service holds a page reference** — a singleton injected into the page stores a callback or reference.
   ```csharp
   // ❌ Leak: singleton captures 'this'
   _myService.OnUpdate = () => UpdateUI(); // 'this' is captured in closure
   
   // ✅ Fix: clear the callback on disappearing
   protected override void OnDisappearing()
   {
       _myService.OnUpdate = null;
   }
   ```

3. **BindingContext not released** — the ViewModel is subscribed to a service event and was never told to clean up.
   ```csharp
   // ✅ Fix: implement IDisposable on ViewModel and unsubscribe there
   public void Dispose() => SomeService.Changed -= OnChanged;
   ```

---

#### ✅ Objects released — healthy
```
[LeakDetector] Snapshot diff (Δ=-2):
  [SecondPage] Δ=-1  (after=0)
  [SecondPageViewModel] Δ=-1  (after=0)
```
**What it means:** Objects that existed in the `before` snapshot were collected. Count went **down**. This is the expected outcome after navigating away from a page.  
**Action needed:** None. Negative deltas are healthy.

---

#### ⚠️ Count growing across multiple navigations
```
[LeakDetector] Snapshot diff (Δ=+4):
  [ProductPage] Δ=+4  (after=4)
  [ProductViewModel] Δ=+4  (after=4)
```
**What it means:** You navigated to `ProductPage` four times and all four instances are still in memory. The count growing proportionally with navigation count is a strong indicator of a **hard reference held by a shared/singleton resource** (e.g. a message bus, an event aggregator, or a static collection).  
**Fix pattern:**
```csharp
// Check for subscriptions to any shared event bus or messenger
// ❌ Leak: each navigation creates a new subscriber, old ones never removed
MessagingCenter.Subscribe<App, string>(this, "Refresh", OnRefresh);

// ✅ Fix: unsubscribe in OnDisappearing or in page Dispose
protected override void OnDisappearing()
{
    MessagingCenter.Unsubscribe<App, string>(this, "Refresh");
}
```

---

### 6.5 GC and Performance Messages

```
[LeakDetector] GC forced.
```
**What it means:** `LeakTracker.ForceGC()` was called. The runtime ran a full blocking garbage collection across all generations and waited for all pending finalizers. Any object with no remaining strong references is now collected.  
**When to call it:** Always call this **before** taking an `after` snapshot or reading the live-object report. Without this, the GC may not have run yet and previously unreachable objects can still appear as alive, producing false positives.

---

```
[LeakDetector] Tracker reset.
```
**What it means:** `LeakTracker.Reset()` was called. All tracked entries and the ID counter are cleared.  
**When to use:** Between isolated test runs to ensure a clean baseline. Do not use in production flows.

---

```
[LeakDetector] No live tracked objects.
```
**What it means:** The periodic 5-second timer fired but `GetAliveObjects()` returned zero results. All tracked objects have been collected.  
**Action needed:** None — this is the ideal state after navigating away from all pages.

---

### 6.6 Performance Output

```
[Perf] LoadData: 34.82 ms
```
**What it means:** The measured operation completed in **34.82 ms**, which is under the warning threshold (default 100 ms). No action needed.

---

```
⚠️  [Perf WARNING] FetchProducts: 312.50 ms (threshold=100 ms)
```
**What it means:** `FetchProducts` took **312 ms** — more than 3× the threshold. This is slow enough to cause noticeable UI lag on mobile devices (anything over ~16 ms per frame is perceptible).

**Common causes and fixes:**

| Root cause | Fix |
|---|---|
| Synchronous network call on UI thread | Move to `async`/`await` with `HttpClient` |
| Blocking database read | Use async EF Core queries or SQLite-net async API |
| Heavy JSON deserialization | Cache the result, or deserialize on a background thread |
| Image loading inline | Use `CommunityToolkit.Maui` async image loading |

**Adjust the threshold** for your platform:
```csharp
Perf.WarnThresholdMs = 50;  // stricter — for performance-sensitive paths
Perf.WarnThresholdMs = 500; // looser — for cold-start one-time operations
```

---

### 6.7 Thread Safety Output

```
[ThreadGuard] ⚠️  NOT on main thread! Called from 'LoadData' in ProductViewModel.cs:47 (Thread #6)
```
**What it means:** Code that must run on the UI thread (e.g. updating an `ObservableCollection`, changing a label) was called from **Thread #6**, which is a background/thread pool thread. This can cause UI crashes or unpredictable rendering bugs.

**Fix:**
```csharp
// ❌ Wrong — updating UI from a background task
await Task.Run(() => Items.Clear()); // crashes on MAUI

// ✅ Fix — dispatch UI updates back to the main thread
await Task.Run(() =>
{
    var results = FetchFromDatabase();
    MainThread.BeginInvokeOnMainThread(() => Items.ReplaceRange(results));
});
```

---

```
[ThreadGuard] ⚠️  Possible blocking call detected! A SynchronizationContext is active but code is running on Thread #5. Avoid .Wait() / .Result in 'GetUser' (UserService.cs:83).
```
**What it means:** You called `.Wait()` or `.Result` on a `Task` while a `SynchronizationContext` is active. This is a classic **async deadlock** setup — the continuation needs the main thread to resume, but the main thread is blocked waiting for the result.

**Fix:**
```csharp
// ❌ Deadlock risk
var user = GetUserAsync().Result;

// ✅ Fix — always await async methods
var user = await GetUserAsync();
```

---

## 7. Best Practices

- **Use in `#if DEBUG` builds only.** The tracker adds minor overhead (GC pressure from WeakReferences).
- **Snapshot before and after navigation** to isolate which types are leaking.
- **Call `LeakTracker.ForceGC()` before diffing** to ensure the GC has had a chance to collect unreachable objects.
- **Unsubscribe static events** when pages are navigated away — these are the most common MAUI leak source.
- **Keep `Perf.WarnThresholdMs` appropriate** for your platform (mobile is slower than desktop).
- **Don't leave `ThreadGuard.ThrowOnViolation = true`** in production; use it only in automated tests.
- **Configure `ThreadGuard.MainThreadDetector` on non-MAUI UI stacks** before relying on `EnsureMainThread()` or `WarnIfBlockingCall()`.

---

## 8. Architecture

```
LeakDetectorSuite/
├── src/
│   ├── LeakDetectorSuite.Memory/          # Core engine (net8/9/10)
│   │   ├── LeakTracker.cs                 # WeakReference tracking, snapshots
│   │   ├── LeakSnapshot.cs                # Immutable snapshot value object
│   │   └── SnapshotDiff.cs                # Diff between two snapshots
│   ├── LeakDetectorSuite.Maui/            # MAUI integration (net10.0-*)
│   │   ├── LeakDetectorHost.cs            # Lifecycle hooks + 5-second timer
│   │   └── LeakDetectorMauiExtensions.cs  # UseLeakDetector() extension
│   ├── LeakDetectorSuite.Performance/     # Profiler (net8/9/10)
│   │   └── Perf.cs                        # Measure(), MeasureAsync()
│   └── LeakDetectorSuite.Threading/       # Thread guard (net8/9/10)
│       └── ThreadGuard.cs                 # EnsureMainThread(), WarnIfBlockingCall()
└── samples/
    └── LeakDetectorSuite.Demo/            # Full MAUI interactive demo app
        ├── Services/
        │   └── DiagnosticsService.cs      # In-app log capture & ObservableCollection
        ├── Pages/
        │   ├── HomePage.xaml(.cs)         # Scenario card hub + global tools
        │   ├── DiagnosticsPage.xaml(.cs)  # Live color-coded log viewer
        │   ├── Scenario1CleanNavPage      # Proper OnDisappearing cleanup → no leak
        │   ├── Scenario2LeakyPage         # Static event leak (no cleanup)
        │   ├── Scenario3MultiNavPage      # Growing leak via repeated navigation
        │   ├── Scenario4PerfPage          # Fast/slow ops + threshold tuning
        │   ├── Scenario5ThreadingPage     # Wrong thread + blocking call detection
        │   └── Scenario6SnapshotPage      # Step-by-step manual snapshot cycle
        └── ViewModels/                    # Tracked automatically by LeakDetectorHost
```

---

## 9. Roadmap

- [ ] **Debug overlay** — floating in-app panel showing live object counts
- [ ] **Roslyn analyzer** — warn at compile time about common leak patterns (static events, closures)
- [ ] **CI integration** — MSTest/XUnit assertions via `LeakTracker.AssertNoLeaks(before, after)`
- [ ] **Blazor Hybrid support** — track components and DI-scoped services
- [ ] **Export to JSON** — snapshot diffs exportable for CI artifact storage

---

## 10. Demo App — Full Walkthrough

The `LeakDetectorSuite.Demo` MAUI app is a self-contained interactive playground that demonstrates every README output scenario on-screen. You do **not** need Android Studio, Xcode, or a log viewer — all output is captured and displayed live inside the app itself.

---

### 10.1 How Output Gets Into the App

In `MauiProgram.cs`, all three library loggers are wired to a single `DiagnosticsService` before the app is built:

```csharp
var diagnostics = DiagnosticsService.Instance;

builder.UseLeakDetector(diagnostics.Log);  // LeakTracker messages
Perf.Logger        = diagnostics.Log;       // [Perf] messages
ThreadGuard.Logger = diagnostics.Log;       // [ThreadGuard] messages
```

`DiagnosticsService` stores entries in an `ObservableCollection<DiagnosticEntry>` (newest first) that the **Diagnostics Log page** binds to directly. Every message that would normally appear in logcat or the VS Output window now appears **on-screen**, color-coded:

| Color | Meaning |
|---|---|
| 🔵 Soft blue-gray | Normal info — tracking started, GC ran, snapshot taken |
| 🟠 Orange | Warning — slow operation, possible blocking call |
| 🔴 Red | Problem — NOT on main thread, unexpected error |

---

### 10.2 Home Page — The Scenario Hub

The home page is a **card-based navigator**. Each card has a colored left-accent stripe matching the scenario's severity:

| Stripe color | Scenario | Meaning |
|---|---|---|
| 🟢 Green | S1 — Clean Navigation | Expected correct pattern |
| 🔴 Red | S2 — Static Event Leak | Intentional bad pattern |
| 🟠 Orange | S3 — Growing Leak | Bad pattern that accumulates |
| 🔵 Blue | S4 — Performance | Measurement and thresholds |
| 🟣 Purple | S5 — Thread Safety | Threading violation detection |
| 🩵 Teal | S6 — Snapshot Comparison | Full manual snapshot API |

**Global Tools strip** at the top of the Home page:

| Button | What it does |
|---|---|
| `🗑 Force GC` | Calls `LeakTracker.ForceGC()` — use after navigating back to verify collection |
| `📸 Snapshot` | Takes a `before` baseline snapshot — tap Force GC then this again to see the diff |
| `🔄 Reset` | Clears all tracking state and the Scenario 3 instance counter |

---

### 10.3 Scenario 1 — Clean Navigation ✅

**What it proves:** Proper `OnDisappearing` cleanup prevents memory leaks entirely.

**The code pattern:**
```csharp
// constructor:
CleanStaticEvent += OnCleanEvent;   // subscribe

// OnDisappearing:
CleanStaticEvent -= OnCleanEvent;   // ✅ unsubscribe before GC
```

**How to run:**
1. Tap **Scenario 1** card on the Home page.
2. Tap **📸 Take Snapshot Here** — records a `before` baseline.
3. Tap **← Go Back (with cleanup)** — `OnDisappearing` fires, event is unsubscribed, and the diff runs automatically.
4. Open the **Diagnostics Log** — you will see:

```
[S1] ✅ OnDisappearing: unsubscribed from static event.
[LeakDetector] GC forced.
[LeakDetector] No changes detected. ✅
[S1] ✅ No leak — page was properly collected.
```

---

### 10.4 Scenario 2 — Static Event Leak ❌

**What it proves:** A single missing `-=` on a static event is enough to keep an entire page + ViewModel in memory indefinitely.

**The code pattern:**
```csharp
// constructor:
LeakyStaticEvent += OnLeakyEvent;   // subscribe
// ❌ No OnDisappearing — event never unsubscribed
```

**How to run:**
1. Tap **Scenario 2** card.
2. Tap **📸 Take Before Snapshot**.
3. Tap **← Go Back (NO cleanup)**.
4. On the Home page tap **🗑 Force GC**.
5. Open the **Diagnostics Log** — you will see:

```
[S2] Page constructed — subscribed to static event (no cleanup planned).
[S2] Navigating back WITHOUT cleanup (leak incoming)...
[LeakDetector] GC forced.
[LeakDetector] Snapshot diff (Δ=+1):
  [Scenario2LeakyPage] Δ=+1  (after=1)
[S2] ❌ LEAK DETECTED — page still alive after GC!
```

**What to fix:** Add `LeakyStaticEvent -= OnLeakyEvent;` inside `OnDisappearing()`.

---

### 10.5 Scenario 3 — Growing Leak (Repeated Navigation) ⚠️

**What it proves:** A leak that appears once will **accumulate** with every navigation. After 3 visits, 3 instances are alive simultaneously.

**How to run:**
1. On the Home page tap **📸 Snapshot** to take a global before.
2. Tap **Scenario 3** → see **Instance #1** on screen → tap **← Go Back**.
3. Tap **Scenario 3** again → see **Instance #2** → tap **← Go Back**.
4. Tap **Scenario 3** again → see **Instance #3** → tap **← Go Back**.
5. Tap **🗑 Force GC** → the diff is computed automatically:

```
[Home] Auto-snapshot before S3 navigation.
[S3] Instance #1 created and subscribed to static event (no cleanup).
[S3] Instance #1 navigating back — reference still held by event.
[S3] Instance #2 created ...
[S3] Instance #3 created ...
[LeakDetector] GC forced.
[LeakDetector] Snapshot diff (Δ=+3):
  [Scenario3MultiNavPage] Δ=+3  (after=3)
```

The instance counter (`#1`, `#2`, `#3`) visible on each page visit makes it obvious that **new objects are being created** while the old ones are never released.

---

### 10.6 Scenario 4 — Performance Profiling ⏱

**What it proves:** `Perf.MeasureAsync` gives precise timing and auto-warns when you exceed the threshold — no manual stopwatch needed.

**How to run — demo sequence:**

| Button | Operation duration | Expected output |
|---|---|---|
| ⚡ Fast Operation | 30 ms | `[Perf] FastOperation: 30.14 ms` |
| 🐢 Slow Operation | 350 ms | `⚠️ [Perf WARNING] SlowOperation: 351.22 ms (threshold=100 ms)` |
| Set threshold → 20 ms | — | `[S4] Threshold set to 20 ms (strict mode).` |
| Run 50 ms operation | 50 ms | `⚠️ [Perf WARNING] MediumOperation: 51.08 ms (threshold=20 ms)` |
| Reset threshold → 100 ms | — | `[S4] Threshold reset to 100 ms (default).` |

All output appears live in the Diagnostics Log page. Notice the threshold display at the top of the page updates in real-time as you change it.

---

### 10.7 Scenario 5 — Thread Safety Guards 🧵

**What it proves:** `ThreadGuard` catches both wrong-thread UI access and potential `async` deadlock patterns — pinpointing the exact file and line number.

**Three sub-scenarios:**

#### ✅ Correct — Main thread check (silent pass)
```
[S5] ✅ EnsureMainThread passed — we are on Thread #1 (main thread).
```
Button click handlers always run on the UI thread. This confirms the guard is working and is silent when correct.

#### ❌ Wrong thread — `EnsureMainThread` from `Task.Run`
```
[S5] Dispatching EnsureMainThread() to a background thread (Task.Run)...
[S5] Now running on Thread #6 (background). Calling EnsureMainThread()...
[ThreadGuard] ⚠️  NOT on main thread! Called from 'OnWrongThread' in
              Scenario5ThreadingPage.xaml.cs:41 (Thread #6)
```
This is the warning you would see if you updated an `ObservableCollection` or changed a label directly from inside `Task.Run`.

#### ❌ Blocking call — `.Wait()/.Result` pattern
```
[S5] On Thread #8 with SynchronizationContext installed. Calling WarnIfBlockingCall()...
[ThreadGuard] ⚠️  Possible blocking call detected! A SynchronizationContext is
              active but code is running on Thread #8. Avoid .Wait()/.Result
              in 'OnBlockingCall' (Scenario5ThreadingPage.xaml.cs:70).
```
The demo creates a `LongRunning` OS thread, installs a `SynchronizationContext` on it (simulating a captured context), then calls `WarnIfBlockingCall()` — matching exactly the conditions under which a deadlock would occur in a real app.

---

### 10.8 Scenario 6 — Manual Snapshot Comparison 📸

**What it proves:** You can precisely control when snapshots are taken and compare them to see exactly which types grew or shrank.

**Step-by-step flow (buttons enable sequentially):**

| Step | Button | What happens | Output |
|---|---|---|---|
| 1 | Take Before Snapshot | `ForceGC()` then `Snapshot()` | `[S6] Before snapshot — 0 DemoObjects alive` |
| 2 | Create 3 Tracked Objects | Creates 3 `DemoObject` instances, calls `LeakTracker.Track()` on each | `[S6] Tracked 3 DemoObjects (strong refs held)` |
| 3 | Release + Force GC | Clears the list, runs `ForceGC()` | `[S6] Released all strong refs + GC forced` |
| 4 | Take After + Compare | `Snapshot()` then `Compare()` | `[LeakDetector] No changes detected. ✅` |

**To intentionally trigger a leak in this scenario:** Run Steps 1 → 2 → 4 (skip Step 3). The output will show:
```
[LeakDetector] Snapshot diff (Δ=+3):
  [DemoObject] Δ=+3  (after=3)
[S6] ❌ Leaks detected — some DemoObjects are still in memory.
```
Because you kept strong references in the list, the GC cannot collect them, and the diff correctly reports them as growth.

---

### 10.9 Key Design Decisions

| Decision | Why |
|---|---|
| **`DiagnosticsService.Instance` (singleton)** | All 3 library loggers (`LeakTracker`, `Perf`, `ThreadGuard`) route into one place. No DI setup required — any page accesses `DiagnosticsService.Instance.Log(...)` directly. |
| **Live in-app log** | You never need logcat or the VS Output window. The color-coded `CollectionView` is visible on any device or emulator without connecting to a debugger. |
| **Color-coded entries** | 🔵 Info / 🟠 Warning / 🔴 Problem allows instant visual triage of dozens of log lines without reading every message. |
| **Scenario 1 vs 2 contrast** | Both pages use the exact same static event pattern. S1 adds one `OnDisappearing` override with a single `-=`. Same code structure, completely opposite GC outcome — making the difference impossible to miss. |
| **Scenario 3 instance counter** | Each visit shows `#1`, `#2`, `#3` on-screen. The number is a `static int` incremented in the constructor. It makes it visually obvious that **new objects are created** while old ones survive — the growing leak becomes intuitive. |
| **Scenario 5 `LongRunning` thread** | `Task.Run` strips the `SynchronizationContext`, so `WarnIfBlockingCall` would not fire from it. Using `TaskCreationOptions.LongRunning` creates a real OS thread we fully control — we install the context manually to reproduce the exact conditions of a `.Wait()` deadlock. |
| **Scenario 6 step machine** | Buttons are enabled/disabled sequentially (Steps 1→2→3→4). This prevents out-of-order execution and guides the user through the correct snapshot workflow, matching the README documentation exactly. |

---

## License

MIT — see [LICENSE](LICENSE).
