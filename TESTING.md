# Tests and slot-lock verification

## Automated checks

Prerequisites: .NET 10 SDK and `VINTAGE_STORY` pointing to a Vintage Story 1.22
installation containing `VintagestoryAPI.dll`, `VintagestoryLib.dll`, the vanilla
mods and `Lib/`. No additional dependency mods are required.

Run from the solution directory:

```powershell
$env:VSTEST_TESTHOST_SHUTDOWN_TIMEOUT = "30000"
dotnet test XInvTweaksFork.sln --configuration Release --logger trx --results-directory TestResults
```

The shared **All Tests** run configuration (`.run/All Tests.run.xml`) invokes this
same command from the solution directory. Select **All Tests** in Rider's run
selector and Run. It uses `dotnet` on PATH and inherits `VINTAGE_STORY`; restart
Rider if you set the variable after starting it. Results are written to
`TestResults/`. This configuration launches the CLI; it does not promise native
test-window integration or test-process debugging.
Release output avoids overwriting the Debug mod DLL while a local game client
is using it. The shutdown timeout lets Atlas 0.16.1 finish releasing its server.
The configuration type and option names were checked against the installed
Rider 2026.1 implementation and JetBrains' run-configuration documentation.

The initial solution had no test projects (baseline: zero discovered tests).
The new xUnit tests exercise the production lock-state implementation and actual
config JSON round-trips, including unavailable indices, restored capacity,
equipped-bag exclusion, legacy negative indices, unlocking and selection cleanup.
An additional test installs the real Harmony patches against the local game's
slot-grid methods to validate method/field bindings without creating a GUI.

The config-reset regression adds scalar-setting JSON tests and a separate
`XInvTweaksFork.AtlasTests` project using Pixnop Atlas 0.16.1. Its fixture seeds
`ExtendChestUi: false` before booting the real staged mod, then checks the mod's
loaded config and the file written by startup/world-load handlers. It also checks
another disabled toggle, a non-default numeric setting and retained slot locks.
All 21 pre-existing tests remain unchanged. The pre-fix regression run reproduced
13 unit failures and the Atlas startup failure.

