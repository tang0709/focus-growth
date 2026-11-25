/* Focus & Growth 0.4: scoped continuous sync. Production writes need separate native authorization. */
(() => {
 'use strict';
 const TEST = new URLSearchParams(location.search).get('syncTest') === '1';
 const API = TEST?'http://127.0.0.1:18420':'http://127.0.0.1:18421';
 const STORE=TEST?'start-workbench-sync-test-v04':'start-workbench-v1',clientId=crypto.randomUUID();
 const KEY = TEST?'fg-sync-test-meta-v04':'fg-sync-production-meta-v04';
 let generation=0,metaCorrupt=false,stale=false,persistenceFailed=false,seenRaw=localStorage.getItem(STORE);
 const fields = {
  projects: ['id','name','deadline','priority','weeklyMin'],
  tasks: ['id','projectId','parentId','name','estimate','date','goal','category','priority','done','actual','completedAt'],
  plans: ['id','taskId','date','start','duration']
 };
 const empty = () => ({schemaVersion:1, revision:0, initialized:false, updatedAt:null, operationIds:[], projects:[],tasks:[],plans:[]});
 const copy = x => JSON.parse(JSON.stringify(x));
 const stable = x => JSON.stringify(x && typeof x === 'object' ? Array.isArray(x) ? x.map(v=>JSON.parse(stable(v))) : Object.fromEntries(Object.keys(x).sort().map(k=>[k,JSON.parse(stable(x[k]))])) : x??null);
 let session = '', enabled = false, applying = false, busy = false, timer, status = '未连接', error = '', last = '', conflicts = [], failures = 0, nextAt = 0;
 let meta = {version:4,base:empty(),pending:null,bound:TEST};
 let latestRemote=null,conflictContext=null,serverPendingOperation=null;
 try{const old=localStorage.getItem(KEY);if(old){meta=JSON.parse(old);if(meta.version!==4||meta.base?.schemaVersion!==1)throw Error('unsupported_metadata');}}catch{metaCorrupt=true;status='冲突';error='同步元数据损坏或版本未知，原件保留；禁止自动同步。';}
 const persist=()=>{if(metaCorrupt)throw Error('metadata_corrupt_preserved');localStorage.setItem(KEY,JSON.stringify(meta));};
 const orderedRows=rows=>[...rows].sort((a,b)=>a.id<b.id?-1:a.id>b.id?1:0);
 const sameData=(a,b)=>Object.keys(fields).every(k=>stable(orderedRows(a[k]))===stable(orderedRows(b[k])));
 function assertFresh(){if(persistenceFailed)throw Error('local_storage_save_failed');if(stale||localStorage.getItem(STORE)!==seenRaw){stale=true;enabled=false;generation++;try{localStorage.setItem('fg-v04-stale-draft-'+Date.now(),JSON.stringify(db));}catch{}status='其他标签已更新数据';error='旧内存副本未覆盖正式存储；已暂停，先导出草稿并刷新。';show();throw Error('external_storage_change');}}
 window.FocusGrowthStorageGuard={before:assertFresh,after:()=>{seenRaw=localStorage.getItem(STORE);},failed:()=>{persistenceFailed=true;enabled=false;generation++;status='无法保存本地数据，已暂停同步';error='请导出当前草稿并检查存储空间。';show();}};
 window.addEventListener('storage',e=>{if(e.key===STORE&&e.newValue!==seenRaw){try{assertFresh();}catch{}}});
 async function checkpoint(raw){const proof=await request('/v1/checkpoint',{clientId,raw});if(proof.restoreTest!=='PASS'||proof.encrypted!==true)throw Error('native_backup_not_verified');return proof;}
 function browserBackup(){assertFresh();const raw=JSON.stringify(db);const slot=Date.now()%3;localStorage.setItem('fg-v04-restore-'+slot,raw);return raw;}

 function project() {
  const state=empty();
  for(const k of Object.keys(fields))state[k]=db[k].map(row=>{
   const safe=Object.fromEntries(fields[k].filter(f=>Object.hasOwn(row,f)).map(f=>[f,copy(row[f])]));
   if(k==='tasks'){safe.projectId=safe.projectId||null;safe.parentId=safe.parentId||null;safe.date=safe.date||'';safe.category=safe.category||'';safe.priority=safe.priority||'';safe.goal=safe.goal||'normal';safe.actual=safe.actual??null;if(safe.completedAt==null)delete safe.completedAt;}
   if(k==='projects'){safe.deadline=safe.deadline||'';safe.priority=safe.priority||'';safe.weeklyMin=safe.weeklyMin||0;}
   return safe;
  }).sort((a,b)=>a.id<b.id?-1:a.id>b.id?1:0);
  return state;
 }
 function merge(base,local,remote) {
  if(base.schemaVersion!==1||remote.schemaVersion!==1)throw Error('unsupported_schema');
  const out=copy(remote),issues=[];
  for(const col of Object.keys(fields)){
   const maps=[base,local,remote].map(s=>new Map(s[col].map(r=>[r.id,r])));
   out[col]=[];
   for(const id of [...new Set(maps.flatMap(m=>[...m.keys()]))].sort()){
    const [b,l,r]=maps.map(m=>m.get(id));
    if(b&&(!l||!r)){issues.push(`${col}/${id}/deletion_disabled`);continue;}
    if(!l){out[col].push(copy(r));continue;}if(!r){out[col].push(copy(l));continue;}
    const row=copy(r);
    for(const f of new Set([...Object.keys(b||{}),...Object.keys(l),...Object.keys(r)])){
     const lc=stable(b?.[f])!==stable(l[f]),rc=stable(b?.[f])!==stable(r[f]);
     if(lc&&rc&&stable(l[f])!==stable(r[f]))issues.push(`${col}/${id}/${f}`);
     else if(lc){if(Object.hasOwn(l,f))row[f]=copy(l[f]);else delete row[f];}
    }
    out[col].push(row);
   }
  }
  return {state:out,issues};
 }
 function show(){const badge=document.getElementById('fg-sync-status');if(badge)badge.textContent=`V0.4 ${TEST?'虚拟同步':'正式同步'} · ${status}`;const info=document.getElementById('fg-sync-info');if(info)info.textContent=`${status}${error?' · '+error:''}\n最近成功：${last||'尚无'}${conflicts.length?'\n冲突字段：'+conflicts.join('、'):''}${window.FocusGrowthDeviceSession?.info()?'\n'+FocusGrowthDeviceSession.warning():''}`;}
 async function rawRequest(path,body) {
  const controller=new AbortController(), timeout=setTimeout(()=>controller.abort(),70000);
  try {const response=await fetch(API+path,{method:body===undefined?'GET':'POST',headers:{'X-FG-Client':'1',...(body===undefined?{}:{'Content-Type':'application/json'}),...(session?{Authorization:'Bearer '+session}:{})},...(body===undefined?{}:{body:JSON.stringify(body)}),signal:controller.signal,cache:'no-store',credentials:'omit',redirect:'error'});
   let result;try{result=await response.json();}catch{throw Error('invalid_service_response');}
   if(!response.ok){if(response.status===401){session='';enabled=false;status='等待授权';}throw Error(result.error||'service_error');}return result;
  }finally{clearTimeout(timeout);}
 }
 let reconnecting=null;
 async function restoreDevice(){if(!window.FocusGrowthDeviceSession)return false;if(reconnecting)return reconnecting;reconnecting=(async()=>{const result=await FocusGrowthDeviceSession.recover(rawRequest);if(!result)return false;session=result.session;const raw=browserBackup();const attached=await rawRequest('/v1/attach',{clientId,raw,source:{origin:location.origin,pathname:location.pathname,storageKey:STORE}});if(attached.backup?.restoreTest!=='PASS'||attached.backup.encrypted!==true)throw Error('restore_backup_not_verified');enabled=attached.enabled===true&&attached.authorized===true;status=enabled?'设备会话已安全恢复':'已恢复连接 · 服务暂停';return true;})().finally(()=>{reconnecting=null;});return reconnecting;}
 async function request(path,body){try{return await rawRequest(path,body);}catch(e){if(e.message==='unauthorized'&&!path.startsWith('/v1/session/')&&await restoreDevice())return rawRequest(path,body);throw e;}}
 async function applyIncoming(remote) {
  if(window.FocusGrowthTimelineGesture?.isActive?.()){status='等待时间轴拖动结束';return false}
  for(let attempt=0;attempt<2;attempt++){
   assertFresh();const raw=JSON.stringify(db),local=project(),result=merge(meta.base,local,remote);
   if(result.issues.length){conflicts=result.issues;status='冲突';return false;}
   if(sameData(local,result.state)){meta.base=copy(remote);persist();return true;}
   const running=db.taskTimer?.taskId;
   if(running&&result.state.tasks.find(t=>t.id===running)?.done){conflicts=['tasks/'+running+'/active_timer_preservation'];status='冲突';error='远端完成涉及运行计时，请先暂停计时并复核；本地计时完整保留。';return false;}
   browserBackup();await checkpoint(raw);assertFresh();if(raw!==JSON.stringify(db))continue;
   const next=copy(db);for(const col of Object.keys(fields)){const old=new Map(db[col].map(row=>[row.id,row]));next[col]=result.state[col].map(row=>({...old.get(row.id),...row}));}
   if(typeof reconcileIncomingCompletions==='function')reconcileIncomingCompletions(db,next);
   if(validate(next))throw Error('incoming_validation_failed');
   if(next.taskTimer&&!next.tasks.some(t=>t.id===next.taskTimer.taskId))throw Error('active_timer_preservation');
   applying=true;try{window.FocusGrowthUndo?.invalidate?.();db=next;render();}finally{applying=false;}meta.base=copy(remote);persist();return true;
  }
  status='等待本地编辑稳定';error='变化发生在备份期间，未覆盖本地，稍后重试。';return false;
 }
 function queue(){
  if(!TEST&&!meta.bound){meta.staged=project();persist();return;}
  if(meta.pending){const latest=project();if(sameData(latest,meta.pending.desired))meta.next=null;else meta.next={base:copy(meta.pending.desired),desired:latest,operationId:meta.next?.operationId||crypto.randomUUID()};persist();return;}
  const desired=project();if(sameData(desired,meta.base))return;
  meta.pending={base:copy(meta.base),desired,operationId:meta.next?.operationId||crypto.randomUUID()};meta.next=null;persist();
 }
 async function sync(manual=false){
  if(window.FocusGrowthTimelineGesture?.isActive?.())return;
  if(!session||busy||stale||metaCorrupt||(!manual&&Date.now()<nextAt))return;
  if(!enabled){if(!TEST)await fgSyncReadOnly();return;}const startedGeneration=generation;
  busy=true;status='同步中';error='';show();
  try{
   let response=await request('/v1/sync',{manual});if(startedGeneration!==generation)return;if(!response.enabled){enabled=false;status='已暂停';return;}latestRemote=copy(response.state);serverPendingOperation=response.pendingOperationId||null;
   if(!meta.pending&&!response.pending){if(!await applyIncoming(response.state))return;queue();}
   if(meta.pending){if(meta.resolutionPrevious){response=await request('/v1/resolve',{...meta.pending,clientId,previousOperationId:meta.resolutionPrevious});delete meta.resolutionPrevious;persist();}else response=await request('/v1/changes',{...meta.pending,clientId});}
   if(startedGeneration!==generation)return;latestRemote=copy(response.state);serverPendingOperation=response.pendingOperationId||null;
   status=response.status;last=response.lastSuccess||last;error=response.error||'';conflicts=response.conflicts||[];
   if(status==='已同步'&&!response.pending){if(meta.pending)meta.base=copy(meta.pending.desired);meta.pending=null;persist();const applied=await applyIncoming(response.state);if(!applied)return;queue();if(applied){failures=0;nextAt=Date.now()+(meta.pending?1500:5000);if(meta.pending){clearTimeout(timer);timer=setTimeout(()=>sync(),1500);}}}
   else nextAt=Date.now()+Math.min(300000,5000*2**Math.min(++failures,6));
  }catch(e){if(status!=='等待授权')status='离线';error=e.message==='Failed to fetch'?'无法连接本地服务。检查程序、配对及浏览器本地网络权限；也可使用本地受信网页。':e.message;nextAt=Date.now()+Math.min(300000,5000*2**Math.min(++failures,6));}
  finally{busy=false;show();}
 }
 function dirty(){if(applying)return;try{assertFresh();queue();status=enabled?'同步中':session?'已暂停 · 本地更改已暂存':'未连接 · 本地更改已暂存';show();if(enabled){clearTimeout(timer);timer=setTimeout(()=>sync(),1500);}}catch(e){enabled=false;error=e.message;status='本地持久化失败或多标签冲突';show();}}
 
 window.FocusGrowthUndoCheck=(current,next)=>{
  for(const col of Object.keys(fields)){
   const after=new Set((next[col]||[]).map(r=>r.id));
   const removed=(current[col]||[]).filter(r=>!after.has(r.id));
   if(!removed.length)continue;
   const known=new Set([...(meta.base[col]||[]),...(latestRemote?.[col]||[])].map(r=>r.id));
   if(removed.some(row=>known.has(row.id))||busy||meta.pending){
    return '撤销会删除已同步或正在同步的项目、任务或排程，受正式同步的禁止自动删除规则保护。此操作未执行';
   }
  }
  return null;
 };
window.FocusGrowthNotifyTaskChange=dirty;
 function track(fn){return function(...args){const before=stable(project());const result=fn.apply(this,args);if(before!==stable(project()))dirty();return result;};}
 for(const name of ['saveTask','saveAssignment','completeTask','confirmPlanExecution','beginTaskTimer','pauseTaskTimer','stopTaskTimer','saveRecoveredTimer','deleteTask','savePlan','deletePlan','dropPlanAsActual','dropTask','saveProject','importText','resetData']){
  // These legacy functions are explicitly global declarations in the single-file UI.
  if(typeof window[name]==='function')window[name]=track(window[name]);
 }
 let resizing=false;const oldResize=window.resizePlan;window.resizePlan=function(...args){resizing=true;return oldResize.apply(this,args);};
 window.addEventListener('pointerup',()=>{if(resizing){resizing=false;setTimeout(dirty,0);}});
 window.fgSyncReadOnly=async()=>{if(!session||stale||metaCorrupt)return;try{const response=await request('/v1/state');if(response.error)throw Error(response.error);latestRemote=copy(response.state);const matches=sameData(project(),response.state);if(matches&&!meta.pending){meta.base=copy(response.state);meta.bound=true;persist();}status='只读核验：'+(matches?'与本地一致':'存在差异，未覆盖本地');error='revision='+response.state.revision+'；正式授权='+response.authorized+'；服务启用='+response.enabled+'；数量='+JSON.stringify(Object.fromEntries(Object.keys(fields).map(k=>[k,response.state[k].length])));show();}catch(e){status='离线';error=e.message;show();}};
 window.fgSyncEnable=async()=>{if(!session)return toast('先配对本地服务。');if(!TEST&&!meta.bound)return toast('先完成只读基准对齐；存在差异时先做备份和预览。');try{browserBackup();await checkpoint(JSON.stringify(db));const result=await request('/v1/enable',{clientId});if(result.enabled!==true||!TEST&&result.authorized!==true)throw Error('continuous_authorization_required');generation++;enabled=true;await sync(true);}catch(e){enabled=false;status='等待授权或启用失败';error=e.message;show();}};
 window.fgSyncPause=async()=>{generation++;enabled=false;clearTimeout(timer);try{if(session)await request('/v1/pause',{});status='已暂停 · 队列保留';error='';}catch(e){status='网页已暂停；请在Windows确认服务暂停';error=e.message;}show();};
 window.fgSyncPair=async()=>{if(metaCorrupt||stale)return toast('先处理元数据或多标签冲突。');status='等待授权';error='';show();try{const result=await request('/v1/pair',{code:document.getElementById('fg-pair-code').value.trim()});if(result.version!=='0.4'||result.mode!==(TEST?'synthetic':'production'))throw Error('native_web_mode_or_build_mismatch');session=result.session;if(window.FocusGrowthDeviceSession)await FocusGrowthDeviceSession.enroll(rawRequest);document.getElementById('fg-pair-code').value='';const raw=browserBackup();const attached=await request('/v1/attach',{clientId,raw,source:{origin:location.origin,pathname:location.pathname,storageKey:STORE}});if(attached.backup?.restoreTest!=='PASS'||attached.backup.encrypted!==true)throw Error('attach_backup_not_verified');if(TEST)await fgSyncEnable();else await fgSyncReadOnly();}catch(e){enabled=false;status='等待授权';error=e.message;show();}};
 window.fgSyncTestNetwork=async offline=>{if(!TEST||!session)return;await request('/v1/test/network',{offline});nextAt=0;await sync(true);};
 window.fgSyncManual=()=>sync(true);
 window.fgSyncConflictPanel=()=>{
  if(!latestRemote||!conflicts.length)return;
  conflictContext={local:project(),remote:copy(latestRemote),paths:copy(conflicts)};
  modal('逐字段解决同步冲突','<div class="notice">请核对每个字段后选择保留哪一端。不会自动选择；删除仍被禁用。旧操作会保留在本地备份中。</div><div id="fg-conflict-fields"></div><div class="actions"><button onclick="fgSyncCommitResolution()">确认所选字段并重试</button></div>');
  const holder=document.getElementById('fg-conflict-fields');
  conflicts.forEach((path,i)=>{const row=document.createElement('div');row.className='field';const label=document.createElement('label');label.textContent=path;row.append(label);const [col,id,field]=path.split('/');const values=document.createElement('pre');values.style.whiteSpace='pre-wrap';values.textContent='本地：'+JSON.stringify(conflictContext.local[col].find(r=>r.id===id)?.[field])+'\n远端：'+JSON.stringify(conflictContext.remote[col].find(r=>r.id===id)?.[field]);row.append(values);const select=document.createElement('select');select.id='fg-conflict-'+i;for(const [value,text] of [['','请选择'],['local','保留本地'],['remote','保留远端']]){const option=document.createElement('option');option.value=value;option.textContent=text;select.append(option);}if(path.endsWith('/deletion_disabled')||path.endsWith('/active_timer_preservation')){select.disabled=true;const message=document.createElement('p');message.textContent='删除传播未启用；请先恢复缺失记录或保留两端备份后人工处理。';row.append(message);}row.append(select);holder.append(row);});
 };
 window.fgSyncCommitResolution=async()=>{
  if(!session||!enabled||!latestRemote||!meta.pending||!conflictContext)return;
  if(stable(project())!==stable(conflictContext.local)||stable(latestRemote)!==stable(conflictContext.remote))return toast('数据已更新，请重新打开冲突面板核对。');
  const local=project(),merged=merge(meta.pending.base,local,latestRemote);
  if(conflicts.some(x=>x.endsWith('/active_timer_preservation'))||merged.issues.some(x=>x.endsWith('/deletion_disabled')))return toast('删除传播仍被禁用，请恢复缺失记录后重试。');
  for(let i=0;i<merged.issues.length;i++){
   const selection=document.getElementById('fg-conflict-'+i)?.value;if(!['local','remote'].includes(selection))return toast('请明确选择每一个冲突字段。');
   const [col,id,field]=merged.issues[i].split('/'),row=merged.state[col].find(r=>r.id===id),source=(selection==='local'?local:latestRemote)[col].find(r=>r.id===id);
   if(source&&Object.hasOwn(source,field))row[field]=copy(source[field]);else delete row[field];
  }
  if(!confirm('确认按所选字段提交合并结果？'))return;
  // Apply only whitelisted collections while preserving local fields and timers.
  const before=copy(db),next=copy(db);for(const col of Object.keys(fields)){const old=new Map(db[col].map(r=>[r.id,r]));next[col]=merged.state[col].map(r=>({...old.get(r.id),...r}));}
  if(validate(next))return toast('合并结果未通过验证，未应用。');
  browserBackup();await checkpoint(JSON.stringify(db));assertFresh();if(stable(project())!==stable(conflictContext.local)||stable(latestRemote)!==stable(conflictContext.remote))return toast('备份期间内容改变，请重新复核冲突。');
  meta.resolutionPrevious=serverPendingOperation||meta.resolutionPrevious||meta.pending.operationId;meta.pending={base:copy(latestRemote),desired:merged.state,operationId:crypto.randomUUID()};persist();
  applying=true;try{db=next;render();}catch{db=before;throw Error('resolution_local_apply_failed');}finally{applying=false;}
  closeModal();await sync(true);
 };
 window.fgSyncDisable=async()=>{await fgSyncPause();if(window.FocusGrowthDeviceSession){await request('/v1/session/revoke',{});await FocusGrowthDeviceSession.forget();}session='';status='已解除配对 · 本地数据及队列保留';show();};
 window.fgSyncRevokeAuthorization=async()=>{await request('/v1/authorization/revoke',{});enabled=false;generation++;status='正式持续授权已撤销 · 数据与队列保留';show();};
 window.fgSyncSeed=()=>{if(!TEST)return;if(!confirm('仅添加清晰标记的隔离虚拟项目、任务和排程，继续？'))return;const id='v04-'+crypto.randomUUID();db.projects.push({id:'p-'+id,name:'SYNTHETIC V04 PROJECT',deadline:'',priority:'',weeklyMin:0});db.tasks.push({id,projectId:'p-'+id,parentId:null,name:'SYNTHETIC V04 TASK',estimate:30,date:today(),goal:'normal',category:'synthetic',priority:'',done:false,actual:null,note:'LOCAL ONLY CANARY',timerMs:1200});db.plans.push({id:'plan-'+id,taskId:id,date:today(),start:900,duration:30});render();dirty();};
 window.fgSyncPanel=()=>{modal('V0.4 Windows 持续同步',  '<div class="notice">'+(TEST?'隔离虚拟工作区：允许测试自动双向、断网恢复及冲突。':'正式工作区：默认只读。启用真实持续写入前必须另一次明确授权，并完成独立原生授权流程。')+'</div>'+  '<label for="fg-pair-code">Windows一次性配对码（不是GitHub Token）</label><input id="fg-pair-code" type="password" autocomplete="off">'+  '<div class="headrow sectiongap"><button onclick="fgSyncPair()">配对V0.4服务</button><button onclick="fgSyncEnable()">启用 / 继续</button><button onclick="fgSyncPause()">暂停</button><button onclick="fgSyncManual()">手动同步</button><button onclick="fgSyncReadOnly()">只读一致性核验</button><button onclick="fgSyncConflictPanel()">解决冲突</button><button onclick="fgSyncDisable()">解除配对</button><button onclick="fgSyncRevokeAuthorization()">撤销正式写入授权</button></div>'+  (TEST?'<div><button onclick="fgSyncSeed()">添加V0.4虚拟样本</button><button onclick="fgSyncTestNetwork(true)">模拟断网</button><button onclick="fgSyncTestNetwork(false)">恢复网络</button></div>':'<p>配对不会启用正式写入。没有原生持续授权时启用请求将被拒绝；网页只读核验不会改本地任务。</p>')+  '<pre id="fg-sync-info" style="white-space:pre-wrap;overflow-wrap:anywhere"></pre><p class="muted">设备与正式同步授权持续有效，直至主动撤销；内部令牌短期有效并安全自动更新。刷新与重启自动恢复，保留上次启用或暂停状态。离线队列和本地私有字段保留；同字段冲突必须选择，删除不传播，运行计时器相关完成变更会先阻断。首次对齐或多标签不一致时停止自动应用。GitHub Token始终由原生程序管理。</p>');show();};
 const tools=document.querySelector('.tools');if(tools){const button=document.createElement('button');button.id='fg-sync-status';button.className='small';button.onclick=window.fgSyncPanel;tools.append(button);}show();
 const oldOpenSync=window.openSync;window.openSync=function(){oldOpenSync();const panel=document.querySelector('#modalRoot .modal')||document.querySelector('.modal');if(panel){const b=document.createElement('button');b.textContent='Windows 私有同步';b.onclick=window.fgSyncPanel;panel.append(b);}};
 const oldSettings=window.renderSettings;window.renderSettings=function(){oldSettings();const b=document.createElement('button');b.textContent='Windows 持续同步 V0.4';b.onclick=window.fgSyncPanel;document.getElementById('settings').prepend(b);};

 // Activity telemetry is strictly local. Never sends keyboard content, activity or states to GitHub.
 let activityPolling=false,activityArmedFor=0,activityRouteUnavailable=false;
 async function desktopActivityPoll(){
  if(activityPolling||!session||(!validWorkSession(db.workSession)&&!db.taskTimer)||activityRouteUnavailable)return;
  activityPolling=true;
  try{
   let response=await request('/v1/activity');
   if(!response?.supported||!Array.isArray(response.slots))throw Error('unsupported_activity_response');
   const trackingStart=validWorkSession(db.workSession)?db.workSession.startedAt:db.taskTimer.startedAt;
   if(!response.armed||activityArmedFor!==trackingStart){
    response=await request('/v1/activity/arm',{});
    activityArmedFor=trackingStart;
   }
   if(!Array.isArray(response.slots))throw Error('unsupported_activity_response');
   const sessionStart=trackingStart,next=copy(db);let added=0;
   for(const evidence of response.slots){
    const d=evidence.date,slot=evidence.slot;
    if(typeof d!=='string'||!/^\d{4}-\d{2}-\d{2}$/.test(d)||!Number.isInteger(slot)||slot<0||slot>143)continue;
    const h=Math.floor(slot/6),m=slot%6*10;
    const slotStart=new Date(d+'T'+String(h).padStart(2,'0')+':'+String(m).padStart(2,'0')+':00').getTime();
    if(!Number.isFinite(slotStart)||slotStart+600000<=sessionStart||slotStart>Date.now()+60000)continue;
    if(next.activity.some(a=>a.date===d&&activityStart(a)<(slot+1)*10&&activityEnd(a)>slot*10))continue;
    next.activity.push({date:d,slot,state:db.state||'unknown',taskId:db.taskTimer?.taskId||null,source:'desktop-input',...(validWorkSession(db.workSession)?{sessionStartedAt:db.workSession.startedAt}:{})});
    added++;
   }
   if(added){
    // Never replace the lane DOM while an actual or planned block is being dragged.
    // The native evidence stays available to fetch on the next poll.
    if(window.FocusGrowthTimelineGesture?.isActive?.())return;
    assertFresh();const error=validate(next);if(error)throw Error('activity_validation_'+error);
    db.activity=next.activity;render(); // Activity is browser-only and outside sync projection.
   }
  }catch(e){
   if(e.message==='route_rejected'||e.message==='activity_unavailable'||e.message==='invalid_service_response')activityRouteUnavailable=true;
  }finally{activityPolling=false;}
 }
 async function desktopActivityArm(){activityArmedFor=0;await desktopActivityPoll()}
 async function desktopActivityStop(){
  if(!session||activityRouteUnavailable)return;
  try{await desktopActivityPoll();await request('/v1/activity/stop',{});activityArmedFor=0;}catch{}
 }
 window.FocusGrowthDesktopActivity={arm:desktopActivityArm,stop:desktopActivityStop,poll:desktopActivityPoll};
 setInterval(()=>{if(validWorkSession(db.workSession)||db.taskTimer)desktopActivityPoll()},5000);
 document.addEventListener('visibilitychange',()=>{if(document.visibilityState==='visible'&&(validWorkSession(db.workSession)||db.taskTimer))desktopActivityPoll()});
 window.addEventListener('online',()=>sync());document.addEventListener('visibilitychange',()=>{if(document.visibilityState==='visible')sync();});
 setInterval(()=>{if(document.visibilityState!=='visible')return;if(session)sync();else if(window.FocusGrowthDeviceSession&&!reconnecting&&Date.now()>=nextAt){restoreDevice().then(ok=>{if(ok)sync(true);}).catch(e=>{nextAt=Date.now()+30000;error=e.message;show();});}},5000);
 // Test seam exposes pure transforms only, never session or credentials.
 window.FocusGrowthSyncTransforms={project,merge};
if(window.FocusGrowthDeviceSession){FocusGrowthDeviceSession.listen(rawRequest,e=>{enabled=false;generation++;error=e.message;status='设备会话需重新认证';show();});restoreDevice().then(ok=>{if(ok)sync(true);}).catch(e=>{error=e.message;status='会话恢复未完成，请检查本地服务或重新配对';show();});}
})();
