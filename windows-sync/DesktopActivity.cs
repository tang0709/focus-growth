using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
namespace FocusGrowth {
 // Never reads keystrokes, window titles or application names. Collects only 10-minute slot keys.
 public sealed class DesktopActivity:IDisposable {
  [StructLayout(LayoutKind.Sequential)]struct LastInputInfo {public uint cbSize;public uint dwTime;}
  [DllImport("user32.dll",SetLastError=true)]static extern bool GetLastInputInfo(ref LastInputInfo info);
  readonly Disk disk;readonly object gate=new object();readonly HashSet<string> slots=new HashSet<string>();
  readonly Timer timer;bool armed;DateTime expiresUtc;bool stopped;
  public DesktopActivity(Disk store){
   disk=store;var data=disk.Load("activity-evidence");
   if(data!=null){
    armed=J.Get(data,"armed") is bool&&(bool)data["armed"];
    DateTime parsed;if(DateTime.TryParse(J.Str(J.Get(data,"expiresUtc")),null,System.Globalization.DateTimeStyles.RoundtripKind,out parsed))expiresUtc=parsed.ToUniversalTime();
    object rows=J.Get(data,"slots");
    if(rows!=null)foreach(var value in J.Arr(rows)){string key=J.Str(value);if(ValidKey(key))slots.Add(key);}
   }
   if(DateTime.UtcNow>=expiresUtc)armed=false;
   timer=new Timer(_=>Sample(),null,1000,5000);
  }
  static bool ValidKey(string key){
   if(key==null||key.Length!=14||key[10]!=':')return false;
   DateTime parsed;int slot;
   return DateTime.TryParseExact(key.Substring(0,10),"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out parsed)
      &&Int32.TryParse(key.Substring(11),out slot)&&slot>=0&&slot<144;
  }
  void Persist(){
   var saved=new Dictionary<string,object>{{"armed",armed},{"expiresUtc",expiresUtc.ToString("o")},{"slots",slots.OrderBy(x=>x).Cast<object>().ToArray()}};
   disk.Save("activity-evidence",saved);
  }
  void Sample(){
   lock(gate){
    if(stopped||!armed)return;
    if(DateTime.UtcNow>=expiresUtc){armed=false;Persist();return;}
    var info=new LastInputInfo{cbSize=(uint)Marshal.SizeOf(typeof(LastInputInfo))};
    if(!GetLastInputInfo(ref info))return;
    uint idle=unchecked((uint)Environment.TickCount-info.dwTime);
    // Poll every 5 seconds. An input within the previous 7 seconds counts once in its 10-minute slot.
    if(idle>7000)return;
    var observed=DateTime.Now.AddMilliseconds(-idle);
    if(observed>DateTime.Now.AddMinutes(1))return;
    string key=observed.ToString("yyyy-MM-dd")+":"+(observed.Hour*6+observed.Minute/10).ToString("000");
    if(slots.Add(key)){
     string cutoff=DateTime.Now.AddDays(-14).ToString("yyyy-MM-dd");
     slots.RemoveWhere(x=>String.CompareOrdinal(x.Substring(0,10),cutoff)<0);
     Persist();
    }
   }
  }
  object View(){
   return new{supported=true,armed=armed&&DateTime.UtcNow<expiresUtc,expiresUtc=expiresUtc.ToString("o"),
    slots=slots.OrderBy(x=>x).Select(x=>new{date=x.Substring(0,10),slot=Int32.Parse(x.Substring(11))}).ToArray()};
  }
  public object Snapshot(){lock(gate){return View();}}
  public object Arm(){lock(gate){armed=true;expiresUtc=DateTime.UtcNow.AddHours(12);Persist();return View();}}
  public object Stop(){lock(gate){armed=false;Persist();return View();}}
  public void Dispose(){lock(gate){stopped=true;armed=false;timer.Dispose();Persist();}}
 }
}
