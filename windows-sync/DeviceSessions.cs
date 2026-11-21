using System;using System.Linq;using System.Text;using System.Collections.Generic;using System.Security.Cryptography;
namespace FocusGrowth {
// Persistent record contains a browser public key, never its private key or a bearer secret.
// Browser private keys must be non-extractable WebCrypto keys stored in IndexedDB.
public sealed class DeviceSessions {
 readonly Disk disk;readonly Func<DateTime> now;readonly Dictionary<string,object> records;
 readonly Dictionary<string,Tuple<string,string,string,DateTime>> challenges=new Dictionary<string,Tuple<string,string,string,DateTime>>();
 public DeviceSessions(Disk store,Func<DateTime> clock=null){disk=store;now=clock??(()=>DateTime.UtcNow);var saved=disk.Load("device-sessions-v1");records=saved??new Dictionary<string,object>();}
 void Save(){disk.Save("device-sessions-v1",records);}
 static byte[] Decode(string s){s=s.Replace('-','+').Replace('_','/');return Convert.FromBase64String(s.PadRight((s.Length+3)/4*4,'='));}
 static RSAParameters Public(string n,string e){var p=new RSAParameters{Modulus=Decode(n),Exponent=Decode(e)};Schema.Require(p.Modulus.Length==256&&p.Exponent.SequenceEqual(new byte[]{1,0,1}),"session_key_rejected");return p;}
 Dictionary<string,object> Valid(string id,string origin){object v;Schema.Require(records.TryGetValue(id,out v),"session_unknown");var r=J.Obj(v);Schema.Require(J.Str(r["origin"])==origin,"session_origin_mismatch");Schema.Require(!(bool)r["revoked"],"session_revoked");Schema.Require(J.Num(J.Get(r,"trustVersion"))==2,"session_reauthentication_required");return r;}
 // Enroll is called only after one-time native pairing approval, not by public resume routes.
 public string Enroll(string origin,string modulus,string exponent){lock(this){Public(modulus,exponent);Schema.Require(origin==Bridge.Pages||origin=="http://127.0.0.1:18420","session_origin_rejected");string id=J.Random();var t=now();records[id]=new Dictionary<string,object>{{"origin",origin},{"n",modulus},{"e",exponent},{"trustVersion",2},{"createdUtc",t.ToString("o")},{"activeUtc",t.ToString("o")},{"revoked",false}};Save();return id;}}
 public string Challenge(string id,string origin,string purpose){lock(this){Valid(id,origin);Schema.Require(purpose=="resume"||purpose=="activity","session_purpose_rejected");foreach(var k in challenges.Where(x=>x.Value.Item4<=now()).Select(x=>x.Key).ToArray())challenges.Remove(k);Schema.Require(challenges.Count<64,"session_challenge_limit");string nonce=J.Random();challenges[nonce]=Tuple.Create(id,origin,purpose,now().AddSeconds(30));return nonce;}}
 public static string Message(string id,string origin,string purpose,string nonce){return "FocusGrowth/session/v1\n"+id+"\n"+origin+"\n"+purpose+"\n"+nonce;}
 public object Prove(string id,string origin,string purpose,string nonce,string signature){lock(this){var r=Valid(id,origin);Tuple<string,string,string,DateTime> c;Schema.Require(challenges.TryGetValue(nonce,out c),"session_challenge_missing");challenges.Remove(nonce);Schema.Require(c.Item1==id&&c.Item2==origin&&c.Item3==purpose&&c.Item4>now(),"session_challenge_mismatch_or_expired");bool ok=false;try{using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.ImportParameters(Public((string)r["n"],(string)r["e"]));ok=rsa.VerifyData(Encoding.UTF8.GetBytes(Message(id,origin,purpose,nonce)),CryptoConfig.MapNameToOID("SHA256"),Decode(signature));}}catch{ok=false;}if(!ok){r["revoked"]=true;Save();}Schema.Require(ok,"session_signature_rejected");if(purpose=="activity"){r["activeUtc"]=now().ToString("o");Save();}return Info(id,origin);}}
 public object Info(string id,string origin){lock(this){var r=Valid(id,origin);return new{trusted=true,createdUtc=r["createdUtc"],expiresUtc=(string)null};}}
 public bool Active(string id,string origin){lock(this){try{Valid(id,origin);return true;}catch(Fault){return false;}}}
 public bool AnyActive(){lock(this){return records.Any(x=>Active(x.Key,J.Str(J.Obj(x.Value)["origin"])));}}
 public object[] List(){lock(this){return records.Select(x=>{var r=J.Obj(x.Value);return (object)new{id=x.Key,origin=J.Str(r["origin"]),active=Active(x.Key,J.Str(r["origin"])),createdUtc=J.Get(r,"createdUtc")};}).ToArray();}}
 public void Revoke(string id,string origin){lock(this){object value;Schema.Require(records.TryGetValue(id,out value),"session_unknown");var r=J.Obj(value);Schema.Require((string)r["origin"]==origin,"session_origin_mismatch");r["revoked"]=true;Save();}}
 public void RevokeAll(){lock(this){foreach(var value in records.Values)J.Obj(value)["revoked"]=true;challenges.Clear();Save();}}
}
}
