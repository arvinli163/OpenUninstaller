# OpenUninstaller

Windows 10/11 软件卸载管理器 MVP。技术栈：C#、WPF、.NET 8、SQLite。

## 已实现
- 扫描 HKLM 64 位、HKLM 32 位、HKCU 卸载注册表项。
- 展示软件名称、版本、发行商、安装日期、架构、安装位置。
- 搜索与多选。
- 启动软件注册的官方卸载命令。
- 安装前后快照：常见程序/用户目录，以及卸载项、Run、RunOnce 注册表树。
- SQLite 保存监控会话和文件/注册表变化记录。
- 便携版和 Inno Setup 安装包发布脚本。


## GitHub Actions 自动编译（推荐）

项目已包含 `.github/workflows/build-windows.yml`。将项目推送到 GitHub 的 `main` 或 `master` 分支后，Actions 会在 Windows 云端构建机自动生成：

- `OpenUninstaller.exe`：便携版单文件程序
- `OpenUninstaller-portable-win-x64.zip`：便携版压缩包
- `OpenUninstaller-Setup-0.1.0.exe`：Inno Setup 安装包

操作步骤：

1. 将整个项目目录提交并推送到 GitHub 仓库。
2. 打开仓库的 **Actions** 页面，进入 **Build Windows packages** 工作流。
3. 等待 `Build Windows x64` 任务成功。
4. 在该次运行页面底部的 **Artifacts** 区域下载 `OpenUninstaller-Windows-x64`。
5. 如果推送形如 `v0.1.0` 的 Git tag，工作流还会自动创建 GitHub Release，并附上便携版 ZIP 与安装版 EXE。

也可以在 Actions 页面点击 **Run workflow** 手动触发。PR 会执行构建验证，但不会发布 GitHub Release。

## 环境
Windows 10/11 x64、.NET 8 SDK。安装版需要 Inno Setup 6。

## 构建
```powershell
.\scripts\build.ps1
```
## 便携版
```powershell
.\scripts\publish.ps1 -Mode portable -Runtime win-x64
```
输出 `artifacts\portable\OpenUninstaller.exe`。

## 安装版
```powershell
.\scripts\publish.ps1 -Mode installer -Runtime win-x64
```
然后使用 Inno Setup 6 编译 `installer\OpenUninstaller.iss`。注意 Inno 脚本默认读取 `artifacts\installer`，请将 publish 输出目录设为 installer，或将 EXE 复制到该目录后再编译。

## 数据库
`%LOCALAPPDATA%\OpenUninstaller\openuninstaller.db`，表为 `MonitorSessions` 和 `MonitorChanges`。

## 安装监控范围与限制（重要）
当前 MVP 是**安装前后快照差异监控**，扫描 Program Files、ProgramData、当前用户 AppData、开始菜单、桌面，以及 HKLM/HKCU 的卸载项和启动项注册表树。它能检测快照时仍存在的新增、修改、删除项目，但不是内核级完整追踪，无法保证捕获瞬时文件、所有用户 hive、自定义目录、驱动活动或每一条系统调用。

若要实现真正全面的安装追踪，后续需评估 ETW 事件采集和签名的文件系统 minifilter / 注册表回调驱动，并进行安全审计。不得以关闭 Windows 安全机制或安装未知驱动作为部署方式。因此本版本不应宣传为“完整系统级监控”。

## 安全边界
- 卸载只调用软件自身注册的卸载命令，不自动删除程序目录。
- 尚未实现自动深度清理和强制删除，避免误删共享组件。
- 建议先在虚拟机测试。
- 不修改或绕过第三方软件许可证。

## 后续路线
1. 文件/注册表残留扫描、关联评分、删除预览和备份。
2. 安全强制卸载工作流。
3. ETW/驱动级安装事件追踪。
4. 服务、计划任务、启动项和 Windows 应用管理。
5. 自动更新、代码签名、多语言。

## License
MIT。第三方依赖遵循其各自许可证。


## 第二阶段功能

- **残留扫描**：基于软件名称/发行商，在当前用户 AppData、ProgramData、开始菜单和桌面提出候选项；并列出该软件原始卸载注册表项。名称匹配可能误报，必须人工审核。
- **安全清理**：文件/目录移动到 `%LOCALAPPDATA%\OpenUninstaller\Quarantine`；卸载注册表项先通过 `reg.exe export` 备份再删除。不会自动清理 Program Files、Windows 目录、共享 DLL、服务或驱动。
- **强制卸载**：跳过厂商卸载程序，进入残留候选审核流程；不是无确认的一键递归删除。
- **实时监控接口**：`LiveInstallMonitor` 使用 `FileSystemWatcher` 观察指定目录。它属于用户态监控，存在缓冲区溢出/权限限制，不能称为内核级完整监控。
- **内核驱动路线**：见 `driver/README.md`。本仓库未提供可加载、已签名的驱动。后续需用匹配版本 WDK 实现 Minifilter 与注册表回调，在隔离 VM 测试并按 Windows 签名要求发布。


## 第三阶段增强（用户态采集 + 驱动原型）

- **智能残留关联**：对候选文件/目录综合名称、发行商、登记安装路径、卸载程序目录、文件版本、文件公司信息、可建立证书链的签名主题，以及相关注册表引用进行评分。评分是线索，不是删除授权；签名验证采用本机证书链构建，未执行在线吊销检查。
- **实时注册表监控**：新增 `RegistryChangeMonitor`，使用 `RegNotifyChangeKeyValue` 监控 HKLM/HKCU 的 SOFTWARE、卸载项、Run 和 Services 树。系统通知只表明监控树发生变化，不提供精确值前后差异。
- **统一安装时间线**：新增 `InstallTimelineMonitor`，合并文件系统事件与注册表事件；Services 注册表树变化归类为“服务/注册表”。事件写入 SQLite `TimelineEvents` 表，并在安装监控结束后展示。
- **服务事件边界**：当前采集的是 Services 注册表配置变化，不等同于 SCM 的服务启动/停止状态事件，也不能捕获所有服务控制操作。
- **内核驱动原型**：`driver/OpenUninstallerMonitor.c` 包含 Minifilter 与注册表回调源码原型。尚未完成用户态通信端口，也未在目标 Windows 版本上构建、签名或测试；不能部署到日常/生产系统。详见 `driver/README.md`。

### 用户态监控边界

实时监控会受权限、注册表视图、文件系统通知缓冲区溢出和监控根目录限制影响。注册表通知不会提供精确键值差异。安装前后快照仍保留，用于补充检测持久变化；两种数据应结合判断，不应把用户态监控宣传为无遗漏记录。
