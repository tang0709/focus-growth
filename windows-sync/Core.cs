using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
namespace FocusGrowth {
public sealed class Fault : Exception { public string Code; public Fault(string code):base(code){Code=code;} }
public static class J {
 public static readonly JavaScriptSerializer Serializer=new JavaScriptSerializer{MaxJsonLength=1048576,RecursionLimit=64};
 public static string Dump(object v){lock(Serializer)return Serializer.Serialize(v);}
 public static object Parse(string s){try{lock(Serializer)return Serializer.DeserializeObject(s);}catch{throw new Fault("invalid_json");}}
 public static Dictionary<string,object> Obj(object o){var d=o as Dictionary<string,object>;if(d==null)throw new Fault("object_required");return d;}
 public static Dictionary<string,object> Copy(object o){return Obj(Parse(Dump(o)));}
 public static object Get(Dictionary<string,object>d,string k){object v;return d.TryGetValue(k,out v)?v:null;}
 public static string Str(object o){return o as string;}
 public static List<object> Arr(object o){var a=o as IEnumerable;if(a==null||o is string||o is IDictionary)throw new Fault("array_required");return a.Cast<object>().ToList();}
 public static bool Equal(object a,object b){if(a==null||b==null)return a==b;if(a is IDictionary&&b is IDictionary){var x=Obj(a);var y=Obj(b);return x.Count==y.Count&&x.All(k=>y.ContainsKey(k.Key)&&Equal(k.Value,y[k.Key]));}if(!(a is string)&&a is IEnumerable&&!(b is string)&&b is IEnumerable){var x=Arr(a);var y=Arr(b);return x.Count==y.Count&&x.Select((v,i)=>Equal(v,y[i])).All(v=>v);}return a.Equals(b);}
 public static int Num(object o){if(!(o is int))throw new Fault("integer_required");return (int)o;}
 public static string Random(){byte[] b=new byte[24];using(var r=RandomNumberGenerator.Create())r.GetBytes(b);return Convert.ToBase64String(b).TrimEnd('=').Replace('+','-').Replace('/','_');}
 public static bool SecretEqual(string a,string b){if(a==null||b==null)return false;byte[] x=Encoding.UTF8.GetBytes(a),y=Encoding.UTF8.GetBytes(b);int n=x.Length^y.Length;for(int i=0;i<Math.Max(x.Length,y.Length);i++)n|=(i<x.Length?x[i]:0)^(i<y.Length?y[i]:0);return n==0;}
}
public static class Schema {
 public static readonly string[] Collections={"projects","tasks","plans"};
 public static readonly Dictionary<string,string[]> Fields=new Dictionary<string,string[]>{
 {"projects",new[]{"id","name","deadline","priority","weeklyMin"}},
 {"tasks",new[]{"id","projectId","parentId","name","estimate","date","goal","category","priority","done","actual","completedAt"}},
 {"plans",new[]{"id","taskId","date","start","duration"}}};
 public static Dictionary<string,object> Empty(){return J.Obj(J.Parse("{\"schemaVersion\":1,\"revision\":0,\"initialized\":false,\"updatedAt\":null,\"operationIds\":[],\"projects\":[],\"tasks\":[],\"plans\":[]}"));}
 public static void Require(bool ok,string code){if(!ok)throw new Fault(code);}
 public static void Id(object o){Require(o is string&&System.Text.RegularExpressions.Regex.IsMatch((string)o,"^[A-Za-z0-9_-]{1,120}$"),"invalid_id");}
 static void Text(Dictionary<string,object>d,string k,int max,bool nullable){object v=J.Get(d,k);Require(nullable&&v==null||v is string&&((string)v).Length<=max,"invalid_text_"+k);if(v is string)Require(!((string)v).Any(c=>Char.IsControl(c)&&c!='\n'&&c!='\t'),"invalid_control");}
 static void Date(Dictionary<string,object>d,string k,bool optional){object v=J.Get(d,k);if(optional&&(v==null||J.Str(v)==""))return;DateTime x;Require(v is string&&DateTime.TryParseExact((string)v,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out x),"invalid_date");}
 static void Range(Dictionary<string,object>d,string k,int min,int max){int n=J.Num(J.Get(d,k));Require(n>=min&&n<=max,"invalid_range_"+k);}
 public static Dictionary<string,Dictionary<string,object>> Map(Dictionary<string,object>s,string k){var map=new Dictionary<string,Dictionary<string,object>>();var a=J.Arr(J.Get(s,k));Require(a.Count<=10000,"too_many_records");foreach(var o in a){var r=J.Obj(o);Id(J.Get(r,"id"));string id=(string)r["id"];Require(!map.ContainsKey(id),"duplicate_id");map[id]=r;}return map;}
 public static void Validate(Dictionary<string,object>s){
  Require(J.Num(J.Get(s,"schemaVersion"))==1,"unsupported_schema");Range(s,"revision",0,Int32.MaxValue-1);Require(J.Get(s,"initialized") is bool,"invalid_initialized");
  var root=new[]{"schemaVersion","revision","initialized","updatedAt","operationIds","projects","tasks","plans"};Require(s.Keys.All(root.Contains),"unknown_state_field");
  if(J.Get(s,"updatedAt")!=null){DateTimeOffset x;Require(J.Get(s,"updatedAt") is string&&DateTimeOffset.TryParse((string)s["updatedAt"],CultureInfo.InvariantCulture,DateTimeStyles.None,out x),"invalid_timestamp");}
  var ops=J.Arr(J.Get(s,"operationIds"));Require(ops.Count<=100000,"too_many_operations");foreach(var o in ops)Id(o);Require(ops.Distinct().Count()==ops.Count,"duplicate_operation_ids");
  var p=Map(s,"projects");var t=Map(s,"tasks");var plans=Map(s,"plans");
  foreach(var col in Collections)foreach(var r in Map(s,col).Values){Require(r.Keys.All(Fields[col].Contains),"unknown_or_private_field");}
  foreach(var r in p.Values){Text(r,"name",500,false);Text(r,"priority",100,false);Date(r,"deadline",true);Range(r,"weeklyMin",0,10080);}
  foreach(var r in t.Values){Text(r,"name",1000,false);Text(r,"category",200,false);Text(r,"priority",100,false);Date(r,"date",true);Range(r,"estimate",1,1440);Require(new[]{"low","normal","high"}.Contains(J.Str(J.Get(r,"goal"))),"invalid_goal");Require(J.Get(r,"done") is bool,"invalid_done");var a=J.Get(r,"actual");if(a!=null)Require((a is int||a is decimal||a is double)&&Convert.ToDouble(a)>=0&&Convert.ToDouble(a)<=10000000,"invalid_actual");
   if(J.Get(r,"completedAt")!=null){DateTimeOffset x;Require(J.Get(r,"completedAt") is string&&DateTimeOffset.TryParse((string)r["completedAt"],out x),"invalid_completedAt");}
   var pid=J.Get(r,"projectId");if(pid!=null&&J.Str(pid)!=""){Id(pid);Require(p.ContainsKey((string)pid),"project_reference");}
   var parent=J.Get(r,"parentId");var seen=new HashSet<string>{(string)r["id"]};while(parent!=null&&J.Str(parent)!=""){Id(parent);Require(t.ContainsKey((string)parent),"parent_reference");Require(seen.Add((string)parent),"task_cycle");var pr=t[(string)parent];Require(J.Equal(J.Get(pr,"projectId"),pid),"cross_project_parent");parent=J.Get(pr,"parentId");}
  }
  foreach(var r in plans.Values){Id(J.Get(r,"taskId"));Require(t.ContainsKey((string)r["taskId"]),"task_reference");Date(r,"date",false);Range(r,"start",0,1439);Range(r,"duration",1,1440);Require((int)r["start"]+(int)r["duration"]<=1440,"plan_bounds");}
 }
 public static Dictionary<string,object> Merge(Dictionary<string,object>b,Dictionary<string,object>l,Dictionary<string,object>r,out List<string> conflicts){
  Validate(b);Validate(l);Validate(r);var result=J.Copy(r);conflicts=new List<string>();
  foreach(string col in Collections){var bm=Map(b,col);var lm=Map(l,col);var rm=Map(r,col);var merged=new List<object>();
   foreach(string id in bm.Keys.Union(lm.Keys).Union(rm.Keys).OrderBy(v=>v)){
    Dictionary<string,object> bv=null,lv=null,rv=null;bm.TryGetValue(id,out bv);lm.TryGetValue(id,out lv);rm.TryGetValue(id,out rv);
    if(bv!=null&&(lv==null||rv==null)){conflicts.Add(col+"/"+id+"/deletion_disabled");continue;}
    if(lv==null){merged.Add(rv);continue;}if(rv==null){merged.Add(lv);continue;}
    var row=new Dictionary<string,object>(rv);
    foreach(string key in lv.Keys.Union(rv.Keys).Union(bv==null?new string[0]:bv.Keys.ToArray())){
     object x=bv==null?null:J.Get(bv,key),y=J.Get(lv,key),z=J.Get(rv,key);bool lc=!J.Equal(x,y),rc=!J.Equal(x,z);
     if(lc&&rc&&!J.Equal(y,z))conflicts.Add(col+"/"+id+"/"+key);else if(lc){if(lv.ContainsKey(key))row[key]=y;else row.Remove(key);}
    }merged.Add(row);
   }result[col]=merged;
  }
  if(conflicts.Count==0)Validate(result);return result;
 }
 public static bool SameData(Dictionary<string,object>a,Dictionary<string,object>b){return Collections.All(k=>J.Equal(a[k],b[k]));}
}
public sealed class Disk {
 readonly string folder;public Disk(string path){folder=path;Directory.CreateDirectory(folder);}
 public Dictionary<string,object> Load(string name){string p=Path.Combine(folder,name+".dat");if(!File.Exists(p))return null;try{return J.Obj(J.Parse(Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(p),null,DataProtectionScope.CurrentUser))));}catch{throw new Fault("local_store_corrupt_preserved");}}
 public void Save(string name,object data){string p=Path.Combine(folder,name+".dat"),tmp=p+".tmp";byte[] plain=Encoding.UTF8.GetBytes(J.Dump(data));try{byte[] encrypted=ProtectedData.Protect(plain,null,DataProtectionScope.CurrentUser);using(var f=new FileStream(tmp,FileMode.Create,FileAccess.Write,FileShare.None)){f.Write(encrypted,0,encrypted.Length);f.Flush(true);}if(File.Exists(p))File.Replace(tmp,p,p+".bak",true);else File.Move(tmp,p);}finally{Array.Clear(plain,0,plain.Length);}}
 public void Clear(string name){Save(name,new Dictionary<string,object>{{"pending",false}});}
}
public sealed class Pairing {
 public string Code=J.Random();public DateTime CodeExpiry=DateTime.UtcNow.AddMinutes(5);int attempts;DateTime window=DateTime.UtcNow;
 readonly Dictionary<string,Tuple<string,DateTime>> sessions=new Dictionary<string,Tuple<string,DateTime>>();
 public void Rotate(){lock(this){Code=J.Random();CodeExpiry=DateTime.UtcNow.AddMinutes(5);sessions.Clear();}}
 public string Pair(string code,string origin,Func<string,bool> approve){lock(this){if((DateTime.UtcNow-window).TotalMinutes>=1){attempts=0;window=DateTime.UtcNow;}if(++attempts>8)throw new Fault("pair_rate_limit");if(DateTime.UtcNow>CodeExpiry||!J.SecretEqual(code,Code))throw new Fault("pair_invalid_or_expired");if(!approve(origin))throw new Fault("pair_denied");Code=J.Random();CodeExpiry=DateTime.MinValue;string token=J.Random();sessions[token]=Tuple.Create(origin,DateTime.UtcNow.AddMinutes(15));return token;}}
 public bool Auth(string token,string origin){lock(this){Tuple<string,DateTime> s;return token!=null&&sessions.TryGetValue(token,out s)&&s.Item1==origin&&s.Item2>DateTime.UtcNow;}}
 public void ExpireForTest(){lock(this){foreach(var key in sessions.Keys.ToArray())sessions[key]=Tuple.Create(sessions[key].Item1,DateTime.MinValue);}}
}
}
