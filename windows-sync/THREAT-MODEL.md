# 虚拟同步桥威胁检查

资产：GitHub PAT、正式浏览器存储、私有正式 state、配对会话、未发送操作队列。

边界：PAT 仅由当前 Windows 用户的 CredReadW 进入本机进程内存。仅向固定 HTTPS api.github.com 发送，禁止重定向。浏览器只接收15分钟会话，不接收 PAT。DPAPI 用于本地缓存和队列，不能防御同一用户下的恶意进程。

1. CSRF/恶意网站：精确 Host/Origin、自定义请求头、短期 bearer，会话绑定 Origin；CORS 不是唯一鉴权。
2. DNS rebinding：Host 只接受 127.0.0.1:18417，不接受 localhost、LAN 或任意域名。
3. Pairing：192位随机码、5分钟、一次性、8次/分钟；服务必须显示本地确认，默认 No。会话192位，15分钟，仅内存，换码撤销全部会话。
4. 数据注入：限制600KB请求、16KB头、64层JSON；只接受白名单字段和确定性引用验证。将文本按原应用转义后渲染，不执行任务内容。
5. 并发：SHA条件更新和 operationId 去重；同字段冲突及删除均停机待处理；不静默覆盖。未确认结果可用相同 operationId 重试。
6. 丢失：队列先于网络持久化，成功读回后清除；DPAPI和原子替换，前一版本保留备份。损坏文件不被当空数据使用。
7. 正式路径：当前二进制只允许 sync/tests/*.json。浏览器普通工作区不发起私有同步，显式虚拟工作区用独立存储键。
8. Pages供应链：公开网页可被更新，同一GitHub账号的Pages项目共享Origin，因此只信任经本机审阅并明确配对的页面。本轮仅合成数据，正式操作优先本地受信UI。

不声称覆盖：同一Windows用户下恶意进程、浏览器扩展入侵、操作系统被控制、用户误配对恶意页面、GitHub PAT未来过期及全授权范围证明。未修改浏览器网络安全机制。

## V0.3.1 preparation boundary
- Normal Pages scripts are remote code and can access the app's browser storage. Pair only the reviewed deployment, confirm its exact origin in Windows, and keep the 15-minute session memory-only. Pairing is not approval to upload.
- The preparation listener uses a separate loopback port 18418 with no Engine instance. Paired write routes return 403 before reading or writing remote data; Cloud separately denies canonical writes and deletes. There is no approval issuance endpoint or generic unlock switch.
- Only an explicit backup/preview request sends the full existing browser snapshot to the authenticated loopback service. It is DPAPI encrypted, restored in memory and validated before difference calculation. GitHub receives GET requests only. Browser responses contain redacted counts/digests, never task values, full storage or PAT.
- Detailed per-field values appear only in a native local dialog and DPAPI-encrypted files outside the repository. Local PRODUCTION-* report files and all backups must be excluded from Git, CI and distribution packages even when redacted.
- Profile source IDs bind requests within a service session; they identify an opted-in browser storage, not a cryptographic proof of an OS Chrome profile. Operators must start from the user's existing normal tab. Missing, demo, empty or mismatched sources cannot authorize migration.
- New local changes invalidate the UI preview. Any future approval must be single-use and bind fresh backup, preview digest, local projection and current remote SHA/revision; this release cannot issue or consume such a production grant.
- Offline or unsupported remote data blocks readiness. The app remains local; no initialization, rescheduling, completion or deletion is inferred. DPAPI protects data at rest for the current Windows user and is not a defense against malicious processes running as that user.
