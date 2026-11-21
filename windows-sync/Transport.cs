using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.Net.Http.Headers;
namespace FocusGrowth {
public static class CredentialStore {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]struct C {public uint Flags,Type;public string Target,Comment;public System.Runtime.InteropServices.ComTypes.FILETIME Written;public uint Size;public IntPtr Blob;public uint Persist,Count;public IntPtr Attributes;public string Alias,User;}
 [DllImport("advapi32.dll",EntryPoint="CredReadW",CharSet=CharSet.Unicode,SetLastError=true)]static extern bool ReadNative(string target,uint type,uint flags,out IntPtr ptr);
 [DllImport("advapi32.dll")]static extern void CredFree(IntPtr p);
 public static string Read(){IntPtr ptr;if(!ReadNative("FocusGrowthSync/GitHub",1,0,out ptr))throw new Fault("credential_unavailable");try{var c=(C)Marshal.PtrToStructure(ptr,typeof(C));string s=Marshal.PtrToStringUni(c.Blob,(int)c.Size/2);for(int i=0;i<c.Size;i++)Marshal.WriteByte(c.Blob,i,0);return s;}finally{CredFree(ptr);}}
}
public sealed class Snapshot {public Dictionary<string,object> State;public string Sha,ETag;}
public interface ITransport {Snapshot Read(Snapshot cached);void Write(Dictionary<string,object> state,string sha);}
public sealed class Cloud : ITransport,IDisposable {
 const string Repo="https://api.github.com/repos/tang0709/focus-growth-data";
 readonly HttpClient client;readonly string path;public string TestPath {get{return path;}}
 public Cloud(string testPath){Schema.Require(System.Text.RegularExpressions.Regex.IsMatch(testPath,"^sync/tests/[A-Za-z0-9_-]{1,120}\\.json$"),"production_locked");path=testPath;System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false});client.Timeout=TimeSpan.FromSeconds(20);client.DefaultRequestHeaders.UserAgent.ParseAdd("FocusGrowthSync/0.3");client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));client.DefaultRequestHeaders.Add("X-GitHub-Api-Version","2022-11-28");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",CredentialStore.Read());}
 HttpResponseMessage Send(string method,string url,object data,string etag){try{using(var req=new HttpRequestMessage(new HttpMethod(method),url)){if(etag!=null)req.Headers.TryAddWithoutValidation("If-None-Match",etag);if(data!=null)req.Content=new StringContent(J.Dump(data),Encoding.UTF8,"application/json");var r=client.SendAsync(req).GetAwaiter().GetResult();if(r.StatusCode==(System.Net.HttpStatusCode)409||r.StatusCode==(System.Net.HttpStatusCode)422){r.Dispose();throw new Fault("sha_conflict");}if((int)r.StatusCode==429||(int)r.StatusCode==403){r.Dispose();throw new Fault("rate_limited_or_forbidden");}return r;}}catch(Fault){throw;}catch{throw new Fault("offline");}}
 Dictionary<string,object> Body(HttpResponseMessage r){try{return J.Obj(J.Parse(r.Content.ReadAsStringAsync().GetAwaiter().GetResult()));}catch(Fault){throw;}catch{throw new Fault("invalid_cloud_response");}}
 public void Verify(){using(var r=Send("GET",Repo,null,null)){Schema.Require((int)r.StatusCode==200,"repository_access");var d=Body(r);Schema.Require(J.Str(J.Get(d,"full_name"))=="tang0709/focus-growth-data"&&J.Get(d,"private") is bool&&(bool)d["private"],"repository_identity_or_privacy");}}
 public Snapshot Read(Snapshot cached){using(var r=Send("GET",Repo+"/contents/"+path+"?ref=main",null,cached==null?null:cached.ETag)){if((int)r.StatusCode==304){Schema.Require(cached!=null,"missing_conditional_cache");return cached;}if((int)r.StatusCode==404)return new Snapshot{State=Schema.Empty()};Schema.Require((int)r.StatusCode==200,"cloud_read_failed");var d=Body(r);Schema.Require(J.Str(J.Get(d,"type"))=="file"&&J.Str(J.Get(d,"encoding"))=="base64","unsupported_cloud_content");var s=J.Obj(J.Parse(Encoding.UTF8.GetString(Convert.FromBase64String((string)d["content"]))));Schema.Validate(s);return new Snapshot{State=s,Sha=(string)d["sha"],ETag=r.Headers.ETag==null?null:r.Headers.ETag.ToString()};}}
 public void Write(Dictionary<string,object>s,string sha){Schema.Validate(s);Verify();var body=new Dictionary<string,object>{{"message","Apply synthetic Focus Growth synchronization operation"},{"branch","main"},{"content",Convert.ToBase64String(Encoding.UTF8.GetBytes(J.Dump(s)))}};if(sha!=null)body["sha"]=sha;using(var r=Send("PUT",Repo+"/contents/"+path,body,null)){Schema.Require((int)r.StatusCode==200||(int)r.StatusCode==201,"cloud_write_failed");}}
 public void DeleteSynthetic(){var s=Read(null);if(s.Sha==null)return;Verify();using(var r=Send("DELETE",Repo+"/contents/"+path,new{message="Remove isolated synthetic sync fixture",sha=s.Sha,branch="main"},null)){Schema.Require((int)r.StatusCode==200,"cleanup_failed");}Schema.Require(Read(null).Sha==null,"cleanup_verify_failed");}
 public void Dispose(){client.DefaultRequestHeaders.Authorization=null;client.Dispose();}
}
public sealed class Engine {
 readonly ITransport cloud;readonly Disk disk;readonly object gate=new object();public Snapshot Current;public string Status="未连接",Error;public string LastSuccess;public List<string> Conflicts=new List<string>();Dictionary<string,object> pending;int failures;DateTime next=DateTime.MinValue;
 public Engine(ITransport t,Disk d){cloud=t;disk=d;var cache=d.Load("cache");Current=cache==null?new Snapshot{State=Schema.Empty()}:new Snapshot{State=J.Obj(cache["state"]),Sha=J.Str(J.Get(cache,"sha"))};Schema.Validate(Current.State);var q=d.Load("queue");if(q!=null&&J.Get(q,"pending") is bool&&(bool)q["pending"]){pending=J.Obj(q["request"]);ValidateRequest(pending);}}
 static void ValidateRequest(Dictionary<string,object>r){Schema.Require(r.Count==3&&r.ContainsKey("base")&&r.ContainsKey("desired")&&r.ContainsKey("operationId"),"invalid_change_request");Schema.Id(r["operationId"]);Schema.Validate(J.Obj(r["base"]));Schema.Validate(J.Obj(r["desired"]));}
 public void Queue(Dictionary<string,object> request){lock(gate){ValidateRequest(request);if(pending!=null){Schema.Require(J.Equal(pending,request),"queue_busy");return;}disk.Save("queue",new{pending=true,request=request});pending=J.Copy(request);Conflicts.Clear();next=DateTime.MinValue;}}
 public void Resolve(Dictionary<string,object> resolution){lock(gate){Schema.Require(resolution.Count==4&&resolution.ContainsKey("previousOperationId"),"invalid_resolution");var request=J.Copy(resolution);string previous=J.Str(request["previousOperationId"]);request.Remove("previousOperationId");ValidateRequest(request);string op=(string)request["operationId"];
  if(J.Arr(Current.State["operationIds"]).Contains(op))return;
  if(pending!=null&&J.Equal(pending,request))return;
  Schema.Require(pending!=null&&Status=="冲突"&&previous==J.Str(pending["operationId"]),"resolution_not_current");
  Schema.Require(J.Equal(request["base"],Current.State),"resolution_remote_changed");
  List<string> blocked;Schema.Merge(Current.State,J.Obj(request["desired"]),Current.State,out blocked);Schema.Require(blocked.Count==0,"resolution_deletion_disabled");
  // Atomic replacement retains the old conflicted queue as its encrypted .bak.
  disk.Save("queue",new{pending=true,request=request});pending=request;Conflicts.Clear();Error=null;Status="同步中";next=DateTime.MinValue;
 }}
 void Cache(Snapshot s){disk.Save("cache",new{state=s.State,sha=s.Sha});Current=s;}
 public Dictionary<string,object> View(){lock(gate)return new Dictionary<string,object>{{"mode","synthetic"},{"status",Status},{"error",Error},{"lastSuccess",LastSuccess},{"pending",pending!=null},{"pendingOperationId",pending==null?null:pending["operationId"]},{"conflicts",Conflicts},{"state",Current.State},{"productionLocked",true}};}
 public void Sync(bool manual){lock(gate){if((!manual||Error=="rate_limited_or_forbidden")&&DateTime.UtcNow<next)return;Status="同步中";try{
  var remote=cloud.Read(Current.Sha==null?null:Current);
  if(pending!=null){string op=(string)pending["operationId"];bool done=false;
   for(int retry=0;retry<3;retry++){
    if(J.Arr(remote.State["operationIds"]).Contains(op)){done=true;break;}
    List<string> conflicts;var merged=Schema.Merge(J.Obj(pending["base"]),J.Obj(pending["desired"]),remote.State,out conflicts);
    if(conflicts.Count>0){Conflicts=conflicts;Cache(remote);Status="冲突";Error="manual_resolution_required";next=DateTime.UtcNow.AddMinutes(1);return;}
    if(Schema.SameData(merged,remote.State)){done=true;break;}
    merged["revision"]=J.Num(remote.State["revision"])+1;merged["initialized"]=true;merged["updatedAt"]=DateTime.UtcNow.ToString("o");var ids=J.Arr(remote.State["operationIds"]);ids.Add(op);merged["operationIds"]=ids;Schema.Validate(merged);
    try{cloud.Write(merged,remote.Sha);}catch(Fault ex){if(ex.Code!="sha_conflict")throw;remote=cloud.Read(null);continue;}
    remote=cloud.Read(null);if(!J.Arr(remote.State["operationIds"]).Contains(op))throw new Fault("write_result_uncertain");done=true;break;
   }
   Schema.Require(done,"retry_exhausted");Cache(remote);disk.Clear("queue");pending=null;
  }else Cache(remote);
  failures=0;Conflicts.Clear();Error=null;Status="已同步";LastSuccess=DateTime.UtcNow.ToString("o");next=DateTime.UtcNow.AddSeconds(30);
 }catch(Fault ex){Error=ex.Code;Status=ex.Code=="offline"?"离线":ex.Code=="sha_conflict"?"冲突":"离线";failures++;next=DateTime.UtcNow.AddSeconds(ex.Code=="rate_limited_or_forbidden"?3600:Math.Min(300,Math.Pow(2,Math.Min(failures,8))*5));}catch{Error="local_io_or_transport_failure";Status="离线";next=DateTime.UtcNow.AddSeconds(60);}}}
}
}
