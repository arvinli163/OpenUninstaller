#include <fltKernel.h>
#include <ntstrsafe.h>

PFLT_FILTER gFilter = NULL;\nstatic NTSTATUS DriverUnload(_In_ FLT_FILTER_UNLOAD_FLAGS Flags);
LARGE_INTEGER gCookie = { 0 };

static VOID LogEvent(_In_z_ PCSTR kind, _In_ PUNICODE_STRING name)
{
    if (name && name->Buffer)
        DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL,
            "[OpenUninstaller] %s: %wZ\n", kind, name);
}

static FLT_PREOP_CALLBACK_STATUS
PreCreate(_Inout_ PFLT_CALLBACK_DATA Data, _In_ PCFLT_RELATED_OBJECTS FltObjects,
          _Flt_CompletionContext_Outptr_ PVOID *CompletionContext)
{
    UNREFERENCED_PARAMETER(FltObjects);
    UNREFERENCED_PARAMETER(CompletionContext);
    if (Data->Iopb->TargetFileObject)
        LogEvent("FILE_CREATE", &Data->Iopb->TargetFileObject->FileName);
    return FLT_PREOP_SUCCESS_NO_CALLBACK;
}

static FLT_PREOP_CALLBACK_STATUS
PreSetInformation(_Inout_ PFLT_CALLBACK_DATA Data, _In_ PCFLT_RELATED_OBJECTS FltObjects,
                  _Flt_CompletionContext_Outptr_ PVOID *CompletionContext)
{
    UNREFERENCED_PARAMETER(FltObjects);
    UNREFERENCED_PARAMETER(CompletionContext);
    if (Data->Iopb->TargetFileObject)
        LogEvent("FILE_SET_INFORMATION", &Data->Iopb->TargetFileObject->FileName);
    return FLT_PREOP_SUCCESS_NO_CALLBACK;
}

static NTSTATUS RegistryCallback(_In_ PVOID Context, _In_ PVOID Argument1, _In_ PVOID Argument2)
{
    REG_NOTIFY_CLASS op = (REG_NOTIFY_CLASS)(ULONG_PTR)Argument1;
    PUNICODE_STRING name = NULL;
    UNREFERENCED_PARAMETER(Context);

    switch (op)
    {
    case RegNtPreCreateKeyEx:
        name = ((PREG_CREATE_KEY_INFORMATION)Argument2)->CompleteName; break;
    case RegNtPreOpenKeyEx:
        name = ((PREG_OPEN_KEY_INFORMATION)Argument2)->CompleteName; break;
    case RegNtPreSetValueKey:
        name = ((PREG_SET_VALUE_KEY_INFORMATION)Argument2)->ValueName; break;
    case RegNtPreDeleteValueKey:
        name = ((PREG_DELETE_VALUE_KEY_INFORMATION)Argument2)->ValueName; break;
    case RegNtPreDeleteKey:
    case RegNtPreRenameKey:
        break;
    default:
        break;
    }
    if (name) LogEvent("REGISTRY_CHANGE", name);
    else DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL,
        "[OpenUninstaller] Registry operation class=%u\n", (ULONG)op);
    return STATUS_SUCCESS;
}

static const FLT_OPERATION_REGISTRATION Callbacks[] = {
    { IRP_MJ_CREATE, 0, PreCreate, NULL },
    { IRP_MJ_SET_INFORMATION, 0, PreSetInformation, NULL },
    { IRP_MJ_OPERATION_END }
};

static NTSTATUS InstanceSetup(_In_ PCFLT_RELATED_OBJECTS FltObjects,
    _In_ FLT_INSTANCE_SETUP_FLAGS Flags, _In_ DEVICE_TYPE VolumeDeviceType,
    _In_ FLT_FILESYSTEM_TYPE VolumeFilesystemType)
{
    UNREFERENCED_PARAMETER(FltObjects); UNREFERENCED_PARAMETER(Flags);
    UNREFERENCED_PARAMETER(VolumeDeviceType); UNREFERENCED_PARAMETER(VolumeFilesystemType);
    return STATUS_SUCCESS;
}

static const FLT_REGISTRATION FilterRegistration = {
    sizeof(FLT_REGISTRATION), FLT_REGISTRATION_VERSION, 0, NULL, Callbacks,
    DriverUnload, InstanceSetup, NULL, NULL, NULL, NULL, NULL, NULL, NULL
};

static NTSTATUS DriverUnload(_In_ FLT_FILTER_UNLOAD_FLAGS Flags)
{
    UNREFERENCED_PARAMETER(Flags);
    if (gCookie.QuadPart) CmUnRegisterCallback(gCookie);
    if (gFilter) { FltUnregisterFilter(gFilter); gFilter = NULL; }
    return STATUS_SUCCESS;
}

NTSTATUS DriverEntry(_In_ PDRIVER_OBJECT DriverObject, _In_ PUNICODE_STRING RegistryPath)
{
    NTSTATUS status;
    UNREFERENCED_PARAMETER(RegistryPath);
    DriverObject->DriverUnload = NULL; /* Minifilter unload is registered below. */
    status = FltRegisterFilter(DriverObject, &FilterRegistration, &gFilter);
    if (!NT_SUCCESS(status)) return status;
    status = CmRegisterCallbackEx(RegistryCallback, &altitude,
        DriverObject, NULL, &gCookie, NULL);
    if (!NT_SUCCESS(status)) { FltUnregisterFilter(gFilter); gFilter = NULL; return status; }
    status = FltStartFiltering(gFilter);
    if (!NT_SUCCESS(status)) { CmUnRegisterCallback(gCookie); FltUnregisterFilter(gFilter); gFilter = NULL; }
    return status;
}
