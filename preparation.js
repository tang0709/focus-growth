/* V0.3.1: normal workspace preparation only. No production-write operation exists here. */
(() => {
 'use strict';
 if(new URLSearchParams(location.search).get('syncTest')==='1')return;
 const VERSION='0.3.1-preparation', API='http://127.0.0.1:18418', KEY='fg-production-preparation-queue-v031';
 let NOTICE='当前面板仅只读；正式远程状态以核验结果为准。';
 let session='',enabled=false,status='正式同步准备 · 未配对',summary=null,previewFingerprint=null;
 const persistStage=()=>{if(!enabled)return;const projected=window.FocusGrowthSyncTransforms.project();localStorage.setItem(KEY,JSON.stringify({version:VERSION,stagedOnly:true,latest:projected,updatedAt:new Date().toISOString()}));if(previewFingerprint&&previewFingerprint!==JSON.stringify(projected)){status='本地内容已变更：预览已过期，必须重新备份和生成预览';show();}};
 function show(){const x=document.getElementById('fg-prep-info');if(x)x.textContent=status+'\n'+NOTICE+(summary?'\n结论：'+summary.conclusion+'\n预览摘要：'+summary.previewDigest+'\n数量：'+JSON.stringify(summary.totals):'');}
 async function request(path,body){const c=new AbortController(),timeout=setTimeout(()=>c.abort(),70000);try{const r=await fetch(API+path,{method:body===undefined?'GET':'POST',headers:{'X-FG-Client':'1',...(session?{Authorization:'Bearer '+session}:{}),...(body===undefined?{}:{'Content-Type':'application/json'})},...(body===undefined?{}:{body:JSON.stringify(body)}),cache:'no-store',redirect:'error',credentials:'omit',signal:c.signal});const result=await r.json();if(!r.ok)throw Error(result.error||'preparation_failed');return result;}finally{clearTimeout(timeout);}}
 window.fgPreparePair=async()=>{try{const result=await request('/v1/pair',{code:document.getElementById('fg-prep-code').value.trim()});if(result.mode!=='preparation'||result.version!==VERSION)throw Error('native_web_build_mismatch');session=result.session;enabled=true;document.getElementById('fg-prep-code').value='';status='已配对 · 正式写入锁定';persistStage();show();}catch(e){session='';enabled=false;status='准备服务未连接：'+e.message;show();}};
 window.fgPreparePreview=async()=>{
  if(!session)return toast('先配对本机准备服务。');
  if(location.origin!=='https://tang0709.github.io'||location.pathname!=='/focus-growth/')return toast('来源不符：请在正式Pages工作区备份，不要把本地空白页当正式数据。');
  const raw=localStorage.getItem('start-workbench-v1');if(raw===null)return toast('未找到正式存储键，已停止；未将缺失数据当空工作区。');
  let parsed;try{parsed=JSON.parse(raw);}catch{return toast('正式本地数据不能解析，已停止，原数据保留。');}
  if(parsed.version!==1||parsed.demo===true)return toast('数据版本或演示标记不符合正式来源，已停止。');
  const reason=validate(parsed);if(reason)return toast('本地校验未通过：'+reason+'；原数据保留。');
  let profileId=localStorage.getItem('fg-production-source-profile-v031');if(!profileId){profileId=crypto.randomUUID();localStorage.setItem('fg-production-source-profile-v031',profileId);}
  const sourceFingerprint=JSON.stringify(window.FocusGrowthSyncTransforms.project());
  status='正在本机加密备份、恢复校验及只读比较…';show();
  try{summary=await request('/v1/preparation/preview',{requestId:crypto.randomUUID(),source:{origin:location.origin,pathname:location.pathname,storageKey:'start-workbench-v1',present:true,synthetic:false,profileId},raw});previewFingerprint=sourceFingerprint;status=sourceFingerprint===JSON.stringify(window.FocusGrowthSyncTransforms.project())?'本机预览已生成 · 正式上传仍锁定':'预览生成期间本地内容已变更，必须重新生成预览';show();}
  catch(e){status='预览未完成：'+e.message;show();}
 };
 function comparable(state){const clean={};for(const col of ['projects','tasks','plans'])clean[col]=state[col].map(row=>{const r={...row};if(col==='tasks'){r.date=r.date||'';r.projectId=r.projectId||null;r.parentId=r.parentId||null;if(r.completedAt==null)delete r.completedAt;}return r;}).sort((a,b)=>a.id<b.id?-1:a.id>b.id?1:0);const ordered=x=>x&&typeof x==='object'?Array.isArray(x)?x.map(ordered):Object.fromEntries(Object.keys(x).sort().map(k=>[k,ordered(x[k])])):x;return JSON.stringify(ordered(clean));}
 window.fgPrepareVerifyRead=async()=>{if(!session)return toast('先配对本机只读服务。');try{const result=await request('/v1/preparation/state');if(result.version!==VERSION||!result.state||result.state.schemaVersion!==1)throw Error('readback_build_or_schema_mismatch');const same=comparable(result.state)===comparable(window.FocusGrowthSyncTransforms.project());NOTICE=result.notice;summary=null;status='远程只读核验：'+(same?'与本地允许字段完全一致':'与本地不一致；未覆盖任一端')+'\ninitialized='+result.initialized+'；revision='+result.remoteRevision+'\nSHA='+result.remoteSha+'\n数量：'+JSON.stringify(Object.fromEntries(['projects','tasks','plans'].map(k=>[k,result.state[k].length])))+'\n持续双向同步：未启用';show();}catch(e){status='远程只读核验失败：'+e.message;show();}};
 window.fgPreparePanel=()=>{modal('V0.3.1 正式同步准备与核验（只读）',`<div class="notice">${NOTICE}</div><p>仅允许本机加密备份、恢复校验、差异预览与远程只读核验。首次写入由绑定快照的一次性原生入口处理；配对不授予持续写入权限。</p><label for="fg-prep-code">Windows准备窗口的一次性配对码（不是GitHub Token）</label><input id="fg-prep-code" type="password" autocomplete="off" style="width:100%"><div class="headrow sectiongap"><button onclick="fgPreparePair()">配对只读准备服务</button><button onclick="fgPreparePreview()">备份并生成本机差异预览</button><button onclick="fgPrepareVerifyRead()">读取远程并核验（不改本地）</button><button disabled>正式写入：独立一次性授权</button></div><pre id="fg-prep-info" style="white-space:pre-wrap;overflow-wrap:anywhere"></pre><p class="muted">远程核验只比较允许字段，不导入或改写任务，不接收GitHub Token。空远程不会清空本地数据；不自动改期或完成任务。浏览器访问受限时不关闭安全机制。</p>`);show();};
 const tools=document.querySelector('.tools');if(tools){const b=document.createElement('button');b.className='small';b.textContent='正式同步准备 · 只读';b.onclick=window.fgPreparePanel;tools.append(b);}
 for(const name of ['saveTask','saveAssignment','completeTask','confirmPlanExecution','beginTaskTimer','pauseTaskTimer','stopTaskTimer','saveRecoveredTimer','deleteTask','savePlan','deletePlan','dropPlanAsActual','dropTask','saveProject','importText','resetData']){const old=window[name];if(typeof old==='function')window[name]=function(...args){const result=old.apply(this,args);persistStage();return result;};}
 window.addEventListener('pointerup',persistStage);
})();
