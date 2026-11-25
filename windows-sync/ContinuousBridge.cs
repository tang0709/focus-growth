using System;using System.IO;using System.Text;using System.Linq;using System.Net;using System.Net.Sockets;using System.Threading;using System.Collections.Generic;
namespace FocusGrowth {
public sealed class ContinuousBridge:IDisposable {
 readonly DeviceSessions devices;readonly DesktopActivity activity;readonly Dictionary<string,string> tokens=new Dictionary<string,string>();readonly object authGate=new object();
 readonly TcpListener listener;readonly Pairing pairing;readonly ContinuousController control;readonly Func<string,bool> approve;readonly string web;readonly string Local;readonly int port;const string Pages=Bridge.Pages;bool stopped;int active;
 public ContinuousBridge(ContinuousController c,Pairing p,string folder,Func<string,bool> confirmation,DeviceSessions deviceSessions=null,Disk activityDisk=null){devices=deviceSessions;activity=activityDisk==null?null:new DesktopActivity(activityDisk);control=c;pairing=p;web=folder;approve=confirmation;port=c.Production?18421:18420;Local="http://127.0.0.1:"+port;listener=new TcpListener(IPAddress.Loopback,port);}
 string Origin(Request r){string value;if(r.Headers.TryGetValue("Origin",out value))return value;string site;if(r.Method=="GET"&&r.Headers.TryGetValue("Sec-Fetch-Site",out site)&&site=="same-origin")return Local;return null;}
 public void Start(){listener.Start(16);ThreadPool.QueueUserWorkItem(_=>{while(!stopped)try{var socket=listener.AcceptTcpClient();if(Interlocked.Increment(ref active)>8){Interlocked.Decrement(ref active);socket.Close();continue;}ThreadPool.QueueUserWorkItem(x=>Handle(socket));}catch{if(!stopped)Thread.Sleep(100);}});}
 static Request Parse(NetworkStream stream){var bytes=new List<byte>();int n;while((n=stream.ReadByte())>=0){bytes.Add((byte)n);if(bytes.Count>16384)throw new Fault("headers_too_large");int c=bytes.Count;if(c>=4&&bytes[c-4]==13&&bytes[c-3]==10&&bytes[c-2]==13&&bytes[c-1]==10)break;}Schema.Require(bytes.Count>0,"invalid_http");var lines=Encoding.ASCII.GetString(bytes.ToArray()).Split(new[]{"\r\n"},StringSplitOptions.None);var first=lines[0].Split(' ');Schema.Require(first.Length==3&&first[2]=="HTTP/1.1"&&first[1].StartsWith("/")&&!first[1].StartsWith("//"),"invalid_http");var r=new Request{Method=first[0],Path=first[1].Split('?')[0]};foreach(var line in lines.Skip(1)){if(line=="")break;int colon=line.IndexOf(':');Schema.Require(colon>0,"invalid_headers");string k=line.Substring(0,colon),v=line.Substring(colon+1).Trim();Schema.Require(!r.Headers.ContainsKey(k)&&!k.Any(Char.IsWhiteSpace),"duplicate_or_invalid_header");r.Headers[k]=v;}Schema.Require(!r.Headers.ContainsKey("Transfer-Encoding"),"transfer_encoding_rejected");int length=0;string raw;if(r.Headers.TryGetValue("Content-Length",out raw))Schema.Require(Int32.TryParse(raw,out length)&&length>=0&&length<=((r.Path=="/v1/attach"||r.Path=="/v1/checkpoint")?12582912:600000),"body_too_large");if(r.Method=="POST"){string type;Schema.Require(r.Headers.TryGetValue("Content-Type",out type)&&type.Split(';')[0].Trim()=="application/json","json_required");Schema.Require(length>0,"body_required");}byte[] body=new byte[length];int read=0;while(read<length){int k=stream.Read(body,read,length-read);Schema.Require(k>0,"truncated_body");read+=k;}try{r.Body=new UTF8Encoding(false,true).GetString(body);}catch{throw new Fault("invalid_utf8");}return r;}
 void Respond(NetworkStream s,int code,object data,string origin,string type){byte[] b=data is byte[]?(byte[])data:Encoding.UTF8.GetBytes(J.Dump(data));var headers="HTTP/1.1 "+code+" Result\r\nConnection: close\r\nContent-Length: "+b.Length+"\r\nContent-Type: "+type+"\r\nCache-Control: no-store\r\nX-Content-Type-Options: nosniff\r\nReferrer-Policy: no-referrer\r\n";if(origin==Local||origin==Pages)headers+="Access-Control-Allow-Origin: "+origin+"\r\nVary: Origin\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nAccess-Control-Allow-Headers: Authorization, Content-Type, X-FG-Client\r\nAccess-Control-Allow-Private-Network: true\r\n";byte[] h=Encoding.ASCII.GetBytes(headers+"\r\n");s.Write(h,0,h.Length);s.Write(b,0,b.Length);}

