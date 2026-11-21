using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using FocusGrowth;
namespace FocusGrowthMigration {
// No network listener, arbitrary destination, force switch, deletion or general update API.
public sealed class Approval {
 public string ConsentId,Preview,Backup,Local,Full,Remote; public int[] Counts;
 public string OperationId {get{return "initial-"+ConsentId;}}
 public void Validate(){Schema.Id(ConsentId);foreach(string v in new[]{Preview,Backup,Local,Full})Schema.Require(v!=null&&System.Text.RegularExpressions.Regex.IsMatch(v,"^[a-f0-9]{64}$"),"approval_digest_invalid");Schema.Require(Remote!=null&&System.Text.RegularExpressions.Regex.IsMatch(Remote,"^[a-f0-9]{40}$"),"approval_remote_invalid");Schema.Require(Counts!=null&&Counts.Length==3&&Counts.All(n=>n>0),"approval_counts_invalid");}
}
public interface IOnce {void Consume(Approval approval);}
public sealed class OnceDisk:IOnce {
 readonly string root; public OnceDisk(string folder){root=folder;Directory.CreateDirectory(root);}
 public void Consume(Approval a){a.Validate();string key=LocalJson.Digest(new Dictionary<string,object>{{"preview",a.Preview},{"remote",a.Remote}});string path=Path.Combine(root,"initial-"+key+".spent");var bytes=ProtectedData.Protect(Encoding.UTF8.GetBytes(LocalJson.Dump(new{consentId=a.ConsentId,operationId=a.OperationId,preview=a.Preview,backup=a.Backup,local=a.Local,full=a.Full,remote=a.Remote,consumedUtc=DateTime.UtcNow.ToString("o")})),null,DataProtectionScope.CurrentUser);try{using(var f=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None)){f.Write(bytes,0,bytes.Length);f.Flush(true);}}catch(IOException){throw new Fault("approval_already_consumed_or_unavailable");}}
}
public static class Gate {
 public static Dictionary<string,object> Payload(Approval a,Dictionary<string,object> approved,Dictionary<string,object> latest,Snapshot remote,string preview,string backup){
  a.Validate();PreparationModel.ValidateFull(approved);PreparationModel.ValidateFull(latest);Schema.Require(J.Get(latest,"demo") is bool&&!(bool)latest["demo"],"production_source_not_confirmed");
  Schema.Require(a.Preview==preview&&a.Backup==backup,"approval_binding_changed");Schema.Require(LocalJson.Digest(approved)==a.Full&&LocalJson.Canonical(approved)==LocalJson.Canonical(latest),"local_snapshot_changed");
  var p=PreparationModel.Project(latest);Schema.Require(LocalJson.Digest(p)==a.Local,"local_projection_changed");for(int i=0;i<Schema.Collections.Length;i++)Schema.Require(J.Arr(p[Schema.Collections[i]]).Count==a.Counts[i],"approved_counts_changed");
  Schema.Validate(remote.State);Schema.Require(remote.Sha==a.Remote&&(int)remote.State["revision"]==0&&!(bool)remote.State["initialized"]&&Schema.Collections.All(k=>J.Arr(remote.State[k]).Count==0)&&J.Arr(remote.State["operationIds"]).Count==0&&remote.State["updatedAt"]==null,"remote_not_approved_empty_state");
  p["revision"]=1;p["initialized"]=true;p["updatedAt"]=DateTime.UtcNow.ToString("o");p["operationIds"]=new[]{a.OperationId};Schema.Validate(p);return p;
 }
 public static void Execute(Approval a,Dictionary<string,object> approved,Func<Dictionary<string,object>> current,Snapshot remote,string preview,string backup,IOnce once,Action<Dictionary<string,object>,string> conditionalCommit){
  var payload=Payload(a,approved,current(),remote,preview,backup);once.Consume(a);
  // Recheck immutable local binding after durable consumption and immediately before the sole PUT.
  Payload(a,approved,current(),remote,preview,backup);conditionalCommit(payload,a.Remote);
 }
}
sealed class ProductionHttp:IDisposable {
 const string Repo="https://api.github.com/repos/tang0709/focus-growth-data";readonly HttpClient client;
 public ProductionHttp(){System.Net.ServicePointManager.SecurityProtocol=System.Net.SecurityProtocolType.Tls12;client=new HttpClient(new HttpClientHandler{AllowAutoRedirect=false});client.Timeout=TimeSpan.FromSeconds(20);client.DefaultRequestHeaders.UserAgent.ParseAdd("FocusGrowthInitialMigration/0.3.1");client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));client.DefaultRequestHeaders.Add("X-GitHub-Api-Version","2022-11-28");client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer",CredentialStore.Read());}
 internal ProductionHttp(HttpMessageHandler syntheticHandler){client=new HttpClient(syntheticHandler);client.DefaultRequestHeaders.Authorization=new AuthenticationHeaderValue("Bearer","SYNTHETIC_ONLY");}
 Dictionary<string,object> Get(string url){try{using(var r=client.GetAsync(url).GetAwaiter().GetResult()){Schema.Require((int)r.StatusCode==200,"remote_read_failed");return J.Obj(J.Parse(r.Content.ReadAsStringAsync().GetAwaiter().GetResult()));}}catch(Fault){throw;}catch{throw new Fault("network_failure");}}
 public void VerifyPrivate(){var r=Get(Repo);Schema.Require(J.Str(J.Get(r,"full_name"))=="tang0709/focus-growth-data"&&J.Get(r,"private") is bool&&(bool)r["private"],"repository_not_private_or_wrong_identity");}
 public Snapshot Read(){var r=Get(Repo+"/contents/sync/state.json?ref=main");Schema.Require(J.Str(J.Get(r,"type"))=="file"&&J.Str(J.Get(r,"encoding"))=="base64","remote_format_invalid");var state=J.Obj(J.Parse(Encoding.UTF8.GetString(Convert.FromBase64String((string)r["content"]))));Schema.Validate(state);return new Snapshot{Sha=(string)r["sha"],State=state};}
 // Only this assembly's audited coordinator can reach the sole fixed-target PUT.
 public object Migrate(Approval a,Dictionary<string,object> approved,Func<Dictionary<string,object>> current,string preview,string backup,IOnce once){VerifyPrivate();var remote=Read();string commit=null;Dictionary<string,object> expected=null;Gate.Execute(a,approved,current,remote,preview,backup,once,(payload,sha)=>{
   VerifyPrivate();var justBefore=Read();Schema.Require(justBefore.Sha==sha,"remote_changed_before_put");Gate.Payload(a,approved,current(),justBefore,preview,backup);
   expected=J.Copy(payload);var body=new{message="Initialize approved Focus Growth workspace",branch="main",sha=sha,content=Convert.ToBase64String(Encoding.UTF8.GetBytes(J.Dump(payload)))};
   try{using(var response=client.PutAsync(Repo+"/contents/sync/state.json",new StringContent(J.Dump(body),Encoding.UTF8,"application/json")).GetAwaiter().GetResult()){Schema.Require((int)response.StatusCode==200,"conditional_write_failed_or_conflict");var receipt=J.Obj(J.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult()));commit=J.Str(J.Get(J.Obj(receipt["commit"]),"sha"));}}catch(Fault){throw;}catch{throw new Fault("write_result_uncertain_no_retry");}
  });var after=Read();Schema.Require(expected!=null&&J.Equal(expected,after.State)&&after.Sha!=remote.Sha,"post_write_verification_failed");return new{status="SUCCESS",beforeSha=remote.Sha,afterSha=after.Sha,commitSha=commit,revision=1,initialized=true,operationId=a.OperationId,projects=J.Arr(after.State["projects"]).Count,tasks=J.Arr(after.State["tasks"]).Count,plans=J.Arr(after.State["plans"]).Count,previewDigest=a.Preview,backupSha256=a.Backup,realDataUploaded=true,continuousProductionSyncEnabled=false};
 }
 public void Dispose(){client.DefaultRequestHeaders.Authorization=null;client.Dispose();}
}
static class Program {
 static Dictionary<string,object> ReadDb(string path){return J.Obj(LocalJson.Parse(File.ReadAllText(path)));}
 [STAThread]static int Main(string[] args){if(args.Length==1&&args[0]=="--selftest")return MigrationTests.Run(AppDomain.CurrentDomain.BaseDirectory);if(args.Length!=3||args[0]!="--initialize-once")return 2;
  string manifestPath=Path.GetFullPath(args[1]),exportPath=Path.GetFullPath(args[2]),folder=Path.GetDirectoryName(manifestPath);bool attempted=false;try{
   var m=ReadDb(manifestPath);Schema.Require(m.Count==9&&J.Str(m["repository"])=="tang0709/focus-growth-data"&&J.Str(m["path"])=="sync/state.json"&&J.Str(m["scope"])=="one_initialization_projects_tasks_plans_only"&&J.Str(m["approvalEvidence"])=="explicit_user_request_in_current_conversation","explicit_scoped_approval_required");
   var report=ReadDb((string)m["previewReport"]);Schema.Require((string)report["conclusion"]=="READY_FOR_APPROVAL","preview_not_ready");var source=J.Obj(report["source"]);Schema.Require((string)source["origin"]==Bridge.Pages&&(string)source["pathname"]=="/focus-growth/"&&(string)source["storageKey"]=="start-workbench-v1"&&(bool)source["present"]&&!(bool)source["synthetic"],"wrong_source");
   var encrypted=J.Obj(report["backup"]);byte[] raw=LocalVault.Read(encrypted);var approved=J.Obj(LocalJson.Parse(Encoding.UTF8.GetString(raw)));Array.Clear(raw,0,raw.Length);var details=J.Obj(LocalJson.Parse(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes((string)report["detailsEncryptedPath"]),null,DataProtectionScope.CurrentUser))));Schema.Require(LocalJson.Digest(details["binding"])==(string)report["previewDigest"],"preview_digest_invalid");
   var expected=J.Obj(m["approvedBinding"]);var a=new Approval{ConsentId=(string)m["consentId"],Preview=(string)expected["preview"],Backup=(string)expected["backup"],Local=(string)expected["local"],Full=(string)expected["full"],Remote=(string)expected["remote"],Counts=J.Arr(m["counts"]).Select(J.Num).ToArray()};
   Schema.Require(a.Remote==(string)report["remoteSha"]&&a.Backup==(string)J.Obj(details["binding"])["backup"]&&a.Local==(string)J.Obj(details["binding"])["local"]&&a.Counts.SequenceEqual(new[]{3,25,12}),"approval_does_not_match_reviewed_object");
   Func<Dictionary<string,object>> current=()=>{Schema.Require(File.GetLastWriteTimeUtc(exportPath)>DateTime.UtcNow.AddSeconds(-90),"latest_browser_export_stale");return ReadDb(exportPath);};
   PreparationModel.ValidateFull(current());var vault=new LocalVault(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusGrowthSync","initial-migration","backups"));var backup=vault.SaveVerified("before-initialization",Encoding.UTF8.GetBytes(LocalJson.Dump(current())));var once=new OnceDisk(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusGrowthSync","initial-migration","approvals"));
   using(var transport=new ProductionHttp()){attempted=true;var receipt=transport.Migrate(a,approved,current,(string)report["previewDigest"],(string)encrypted["sha256"],once);File.WriteAllText(Path.Combine(folder,"RESULT.json"),LocalJson.Dump(receipt));}return 0;
  }catch(Fault e){File.WriteAllText(Path.Combine(folder,"RESULT.json"),LocalJson.Dump(new{status="BLOCKED",reason=e.Code,operationMayHaveStarted=attempted,automaticRetry=false}));return 1;}catch{File.WriteAllText(Path.Combine(folder,"RESULT.json"),LocalJson.Dump(new{status="BLOCKED",reason="local_environment_failure",operationMayHaveStarted=attempted,automaticRetry=false}));return 1;}
 }
}
}
