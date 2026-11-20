# Kernel monitoring driver engineering track

This directory is a design boundary, not an installable or production driver.

A real Windows filesystem minifilter requires the Windows Driver Kit (WDK), native kernel C,
testing in a disposable VM, compliant signing and deployment. Registry monitoring requires
`CmRegisterCallbackEx` and careful callback lifetime/IRQL/unload handling.

Planned design:
- File system: Filter Manager minifilter (`FltRegisterFilter`, pre/post operation callbacks).
- Registry: `CmRegisterCallbackEx` for key/value create, set, delete and rename metadata.
- IPC: bounded authenticated communication port to the desktop service; no arbitrary command channel.
- Data minimization: event metadata only; do not collect file contents, credentials, or unrelated user data.
- Visible controls, bounded queues, dropped-event counters, clean unload and uninstall.

Not implemented or signed. Do not load or distribute a driver until implemented, signed, reviewed,
and validated against supported Windows builds. `LiveInstallMonitor` is only a user-mode watcher.
