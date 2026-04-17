# PadLink Indirect Display Driver (IddCx)

This folder will contain the **Windows Indirect Display Driver** that exposes a **virtual monitor** to the OS using **IddCx** (Indirect Display Driver Class Extension).

## Why IddCx / IDD (not mirror hacks)

Microsoft’s **Indirect Display Driver** model is the supported way to create a **software-emulated display** that participates in the same **modes, topology, and GDI/DXGI composition** paths as a physical monitor. Random GDI `BitBlt` / DXGI duplication of the *primary* surface is **not** a second monitor — it cannot extend the desktop the way users expect.

**Official references**

- [Indirect Display Driver Model Overview](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/indirect-display-driver-model-overview)
- [Implementing an Indirect Display Driver (IddCx)](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/indirect-display-driver-implements-iddcx)
- [IddCx objects and handles](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/iddcx-objects)
- Sample: search GitHub for **Microsoft Windows Driver Samples** → **IndirectDisplay** (WDK sample driver)

## What ships in this repo today

- **Scaffold only** (`Driver.cpp`, `Adapter.cpp` stubs) — **no functional IddCx hookup** in this first pass.
- **WHY incomplete:** Bringing up a signed IddCx driver requires **WDK**, matching **Visual Studio** toolchain, **test signing**, and iterative debugging on hardware.
- **BLOCKER:** Local dev environment must install WDK; driver must load without bugcheck.
- **NEXT:** Copy Microsoft `IndirectDisplay` sample, rename to PadLink, reduce to a single connector/monitor, verify modes in Settings → Display.

## Build prerequisites (typical)

1. Visual Studio 2022 with **Desktop development with C++**
2. **Windows Driver Kit (WDK)** matching your OS/SDK version
3. Enable **test signing** (development machines):

   ```powershell
   bcdedit /set testsigning on
   ```

   Reboot. (See `docs/SETUP.md`.)

## Install / test (outline)

1. Build the driver package (`.inf`, `.sys`) from Visual Studio / MSBuild.
2. `pnputil /add-driver PadLink.inf /install` (elevated; exact path TBD once project is wired)
3. Confirm a **second display** appears in **Settings → System → Display** and can be set to **Extend**.

## Notes

- **Pointer / touch injection** stays in **user mode** (host app). The driver’s job is **display topology + presenting a swapchain target**, not HID spoofing.
- **Cursor:** IddCx has cursor-related DDIs — integrate when moving beyond the minimal bring-up.