 void Handle(TcpClient socket){Request req=null;try{using(socket){socket.ReceiveTimeout=5000;socket.SendTimeout=5000;using(var stream=socket.GetStream()){try{req=Parse(stream);string host;Schema.Require(req.Headers.TryGetValue("Host",out host)&&host=="127.0.0.1:"+port,"host_rejected");Schema.Require(new[]{"GET","POST","OPTIONS"}.Contains(req.Method),"method_rejected");string origin=Origin(req);
 if(req.Method=="GET"&&req.Path=="/health"){Respond(stream,200,new{version="0.4",mode=control.Production?"production":"synthetic",productionLocked=control.Production&&!control.Authorized,paused=!control.Enabled},null,"application/json");return;}
 if(req.Method=="GET"&&new[]{"/","/sync.js","/preparation.js","/device-session.js"}.Contains(req.Path)){Respond(stream,200,File.ReadAllBytes(Path.Combine(web,req.Path=="/"?"index.html":req.Path.Substring(1))),null,req.Path=="/"?"text/html; charset=utf-8":"text/javascript; charset=utf-8");return;}
 Schema.Require(origin==Local||origin==Pages,"origin_rejected");var routes=new[]{"/v1/pair","/v1/state","/v1/attach","/v1/checkpoint","/v1/enable","/v1/pause","/v1/sync","/v1/changes","/v1/resolve","/v1/test/network","/v1/session/enroll","/v1/session/challenge","/v1/session/prove","/v1/session/info","/v1/session/revoke","/v1/authorization/revoke","/v1/activity","/v1/activity/arm","/v1/activity/stop"};Schema.Require(routes.Contains(req.Path),"route_rejected");
 if(req.Method=="OPTIONS"){string method;Schema.Require(req.Headers.TryGetValue("Access-Control-Request-Method",out method)&&new[]{"GET","POST"}.Contains(method),"preflight_rejected");Respond(stream,204,new byte[0],origin,"application/json");return;}
 var body=req.Method=="POST"?J.Obj(LocalJson.Parse(req.Body)):null;
 if(req.Method=="POST"&&req.Path=="/v1/pair"){Schema.Require(body.Count==1&&body.ContainsKey("code"),"invalid_pair_body");var session=pairing.Pair(J.Str(body["code"]),origin,approve);Respond(stream,200,new{session=session,expiresIn=900,version="0.4",mode=control.Production?"production":"synthetic"},origin,"application/json");return;}
 
 if(devices!=null&&req.Method=="POST"&&(req.Path=="/v1/session/challenge"||req.Path=="/v1/session/prove")){
 string id=J.Str(J.Get(body,"id")),purpose=J.Str(J.Get(body,"purpose"));object reply;
 if(req.Path=="/v1/session/challenge"){Schema.Require(body.Count==2,"invalid_challenge");reply=new{nonce=devices.Challenge(id,origin,purpose)};}
 else {Schema.Require(body.Count==4,"invalid_proof");var info=devices.Prove(id,origin,purpose,J.Str(body["nonce"]),J.Str(body["signature"]));if(purpose=="resume"){lock(authGate){string token=pairing.IssueVerified(origin);tokens.Clear();tokens[token]=id;control.TransferLease();reply=new{session=token,info=info};}}else reply=info;}
 Respond(stream,200,reply,origin,"application/json");return;}
 string auth=null,csrf;Schema.Require(req.Headers.TryGetValue("X-FG-Client",out csrf)&&csrf=="1"&&req.Headers.TryGetValue("Authorization",out auth)&&auth.StartsWith("Bearer ")&&pairing.Auth(auth.Substring(7),origin),"unauthorized");
 
 if(devices!=null){string token=auth.Substring(7),id=null;lock(authGate){tokens.TryGetValue(token,out id);}
 if(req.Path=="/v1/session/enroll"){Schema.Require(req.Method=="POST"&&body.Count==2,"invalid_enroll");Schema.Require(id==null,"already_enrolled");id=devices.Enroll(origin,J.Str(body["n"]),J.Str(body["e"]));lock(authGate){tokens[token]=id;}Respond(stream,200,new{id=id,info=devices.Info(id,origin)},origin,"application/json");return;}
 Schema.Require(id!=null&&devices.Active(id,origin),"unauthorized");
 if(req.Method=="GET"&&req.Path=="/v1/session/info"){Respond(stream,200,devices.Info(id,origin),origin,"application/json");return;}
 if(req.Method=="POST"&&req.Path=="/v1/session/revoke"){Schema.Require(body.Count==0,"invalid_revoke");devices.Revoke(id,origin);control.Release();pairing.Rotate();lock(authGate){tokens.Clear();}Respond(stream,200,new{revoked=true},origin,"application/json");return;}
 if(req.Method=="POST"&&req.Path=="/v1/authorization/revoke"){Schema.Require(body.Count==0,"invalid_revoke");control.RevokeAuthorization();Respond(stream,200,new{revoked=true},origin,"application/json");return;}
 }
 if(req.Method=="GET"&&req.Path=="/v1/activity"){Schema.Require(activity!=null,"activity_unavailable");Respond(stream,200,activity.Snapshot(),origin,"application/json");return;}
 if(req.Method=="POST"&&(req.Path=="/v1/activity/arm"||req.Path=="/v1/activity/stop")){
  Schema.Require(activity!=null&&body.Count==0,"invalid_activity_body");
  Respond(stream,200,req.Path=="/v1/activity/arm"?activity.Arm():activity.Stop(),origin,"application/json");return;
 }
 if(req.Method=="GET"&&req.Path=="/v1/state"){Respond(stream,200,control.View(),origin,"application/json");return;}
 Schema.Require(req.Method=="POST","method_rejected");object result=null;
 if(req.Path=="/v1/attach")result=control.Attach(body,origin);
 else if(req.Path=="/v1/checkpoint")result=control.Checkpoint(body);
 else if(req.Path=="/v1/enable"){Schema.Require(body.Count==1,"invalid_enable");control.Resume(J.Str(J.Get(body,"clientId")));control.Tick(true);}
 else if(req.Path=="/v1/pause"){Schema.Require(body.Count==0,"invalid_pause");control.Pause();}
 else if(req.Path=="/v1/changes"||req.Path=="/v1/resolve"){control.Queue(body,req.Path=="/v1/resolve");control.Tick(false);}
 else if(req.Path=="/v1/sync"){Schema.Require(body.Count==1&&J.Get(body,"manual") is bool,"invalid_sync");if((bool)body["manual"])control.Tick(true);}
 else if(req.Path=="/v1/test/network"){Schema.Require(body.Count==1&&J.Get(body,"offline") is bool,"invalid_network");control.SetNetwork((bool)body["offline"]);control.Tick(true);}
 else throw new Fault("route_rejected");Respond(stream,200,result??control.View(),origin,"application/json");
 }catch(Fault ex){Respond(stream,ex.Code=="unauthorized"?401:ex.Code.Contains("authorization")||ex.Code=="synthetic_control_only"?403:ex.Code.Contains("conflict")||ex.Code=="workspace_in_use"?409:400,new{error=ex.Code},req==null?null:Origin(req),"application/json");}catch{Respond(stream,500,new{error="request_failed"},null,"application/json");}}}}catch{}finally{Interlocked.Decrement(ref active);}}
 public void Dispose(){stopped=true;listener.Stop();activity?.Dispose();}
}
}
