// PadLink IDD — scaffold
// TODO(PadLink): Implement DriverEntry, AddDevice, IddCx callbacks per Microsoft IndirectDisplay sample.
// WHY incomplete: requires full WDK project linkage (IddCx.sys import, INF, coinstallers).
// NEXT: Start from official WDK IndirectDisplay sample; replace this file incrementally.

#include <wdm.h>

extern "C" DRIVER_INITIALIZE DriverEntry;

NTSTATUS
DriverEntry(
    _In_ PDRIVER_OBJECT DriverObject,
    _In_ PUNICODE_STRING RegistryPath
)
{
    UNREFERENCED_PARAMETER(DriverObject);
    UNREFERENCED_PARAMETER(RegistryPath);

    // Intentionally fail until IddCx is wired — prevents accidental "empty" driver load in production.
    return STATUS_NOT_SUPPORTED;
}
