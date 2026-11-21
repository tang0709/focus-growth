/* Focus & Growth 0.3: synthetic-only bridge. GitHub credentials never enter this file. */
(() => {
 'use strict';
 const TEST = new URLSearchParams(location.search).get('syncTest') === '1';
 const API = 'http://127.0.0.1:18417';
 const KEY = 'fg-sync-test-meta-v1';
 const fields = {
  projects: ['id','name','deadline','priority','weeklyMin'],
  tasks: ['id','projectId','parentId','name','estimate','date','goal','category','priority','done','actual','completedAt'],
  plans: ['id','taskId','date','start','duration']
 };
 const empty = () => ({schemaVersion:1, revision:0, initialized:false, updatedAt:null, operationIds:[], projects:[],tasks:[],plans:[]});
 const copy = x => JSON.parse(JSON.stringify(x));
 const stable = x => JSON.stringify(x && typeof x === 'object' ? Array.isArray(x) ? x.map(v=>JSON.parse(stable(v))) : Object.fromEntries(Object.keys(x).sort().map(k=>[k,JSON.parse(stable(x[k]))])) : x??null);
 let session = '', enabled = false, applying = false, busy = false, timer, status = '未连接', error = '', last = '', conflicts = [], failures = 0, nextAt = 0;
 let meta = {base:empty(),pending:null};
 let latestRemote=null,conflictContext=null,serverPendingOperation=null;
 if(TEST) {try {const old=localStorage.getItem(KEY);if(old)meta=JSON.parse(old);} catch {status='冲突';error='本地同步元数据损坏，已保留；请导出备份后检查。';}}
 const persist = () => {if(TEST)localStorage.setItem(KEY,JSON.stringify(meta));};
 function project() {
  const state=empty();
  for(const k of Object.keys(fields))state[k]=db[k].map(row=>{
   const safe=Object.fromEntries(fields[k].filter(f=>Object.hasOwn(row,f)).map(f=>[f,copy(row[f])]));
   if(k==='tasks'){safe.projectId=safe.projectId||null;safe.parentId=safe.parentId||null;safe.date=safe.date||null;safe.category=safe.category||'';safe.priority=safe.priority||'';safe.goal=safe.goal||'normal';safe.actual=safe.actual??null;}
   if(k==='projects'){safe.deadline=safe.deadline||'';safe.priority=safe.priority||'';safe.weeklyMin=safe.weeklyMin||0;}
   return safe;
  }).sort((a,b)=>a.id.localeCompare(b.id));
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
 function show(){const badge=document.getElementById('fg-sync-status');if(badge)badge.textContent=`${TEST?'虚拟同步':'同步'} · ${status}`;const info=document.getElementById('fg-sync-info');if(info)info.textContent=`${status}${error?' · '+error:''}\n最近成功：${last||'尚无'}${conflicts.length?'\n冲突字段：'+conflicts.join('、'):''}`;}
 async function request(path,body) {
  const controller=new AbortController(), timeout=setTimeout(()=>controller.abort(),70000);
  try {const response=await fetch(API+path,{method:body===undefined?'GET':'POST',headers:{'X-FG-Client':'1',...(body===undefined?{}:{'Content-Type':'application/json'}),...(session?{Authorization:'Bearer '+session}:{})},...(body===undefined?{}:{body:JSON.stringify(body)}),signal:controller.signal,cache:'no-store',credentials:'omit',redirect:'error'});
   let result;try{result=await response.json();}catch{throw Error('invalid_service_response');}
   if(!response.ok){if(response.status===401){session='';enabled=false;status='等待授权';}throw Error(result.error||'service_error');}return result;
  }finally{clearTimeout(timeout);}
 }
 function applyIncoming(remote) {
  const local=project();const result=merge(meta.base,local,remote);
  if(result.issues.length){conflicts=result.issues;status='冲突';return false;}
  const next=copy(db);
  for(const col of Object.keys(fields)){
   const old=new Map(db[col].map(row=>[row.id,row]));
   // Keep local-only task notes, timerMs and all unrelated collections.
   next[col]=result.state[col].map(row=>({...old.get(row.id),...row}));
  }
  const reason=validate(next);if(reason)throw Error('incoming_validation_failed');
  if(next.taskTimer&&!next.tasks.some(t=>t.id===next.taskTimer.taskId))throw Error('active_timer_preservation');
  applying=true;try{db=next;render();}finally{applying=false;}
  meta.base=copy(remote);persist();return true;
 }
 function queue(){
  if(meta.pending){const latest=project();if(Object.keys(fields).every(k=>stable(latest[k])===stable(meta.pending.desired[k])))meta.next=null;else meta.next={base:copy(meta.pending.desired),desired:latest,operationId:meta.next?.operationId||crypto.randomUUID()};persist();return;}
  const desired=project();if(Object.keys(fields).every(k=>stable(desired[k])===stable(meta.base[k])))return;
  meta.pending={base:copy(meta.base),desired,operationId:meta.next?.operationId||crypto.randomUUID()};meta.next=null;persist();
 }
 async function sync(manual=false){
  if(!TEST||!enabled||!session||busy||(!manual&&Date.now()<nextAt))return;
  busy=true;status='同步中';error='';show();
  try{
   let response=await request('/v1/sync',{});latestRemote=copy(response.state);serverPendingOperation=response.pendingOperationId||null;
   if(!meta.pending&&!response.pending){applyIncoming(response.state);queue();}
   if(meta.pending){if(meta.resolutionPrevious){response=await request('/v1/resolve',{...meta.pending,previousOperationId:meta.resolutionPrevious});delete meta.resolutionPrevious;persist();}else response=await request('/v1/changes',meta.pending);}
   latestRemote=copy(response.state);serverPendingOperation=response.pendingOperationId||null;
   status=response.status;last=response.lastSuccess||last;error=response.error||'';conflicts=response.conflicts||[];
   if(status==='已同步'&&!response.pending){if(meta.pending)meta.base=copy(meta.pending.desired);meta.pending=null;persist();const applied=applyIncoming(response.state);queue();if(applied){failures=0;nextAt=Date.now()+(meta.pending?1500:30000);if(meta.pending){clearTimeout(timer);timer=setTimeout(()=>sync(),1500);}}}
   else nextAt=Date.now()+Math.min(300000,5000*2**Math.min(++failures,6));
  }catch(e){if(status!=='等待授权')status='离线';error=e.message==='Failed to fetch'?'无法连接本地服务。检查程序、配对及浏览器本地网络权限；也可使用本地受信网页。':e.message;nextAt=Date.now()+Math.min(300000,5000*2**Math.min(++failures,6));}
  finally{busy=false;show();}
 }
 function dirty(){if(!TEST||applying)return;queue();status=session?'同步中':'未连接';show();if(enabled){clearTimeout(timer);timer=setTimeout(()=>sync(),1500);}}
 function track(fn){return function(...args){const before=stable(project());const result=fn.apply(this,args);if(before!==stable(project()))dirty();return result;};}
 for(const name of ['saveTask','completeTask','deleteTask','savePlan','deletePlan','dropTask','saveProject','importText','resetData']){
  // These legacy functions are explicitly global declarations in the single-file UI.
  if(typeof window[name]==='function')window[name]=track(window[name]);
 }
 let resizing=false;const oldResize=window.resizePlan;window.resizePlan=function(...args){resizing=true;return oldResize.apply(this,args);};
 window.addEventListener('pointerup',()=>{if(resizing){resizing=false;setTimeout(dirty,0);}});
 window.fgSyncPair=async()=>{
  if(!TEST)return;
  status='等待授权';error='';show();
  try {const code=document.getElementById('fg-pair-code').value.trim();const result=await request('/v1/pair',{code});session=result.session;enabled=true;document.getElementById('fg-pair-code').value='';
   // A full local backup is mandatory even for this isolated synthetic workspace.
   localStorage.setItem('fg-sync-test-backup-'+Date.now(),JSON.stringify(db));
   await sync(true);
  }catch(e){status='等待授权';error=e.message;show();}
 };
 window.fgSyncManual=()=>sync(true);
 window.fgSyncConflictPanel=()=>{
  if(!TEST||!latestRemote||!conflicts.length)return;
  conflictContext={local:project(),remote:copy(latestRemote),paths:copy(conflicts)};
  modal('逐字段解决同步冲突','<div class="notice">请核对每个字段后选择保留哪一端。不会自动选择；删除仍被禁用。旧操作会保留在本地备份中。</div><div id="fg-conflict-fields"></div><div class="actions"><button onclick="fgSyncCommitResolution()">确认所选字段并重试</button></div>');
  const holder=document.getElementById('fg-conflict-fields');
  conflicts.forEach((path,i)=>{const row=document.createElement('div');row.className='field';const label=document.createElement('label');label.textContent=path;row.append(label);const [col,id,field]=path.split('/');const values=document.createElement('pre');values.style.whiteSpace='pre-wrap';values.textContent='本地：'+JSON.stringify(conflictContext.local[col].find(r=>r.id===id)?.[field])+'\n远端：'+JSON.stringify(conflictContext.remote[col].find(r=>r.id===id)?.[field]);row.append(values);const select=document.createElement('select');select.id='fg-conflict-'+i;for(const [value,text] of [['','请选择'],['local','保留本地'],['remote','保留远端']]){const option=document.createElement('option');option.value=value;option.textContent=text;select.append(option);}if(path.endsWith('/deletion_disabled')){select.disabled=true;const message=document.createElement('p');message.textContent='删除传播未启用；请先恢复缺失记录或保留两端备份后人工处理。';row.append(message);}row.append(select);holder.append(row);});
 };
 window.fgSyncCommitResolution=async()=>{
  if(!TEST||!session||!latestRemote||!meta.pending||!conflictContext)return;
  if(stable(project())!==stable(conflictContext.local)||stable(latestRemote)!==stable(conflictContext.remote))return toast('数据已更新，请重新打开冲突面板核对。');
  const local=project(),merged=merge(meta.pending.base,local,latestRemote);
  if(merged.issues.some(x=>x.endsWith('/deletion_disabled')))return toast('删除传播仍被禁用，请恢复缺失记录后重试。');
  for(let i=0;i<merged.issues.length;i++){
   const selection=document.getElementById('fg-conflict-'+i)?.value;if(!['local','remote'].includes(selection))return toast('请明确选择每一个冲突字段。');
   const [col,id,field]=merged.issues[i].split('/'),row=merged.state[col].find(r=>r.id===id),source=(selection==='local'?local:latestRemote)[col].find(r=>r.id===id);
   if(source&&Object.hasOwn(source,field))row[field]=copy(source[field]);else delete row[field];
  }
  if(!confirm('确认按所选字段提交合并结果？'))return;
  // Apply only whitelisted collections while preserving local fields and timers.
  const before=copy(db),next=copy(db);for(const col of Object.keys(fields)){const old=new Map(db[col].map(r=>[r.id,r]));next[col]=merged.state[col].map(r=>({...old.get(r.id),...r}));}
  if(validate(next))return toast('合并结果未通过验证，未应用。');
  localStorage.setItem('fg-sync-test-conflict-backup-'+Date.now(),JSON.stringify({db,pending:meta.pending}));
  meta.resolutionPrevious=serverPendingOperation||meta.resolutionPrevious||meta.pending.operationId;meta.pending={base:copy(latestRemote),desired:merged.state,operationId:crypto.randomUUID()};persist();
  applying=true;try{db=next;render();}catch{db=before;throw Error('resolution_local_apply_failed');}finally{applying=false;}
  closeModal();await sync(true);
 };
 window.fgSyncDisable=()=>{enabled=false;session='';status='未连接';clearTimeout(timer);show();};
 window.fgSyncSeed=()=>{if(!TEST)return;if(!confirm('仅向独立虚拟工作区添加示例任务，继续？'))return;const id='synthetic-'+crypto.randomUUID();db.tasks.push({id,name:'虚拟同步测试任务',projectId:null,parentId:null,date:today(),estimate:30,goal:'normal',category:'synthetic',priority:'',done:false,actual:null});render();dirty();};
 window.fgSyncPanel=()=>{
  modal('Windows 私有同步 · 虚拟数据测试',`<div class="notice">真实数据首次同步尚未授权，正式写入保持关闭。GitHub Token 只保存在 Windows 凭据管理器中。</div>${TEST?`<p>先启动本地程序，复制窗口中的一次性配对码，提交后在 Windows 窗口确认。配对有效期15分钟，刷新网页需重新配对。</p><div class="field"><label for="fg-pair-code">一次性配对码（不是 GitHub Token）</label><input id="fg-pair-code" type="password" autocomplete="off"></div><div class="headrow"><button onclick="fgSyncPair()">配对本地服务</button><button onclick="fgSyncManual()">手动同步</button><button onclick="fgSyncConflictPanel()">解决冲突</button><button onclick="fgSyncDisable()">断开</button><button onclick="fgSyncSeed()">添加虚拟任务</button></div>`:`<p>当前为原有本地数据工作区，同步未启用。</p><a href="?syncTest=1" target="_blank" rel="noopener">打开隔离的虚拟同步工作区</a>`}<pre id="fg-sync-info" style="white-space:pre-wrap;overflow-wrap:anywhere"></pre><p class="muted">离线更改保留在本地队列。同一字段并发修改会停止同步，删除不会自动传播；可逐字段选择冲突结果，删除仍被禁用；请保留两端备份。浏览器拒绝本地网络访问时，请使用 <a href="http://127.0.0.1:18417/?syncTest=1" target="_blank" rel="noopener">本地受信工作台</a>，不要关闭浏览器安全设置。</p>`);show();
 };
 const tools=document.querySelector('.tools');if(tools){const button=document.createElement('button');button.id='fg-sync-status';button.className='small';button.onclick=window.fgSyncPanel;tools.append(button);}show();
 const oldOpenSync=window.openSync;window.openSync=function(){oldOpenSync();const panel=document.querySelector('#modalRoot .modal')||document.querySelector('.modal');if(panel){const b=document.createElement('button');b.textContent='Windows 私有同步';b.onclick=window.fgSyncPanel;panel.append(b);}};
 const oldSettings=window.renderSettings;window.renderSettings=function(){oldSettings();const b=document.createElement('button');b.textContent='Windows 私有同步（虚拟测试）';b.onclick=window.fgSyncPanel;document.getElementById('settings').prepend(b);};
 window.addEventListener('online',()=>sync());document.addEventListener('visibilitychange',()=>{if(document.visibilityState==='visible')sync();});
 setInterval(()=>{if(enabled&&document.visibilityState==='visible')sync();},30000);
 // Test seam exposes pure transforms only, never session or credentials.
 window.FocusGrowthSyncTransforms={project,merge};
})();
