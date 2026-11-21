/* Browser device identity: a non-exportable signing key, never a GitHub PAT or persistent bearer. */
window.FocusGrowthDeviceSession=(()=>{
 'use strict';
 const test=new URLSearchParams(location.search).get('syncTest')==='1';
 let record=null,opening=null,active=false,lastTouch=0,working=false,info=null;
 let held=false,releaseLock=null,acquiring=null;
 async function acquire(){if(held)return;if(acquiring)return acquiring;if(!navigator.locks)throw Error('secure_device_lock_unavailable');acquiring=new Promise((resolve,reject)=>{navigator.locks.request('fg-sync-device-'+(test?'test':'production'),{ifAvailable:true},async lock=>{if(!lock){reject(Error('device_session_in_use_in_another_tab'));return;}held=true;resolve();await new Promise(done=>releaseLock=done);}).catch(reject);}).finally(()=>acquiring=null);return acquiring;}
 function release(){held=false;if(releaseLock)releaseLock();releaseLock=null;}
 window.addEventListener?.('pagehide',release);
 const b64=bytes=>btoa(String.fromCharCode(...new Uint8Array(bytes))).replace(/=/g,'').replace(/\+/g,'-').replace(/\//g,'_');
 async function database(){if(!opening)opening=new Promise((resolve,reject)=>{const r=indexedDB.open('fg-device-session-v1-'+(test?'test':'production'),1);r.onupgradeneeded=()=>r.result.createObjectStore('identity');r.onsuccess=()=>resolve(r.result);r.onerror=()=>reject(Error('device_store_unavailable'));});return opening;}
 async function store(value,write=false){const db=await database();return new Promise((resolve,reject)=>{const tx=db.transaction('identity',write?'readwrite':'readonly'),s=tx.objectStore('identity');const r=write?(value?s.put(value,'current'):s.delete('current')):s.get('current');tx.oncomplete=()=>resolve(write?value:r.result);tx.onerror=()=>reject(Error('device_store_failed'));tx.onabort=()=>reject(Error('device_store_failed'));});}
 async function enroll(request){await acquire();const keys=await crypto.subtle.generateKey({name:'RSASSA-PKCS1-v1_5',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash:'SHA-256'},false,['sign','verify']);const pub=await crypto.subtle.exportKey('jwk',keys.publicKey);const result=await request('/v1/session/enroll',{n:pub.n,e:pub.e});record={id:result.id,key:keys.privateKey};await store(record,true);info=result.info;active=true;lastTouch=Date.now();return info;}
 async function prove(request,purpose){record=record||await store();if(!record)return null;await acquire();if(record.key.extractable!==false)throw Error('device_key_not_secure');const c=await request('/v1/session/challenge',{id:record.id,purpose});const message=['FocusGrowth/session/v1',record.id,location.origin,purpose,c.nonce].join('\n');const signature=b64(await crypto.subtle.sign('RSASSA-PKCS1-v1_5',record.key,new TextEncoder().encode(message)));const result=await request('/v1/session/prove',{id:record.id,purpose,nonce:c.nonce,signature});info=result.info||result;active=true;return result;}
 async function forget(){await store(null,true);record=null;info=null;active=false;}
 function warning(){return info?.trusted?'此浏览器已永久受信任，可随时解除配对；内部令牌短期有效并自动更新':'';}
 function listen(request,onError){const touch=async e=>{if(!e.isTrusted||!active||document.visibilityState!=='visible'||working||Date.now()-lastTouch<30000)return;working=true;lastTouch=Date.now();try{await prove(request,'activity');}catch(error){onError(error);}finally{working=false;}};for(const type of ['pointerdown','keydown','wheel','touchstart'])document.addEventListener(type,touch,{capture:true,passive:true});}
 return {enroll,recover:request=>prove(request,'resume'),forget,warning,listen,release,info:()=>info};
})();
