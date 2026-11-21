# Focus & Growth Windows 同步服务 · V0.3 虚拟测试版

当前仅允许虚拟工作区。正式 `sync/state.json` 被程序路径白名单锁定，不能通过网页参数、API 或启动参数打开正式写入。真实数据首次同步需用户另行确认，届时还需完成生产迁移的独立验收。

## 启动、配对与停止

1. 使用保存了 `FocusGrowthSync/GitHub` 凭据的同一 Windows 用户双击 `FocusGrowthSync.exe`。不需要再次填写 GitHub Token。需要 .NET Framework 4.x，使用 Windows 自带组件，无第三方运行依赖。
2. 点击本地程序的 **Open local synthetic workbench**。本地预览地址为 `http://127.0.0.1:18417/?syncTest=1`。
3. 在程序中点击 **New pairing code**，将一次性配对码复制到网页“虚拟同步”面板，点击“配对本地服务”，再在 Windows 窗口核对来源并选择 Yes。配对码5分钟有效、使用一次作废；会话15分钟有效，刷新、断开或重启后需重新配对。配对码不是 GitHub Token。
4. 用“添加虚拟任务”或原有任务界面编辑合成项目、任务和排程，点击“手动同步”。原有 JSON 导入导出仍保留。
5. 关闭本地程序或点击 **Stop service** 停止轮询和监听。没有注册系统服务、开机自启或全电脑输入监测。

原始网页使用的 `start-workbench-v1` 不变。只有显式 `?syncTest=1` 使用 `start-workbench-sync-test-v1`，以免污染真实浏览器任务。配对会话只在内存中保存；网页不接收 GitHub PAT。测试工作区配对时会先在浏览器保存一份本地完整备份。浏览器禁用存储时不得继续同步。

## 同步行为

- Windows 服务仅监听 `127.0.0.1:18417`。云端仅允许 `tang0709/focus-growth-data`、`main` 分支和 `sync/tests/<工作区ID>.json`；每次写入前确认仓库身份及 private=true。
- 项目、任务、排程按稳定 ID 和字段三方合并，保留无关记录。相同字段的并发更改和任何删除停止同步，不自动选一方。冲突面板显示各字段的两端值，必须逐字段明确选择；提交前备份，旧冲突队列保留为加密备份。远端或本地数据变化时要求重新核对，不能清空队列强行覆盖。
- 使用 GitHub blob SHA 条件更新；409/422 后重新读取并检测冲突；每个变更使用固定 operationId，写入后读回验证。成功变更增加 revision；不确定结果和离线时保留原操作与未发送队列。
- 每30秒轮询，使用 ETag 条件读取；故障后退避。网页渲染和 `save()` 不触发网络同步，只有项目、任务、排程修改进入显式脏标记路径并做防抖。
- 不上传活动、想法、备注、主题、启动状态、完整历史、当前运行计时器或 timerMs。计时 opt-in 尚未启用。传入数据通过原有网页验证路径应用，保留本地备注、计时与其他集合。
- Windows 元数据、缓存、原操作队列使用当前用户 DPAPI 加密，原子替换并保留前一版备份，存储在 `%LOCALAPPDATA%\FocusGrowthSync\synthetic-v1`，不在仓库或网页根目录。损坏数据停止使用并保留原文件。

## 网络与威胁边界

健康路由不返回个人数据。其他 API 要求精确 Host、允许的 Origin、短期会话和自定义防 CSRF 请求头；配对需原生界面确认。严格限定 JSON、方法、路由、请求大小，禁用任意文件访问、命令执行及 PAT API。错误信息为固定代码，不记录 HTTP 头、响应正文或任务内容。

允许来源是本地页面与 `https://tang0709.github.io`。GitHub Pages 同源下的其他项目共享 Origin，因此 Origin 本身不代表可信；远程页面的配对仍必须核对用户意图并经本地确认。本轮远程配对仅涉及合成数据，正式私有操作优先使用本地受信前端。公开页面代码可以更新，不能视为持久信任锚。

Chrome 对公网到回环请求可能要求本地网络访问权限。不得修改浏览器 flags、关闭安全保护或接受证书警告来绕过限制；若实际 Pages 测试不适用，应使用本地受信工作台或后续原生消息扩展。[Chrome 官方本地网络访问说明](https://developer.chrome.com/blog/local-network-access)

## 开发与复现测试

在 PowerShell 7 中执行 `./build.ps1` 编译。源码分为 `Core.cs`（schema/合并/配对/持久化）、`Transport.cs`（凭据/GitHub/状态机）、`Bridge.cs`（回环 HTTP）、`Program.cs`（本地控制窗口）、`Tests.cs`（合成测试）。

- `FocusGrowthSync.exe --selftest`：纯本地单元测试，结果 `unit-results.json`。
- `FocusGrowthSync.exe --cloud-test`：读取已有凭据，在随机私有测试文件执行写入、更新及过期 SHA 测试，然后删除文件；结果 `cloud-results.json`。
- `node tests/frontend.cjs`：隔离 VM 中的网页逻辑及兼容性测试；结果 `frontend-results.json`，不能代替真实浏览器测试。
- 启动服务后 `node tests/security.cjs`：真实 HTTP 边界测试；结果 `security-results.json`。

临时单元测试文件位于项目 `work/sync-build`，收尾仅清理这些可再生成文件；必要源码、可运行程序和最终报告保留在 outputs/。

云端测试的虚拟内容会留在私有 Git 历史中。主程序的独立虚拟工作区文件用于持续测试，非正式数据；清理时必须针对确切路径和当前 SHA，不修改正式文件。

## 当前验收与部署

V0.3 已发布：正常网页 https://tang0709.github.io/focus-growth/ ，隔离虚拟同步工作区 https://tang0709.github.io/focus-growth/?syncTest=1 。53项本地测试、实际本地双向同步、Pages来源配对及线上写回均已验证。详情见本地交付的 TEST-REPORT.md 和 summary.json。普通工作区同步默认关闭，正式 sync/state.json 写入仍锁定；真实首同步需另行确认、完整备份和迁移验收。当前运行程序可继续使用，无需重新配置GitHub凭据。
