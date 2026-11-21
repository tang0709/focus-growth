const http=require('node:http'),fs=require('node:fs'),assert=require('node:assert/strict');
const results=[];
function request(method,path,headers={},body=''){return new Promise((resolve,reject)=>{const req=http.request({hostname:'127.0.0.1',port:18417,method,path,headers},res=>{let raw='';res.on('data',b=>raw+=b);res.on('end',()=>resolve({status:res.statusCode,headers:res.headers,raw}));});req.on('error',reject);req.setTimeout(6000,()=>req.destroy(Error('timeout')));req.end(body);});}
async function test(name,fn){try{await fn();results.push({name,pass:true});}catch(e){results.push({name,pass:false});}}
(async()=>{
 await test('health_has_no_personal_content',async()=>{const r=await request('GET','/health');assert.equal(r.status,200);assert.deepEqual(Object.keys(JSON.parse(r.raw)).sort(),['mode','productionLocked','version']);});
 await test('unauthenticated_request_rejected',async()=>{const r=await request('GET','/v1/state',{Origin:'http://127.0.0.1:18417','X-FG-Client':'1'});assert.equal(r.status,401);});
 await test('hostile_origin_rejected',async()=>{const r=await request('GET','/v1/state',{Origin:'https://evil.example',Authorization:'Bearer invalid','X-FG-Client':'1'});assert.equal(r.status,400);assert.equal(r.headers['access-control-allow-origin'],undefined);});
 await test('dns_rebinding_host_rejected',async()=>{const r=await request('GET','/health',{Host:'evil.example:18417'});assert.equal(r.status,400);});
 await test('csrf_form_post_rejected',async()=>{try{const r=await request('POST','/v1/changes',{Origin:'http://127.0.0.1:18417','Content-Type':'text/plain','Content-Length':2},'{}');assert.equal(r.status,400);}catch(e){/* The parser may close before consuming a forbidden form body. A TCP reset also rejects this request. */assert.equal(e.code,'ECONNRESET');}});
 await test('malformed_pair_json_rejected',async()=>{const r=await request('POST','/v1/pair',{Origin:'http://127.0.0.1:18417','Content-Type':'application/json','Content-Length':1},'{');assert.equal(r.status,400);});
 await test('oversized_body_rejected',async()=>{const r=await request('POST','/v1/changes',{Origin:'http://127.0.0.1:18417','Content-Type':'application/json','Content-Length':600001});assert.equal(r.status,400);});
 await test('exact_pages_preflight_no_wildcard',async()=>{const r=await request('OPTIONS','/v1/state',{Origin:'https://tang0709.github.io','Access-Control-Request-Method':'GET','Access-Control-Request-Headers':'Authorization, X-FG-Client'});assert.equal(r.status,204);assert.equal(r.headers['access-control-allow-origin'],'https://tang0709.github.io');});
 await test('arbitrary_file_path_rejected',async()=>{const r=await request('GET','/../Core.cs',{Origin:'http://127.0.0.1:18417'});assert.equal(r.status,401);assert(!r.raw.includes('CredentialStore'));});
 fs.writeFileSync(require('node:path').join(__dirname,'../security-results.json'),JSON.stringify({passed:results.filter(r=>r.pass).length,total:results.length,tests:results},null,2));console.log(JSON.stringify(results));if(results.some(r=>!r.pass))process.exitCode=1;
})();
