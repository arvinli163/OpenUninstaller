# Windows kernel monitor (WDK source prototype)

本目录新增 `OpenUninstallerMonitor.c` 与 INF，包含文件系统 Minifilter（创建/设置文件信息前置回调）和注册表 `CmRegisterCallbackEx` 回调的最小原型。

## 重要状态声明

- 这是**驱动源码原型**，不是已签名、已验证或可直接部署的驱动。
- 当前内核回调只输出调试事件（DbgPrintEx），没有实现与桌面端的安全通信端口、事件队列、丢失计数、背压和隐私过滤。
- 回调没有覆盖所有文件操作/注册表通知类型，不能声称完整记录安装行为。
- INF 中的 Altitude 是开发占位值，正式提交前必须向 Microsoft 申请/取得合法 altitude；不能将占位值用于生产部署。
- 本仓库没有驱动签名证书、EV/attestation/WHQL 提交凭据，也没有实际 Windows 测试环境，因此**不能声称驱动已经签名或测试通过**。

## 构建与验证

1. 在 Windows 11 x64 的隔离测试虚拟机安装与目标 Windows SDK 匹配的 WDK、Visual Studio C++ 桌面工具。
2. 建立 Empty KMDF/Kernel Mode Driver 项目，选择 Minifilter 驱动类型，将 `OpenUninstallerMonitor.c` 加入工程，并链接 `FltMgr.lib`、`Cng.lib`（如后续实现加密 IPC）。
3. 使用 WDK 的 Static Driver Verifier（适用时）、Code Analysis、Driver Verifier，在可回滚 VM 中验证；先只启用测试签名并保留内核调试。
4. 生产发布需按 Microsoft 当前驱动签名政策通过 Hardware Dev Center 的适用提交流程（attestation 或 HLK/WHQL），安装由 Windows 信任链认可的签名包。
5. 验证卸载/休眠恢复/快速启动/多卷/高 IO/注册表并发/异常卸载，不通过时禁止部署。

建议补齐：安全的 Filter Manager communication port（仅元数据、ACL 限制）、用户态服务接收与持久化、注册表对象名称解析、操作结果回调、事件丢失计数、配置化过滤策略和卸载竞态测试。

## 安全原则

不得通过关闭 Secure Boot、禁用驱动签名强制或关闭 Defender 来安装。不要在生产主机加载未签名驱动。
