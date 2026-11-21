using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
namespace FocusGrowth {
public sealed class FakeCloud:ITransport {
 public Snapshot Data=new Snapshot{State=Schema.Empty(),Sha="0"};public bool Offline,OnceConflict;public int Writes;
 public Snapshot Read(Snapshot c){if(Offline)throw new Fault("offline");return new Snapshot{State=J.Copy(Data.State),Sha=Data.Sha};}
 public void Write(Dictionary<string,object>s,string sha){if(Offline)throw new Fault("offline");if(OnceConflict){OnceConflict=false;throw new Fault("sha_conflict");}if(sha!=Data.Sha)throw new Fault("sha_conflict");Data=new Snapshot{State=J.Copy(s),Sha=(++Writes).ToString()};}
}
public static class Tests {
 static List<object> results=new List<object>();
 static void Test(string name,Action f){try{f();results.Add(new{name=name,pass=true});}catch{results.Add(new{name=name,pass=false});}}
 static void Yes(bool b){if(!b)throw new Exception();}
 static void Reject(Action f){try{f();}catch(Fault){return;}throw new Exception();}
 public static Dictionary<string,object> Fixture(){var s=Schema.Empty();s["projects"]=new object[]{new Dictionary<string,object>{{"id","p-test"},{"name","Synthetic project"},{"deadline",""},{"priority",""},{"weeklyMin",0}}};s["tasks"]=new object[]{new Dictionary<string,object>{{"id","t-test"},{"projectId","p-test"},{"parentId",null},{"name","Synthetic task"},{"estimate",30},{"date","2026-11-21"},{"goal","normal"},{"category","test"},{"priority",""},{"done",false},{"actual",null}}};return s;}
 static Dictionary<string,object> Task(Dictionary<string,object>s){return J.Obj(J.Arr(s["tasks"])[0]);}
 static Dictionary<string,object> Req(Dictionary<string,object>b,Dictionary<string,object>d,string op){return new Dictionary<string,object>{{"base",b},{"desired",d},{"operationId",op}};}
 public static void Run(string root){results.Clear();string projectRoot=Directory.Exists(Path.Combine(root,"..",".git"))||File.Exists(Path.Combine(root,"..",".git"))?Path.GetFullPath(Path.Combine(root,"..")):Path.GetFullPath(Path.Combine(root,"..",".."));string dir=Path.Combine(projectRoot,"work","sync-build","tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
 Test("empty_uninitialized_is_valid",()=>Schema.Validate(Schema.Empty()));
 Test("synthetic_fixture_valid",()=>Schema.Validate(Fixture()));
 Test("disjoint_fields_merge",()=>{var b=Fixture();var l=J.Copy(b);var r=J.Copy(b);Task(l)["name"]="local";Task(r)["estimate"]=60;List<string> c;var m=Schema.Merge(b,l,r,out c);Yes(c.Count==0&&(string)Task(m)["name"]=="local"&&(int)Task(m)["estimate"]==60);});
 Test("same_field_conflict",()=>{var b=Fixture();var l=J.Copy(b);var r=J.Copy(b);Task(l)["name"]="local";Task(r)["name"]="remote";List<string> c;Schema.Merge(b,l,r,out c);Yes(c.Count==1);});
 Test("unrelated_records_preserved",()=>{var b=Schema.Empty();var l=Fixture();var r=Schema.Empty();List<string> c;var m=Schema.Merge(b,l,r,out c);Yes(c.Count==0&&J.Arr(m["tasks"]).Count==1);});
 Test("deletion_disabled",()=>{var b=Fixture();var l=J.Copy(b);l["tasks"]=new object[0];List<string> c;Schema.Merge(b,l,b,out c);Yes(c.Any(x=>x.EndsWith("deletion_disabled")));});
 Test("invalid_id_rejected",()=>{var s=Fixture();Task(s)["id"]="../bad";Reject(()=>Schema.Validate(s));});
 Test("bad_reference_rejected",()=>{var s=Fixture();Task(s)["projectId"]="missing";Reject(()=>Schema.Validate(s));});
 Test("calendar_date_bounds",()=>{var s=Fixture();Task(s)["date"]="2026-02-30";Reject(()=>Schema.Validate(s));});
 Test("hierarchy_cycle",()=>{var s=Fixture();Task(s)["parentId"]="t-test";Reject(()=>Schema.Validate(s));});
 Test("unsupported_schema_halts",()=>{var s=Fixture();s["schemaVersion"]=2;Reject(()=>Schema.Validate(s));});
 Test("private_fields_rejected",()=>{foreach(string k in new[]{"note","timerMs","taskTimer","theme","activity","apiKey"}){var s=Fixture();Task(s)[k]="private";Reject(()=>Schema.Validate(s));}});
 Test("plan_bounds",()=>{var s=Fixture();s["plans"]=new object[]{new Dictionary<string,object>{{"id","pl"},{"taskId","t-test"},{"date","2026-11-21"},{"start",1430},{"duration",30}}};Reject(()=>Schema.Validate(s));});
 Test("duplicate_ids",()=>{var s=Fixture();s["tasks"]=new object[]{Task(s),Task(s)};Reject(()=>Schema.Validate(s));});
 Test("idempotent_operation_and_revision",()=>{var c=new FakeCloud();var e=new Engine(c,new Disk(Path.Combine(dir,"idempotent")));var q=Req(Schema.Empty(),Fixture(),"op1");e.Queue(q);e.Sync(true);e.Queue(q);e.Sync(true);Yes(c.Writes==1&&(int)c.Data.State["revision"]==1);});
 Test("sha_race_refetch",()=>{var c=new FakeCloud{OnceConflict=true};var e=new Engine(c,new Disk(Path.Combine(dir,"race")));e.Queue(Req(Schema.Empty(),Fixture(),"op2"));e.Sync(true);Yes(e.Status=="已同步"&&c.Writes==1);});
 Test("durable_offline_queue_restart",()=>{var c=new FakeCloud{Offline=true};var d=new Disk(Path.Combine(dir,"offline"));var e=new Engine(c,d);e.Queue(Req(Schema.Empty(),Fixture(),"op3"));e.Sync(true);Yes(e.Status=="离线");var restarted=new Engine(c,d);c.Offline=false;restarted.Sync(true);Yes(restarted.Status=="已同步"&&c.Writes==1);});
 Test("corrupt_store_preserved",()=>{var d=new Disk(Path.Combine(dir,"corrupt"));File.WriteAllText(Path.Combine(dir,"corrupt","cache.dat"),"invalid");Reject(()=>d.Load("cache"));Yes(File.Exists(Path.Combine(dir,"corrupt","cache.dat")));});
 Test("hostile_origin_and_dns_rebinding",()=>{foreach(string host in new[]{"evil.test:18417","localhost:18417","127.0.0.1:80"}){var r=new Request{Method="POST",Path="/v1/changes"};r.Headers["Host"]=host;r.Headers["Origin"]=Bridge.Local;Reject(()=>Bridge.Gate(r));}var bad=new Request{Method="POST",Path="/v1/changes"};bad.Headers["Host"]="127.0.0.1:18417";bad.Headers["Origin"]="https://evil.test";Reject(()=>Bridge.Gate(bad));});
 Test("pairing_approval_origin_expiry",()=>{var p=new Pairing();string code=p.Code;Reject(()=>p.Pair(code,Bridge.Local,_=>false));string token=p.Pair(code,Bridge.Local,_=>true);Yes(p.Auth(token,Bridge.Local)&&!p.Auth(token,Bridge.Pages)&&!p.Auth("wrong",Bridge.Local));Reject(()=>p.Pair(code,Bridge.Local,_=>true));p.ExpireForTest();Yes(!p.Auth(token,Bridge.Local));});
 Test("expired_pairing_code",()=>{var p=new Pairing();p.CodeExpiry=DateTime.MinValue;Reject(()=>p.Pair(p.Code,Bridge.Local,_=>true));});
 Test("invalid_json_and_oversize",()=>{Reject(()=>J.Parse("{"));Reject(()=>J.Parse(new string('x',1048577)));});
 Test("production_path_locked",()=>Reject(()=>{using(var c=new Cloud("sync/state.json")){};}));
 Test("explicit_conflict_resolution_and_idempotency",()=>{var c=new FakeCloud();c.Data=new Snapshot{State=Fixture(),Sha="base"};var e=new Engine(c,new Disk(Path.Combine(dir,"resolve")));e.Sync(true);var b=J.Copy(c.Data.State);var local=J.Copy(b);Task(local)["name"]="local";Task(c.Data.State)["name"]="remote";c.Data.Sha="remote";e.Queue(Req(b,local,"old-op"));e.Sync(true);Yes(e.Status=="冲突");var desired=J.Copy(e.Current.State);Task(desired)["name"]="local";var resolution=Req(J.Copy(e.Current.State),desired,"resolved-op");resolution["previousOperationId"]="old-op";e.Resolve(resolution);e.Sync(true);e.Resolve(resolution);Yes(e.Status=="已同步"&&(string)Task(c.Data.State)["name"]=="local"&&c.Writes==1);});
 Test("resolution_requires_current_conflict",()=>{var c=new FakeCloud();var e=new Engine(c,new Disk(Path.Combine(dir,"no-resolve")));var r=Req(Schema.Empty(),Fixture(),"bad-resolve");r["previousOperationId"]="missing";Reject(()=>e.Resolve(r));});
 int passed=results.Count(x=>J.Dump(x).Contains("\"pass\":true"));File.WriteAllText(Path.Combine(root,"unit-results.json"),J.Dump(new{passed=passed,total=results.Count,tests=results}),new System.Text.UTF8Encoding(false));Environment.ExitCode=passed==results.Count?0:1;
 }
 public static void CloudTest(string root){var output=new Dictionary<string,object>();string path="sync/tests/integration-"+Guid.NewGuid().ToString("N")+".json";output["path"]=path;try{using(var c=new Cloud(path)){try{c.Verify();output["identity_private"]=true;var b=c.Read(null);Yes(b.Sha==null);var d=Fixture();d["revision"]=1;d["initialized"]=true;d["updatedAt"]=DateTime.UtcNow.ToString("o");d["operationIds"]=new[]{"cloud-test"};c.Write(d,null);var one=c.Read(null);Yes((int)one.State["revision"]==1);output["create_and_read"]=true;Task(one.State)["name"]="Synthetic remote update";one.State["revision"]=2;c.Write(one.State,one.Sha);Yes((string)Task(c.Read(null).State)["name"]=="Synthetic remote update");output["update_and_read"]=true;var stale=Fixture();Reject(()=>c.Write(stale,"invalidsha"));output["stale_sha_rejected"]=true;}finally{c.DeleteSynthetic();output["cleanup"]=true;}}}catch(Fault e){output["error"]=e.Code;}catch{output["error"]="test_failed";}output["production_modified"]=false;File.WriteAllText(Path.Combine(root,"cloud-results.json"),J.Dump(output));}
}
}