Atlas was not indexed by Context7; setup and fixture APIs were verified against
the [official quickstart](https://github.com/Pixnop/Atlas) and
[scenario documentation](https://github.com/Pixnop/Atlas/wiki/Writing-Scenarios).

### Client-only coverage boundary

Slot selection and sorting run on the client. Atlas boots a
headless server and cannot execute these dialogs, mouse handlers or rendering.
With the user's approval, this feature uses automated lock-state tests plus the
real-client checklist below, rather than a vacuous Atlas scenario. Config loading
also runs on the server, so its reset regression has meaningful Atlas coverage;
this does not validate the client dialog or rendering.

The CLI command is validated separately from launching the saved configuration
inside Rider. Neither an IDE launch nor the real-client checks below have been
performed by the coding agent.

## Priority feature toggle

In `ModConfig/xinvtweaksfork.json`, set `"EnablePriority": false` to disable
priority sorting. The default is `false`, including for existing configs that
omit the setting. Change it with the game closed and restart the client.

When disabled, the priority text input and its clear button are hidden, and the
lock button moves up to avoid an empty row. Both text-based priority and the
`Priorities` / `"priority"` sort rule are ignored, including cached priority
values. Other sort rules still apply. Saved priority rules are retained so
re-enabling the feature restores them.

Automated checks cover both comparator modes, stale text/cached values, config
round-trips and real staged-mod startup with the setting disabled or omitted.
The shared **All Tests** Rider configuration includes these tests without a
filter. The CLI command is verified; launching it in Rider remains unverified.

Real-client checks (pending): disable the option, join a world and confirm the
input/clear row is absent while sorting and slot locks still work. Re-enable it,
restart and check that the row returns and entering/clearing priority text works.

## ConfigLib / Mod settings

Install compatible versions of [ConfigLib](https://mods.vintagestory.at/configlib)
and its [ImGui dependency](https://mods.vintagestory.at/imgui). In a world, open
**Esc → Mod settings → Xandus Inventory Tweaks Fork**. All 11 boolean toggles
and both integer settings are editable. Click Save, then restart the game to
apply the changes, since many features install Harmony patches only at startup.
The menu explicitly marks the restart requirement.

The integration is optional: no ConfigLib DLL reference or required mod
dependency is added. Its documented JSON definition is at
`assets/xinvtweaksfork/config/configlib-patches.json`, points directly at the
existing `ModConfig/xinvtweaksfork.json`, and marks settings client-side so
multiplayer players can edit their own preferences rather than server values.
Arrays/dictionaries still use the JSON file; slot locks also have their dedicated
in-game editor. The default for EnablePriority is false.

When ConfigLib is enabled, mod-owned config writes preserve scalar values already
saved on disk, while still saving current slot-lock and other collection changes.
This prevents locking a slot or finalizing a world from overwriting pending menu
changes. It does not attempt to hot-reconfigure the active session.

ConfigLib was not indexed by Context7. This integration follows the upstream
[existing-mod example](https://github.com/maltiez2/vsmod_configlib/wiki/Examples)
and [JSON API](https://github.com/maltiez2/vsmod_configlib/wiki/JSON-API), with
client-side/save behavior checked against its source.

Unit tests verify all exposed defaults/types, translations and merging behavior.
New Atlas contracts inspect the real staged asset, boot without the optional
dependency, and test pending edits surviving production writes through the real
game config API. These do **not** load ConfigLib or execute its menu; full
ConfigLib + ImGui client integration remains unverified.

As explicitly authorized, only the two existing priority-default tests were
updated to expect false; all other existing tests/fixtures/helpers stay unchanged.
The shared **All Tests** configuration includes all new tests automatically.
Validation: the full solution command above passed 49 unit tests and 5 Atlas
scenarios (54 total, zero failures/skips), up from the 48-test baseline.
The Rider launch and the real ConfigLib/ImGui menu remain unverified.

Client verification checklist (pending):

1. With ConfigLib + ImGui enabled, verify the menu appears with readable labels,
   defaults matching the mod config, and nonnegative numeric inputs.
2. Toggle ExtendChestUi and EnablePriority, change Delay, then Save. Lock/unlock
   a backpack slot before leaving: the file must retain both the menu changes
   and the new slot locks.
3. Restart the client and verify the saved settings take effect. Test Restore,
   Defaults and Reload before saving, and confirm unrelated lists/dictionaries
   are not lost.
4. In multiplayer, confirm these controls edit local preferences without
   requiring controlserver permission or changing another player's settings.
5. Without ConfigLib, confirm the mod still loads and the normal JSON/slot-lock
   editors continue working.

## Using slot locks

Open your backpack, click **Lock slots**, then left-click storage slots to toggle
locks, including empty slots. A black padlock outline marks locked slots, even outside
selection mode. Click **Done locking slots** to return to normal item interaction.
Closing the last inventory dialog also exits selection mode.

Locks use the existing config and existing sort/push semantics. They prevent
backpack sorting and automatic push from taking those slots; they do not prevent
ordinary manual movement outside selection mode or change pull/fill behavior.
The equipped bags, hotbar and containers are not selectable lock targets.

Unavailable positive indices are **suspended, not deleted**. The twentieth storage
slot is config index `19` (inventory slot `23`, after four equipped-bag slots).
With only fifteen storage slots its lock is inactive, and returns when capacity
is restored. Legacy negative config entries still count backward from the
current inventory end. Unlocking such a slot removes both aliases if present.
Selections save immediately to `xinvtweaksfork.json`.

## Real-client checklist (not automated; pending)

Use a disposable world and back up your mod config.

1. Open the backpack and enter lock mode. Check the button changes to
   **Done locking slots**. Left-click empty and occupied storage slots: each gets
   a padlock and no items move, including when holding an item on the cursor or
   pressing Shift/Ctrl. A second click removes the padlock.
2. Right-click, wheel, drag and press Enter on backpack storage slots while
   editing: no item transfer should occur. Equipped bags, hotbar and chest slots
   must not acquire locks. Confirm normal movement resumes after Done.
3. Lock occupied and empty slots; exit lock mode. Sort the backpack and push to
   a matching chest: locked slots and their contents stay put, while unlocked
   slots still sort/push. Existing pull/fill behavior remains unchanged.
4. Lock storage slot 20, empty the bags to be removed, and remove enough bags to
   leave 15 storage slots. Sort/push and reopen inventory: no error, no phantom
   padlock on another slot. Restore capacity: slot 20's lock returns.
   Repeat with no backpacks, and with lock mode still enabled.
5. Close the last inventory window while editing; reopen and verify normal
   interaction and the **Lock slots** label. With several inventories open,
   closing only one must not prematurely exit selection mode.
6. Restart the client/world: locks persist, but selection mode is off. Check the
   config retains unrelated custom settings and unavailable lock indices.
7. Check standard inventory and PlayerInventoryLib (if installed), scrolling,
   GUI scaling and window repositioning: badges align with the actual rendered
   slots and do not appear outside clipped inventory bounds.
8. With `ExtendChestUi: false`, no new button/input/render patches should be active.
   Set it with the game closed, join a world, leave, and confirm it stays false
   in `ModConfig/xinvtweaksfork.json`; rejoining must keep the extra dialog absent.
   With `PushPullWheel: false` and UI enabled, locking still works without
   re-enabling the mod's Shift-wheel transfers.
