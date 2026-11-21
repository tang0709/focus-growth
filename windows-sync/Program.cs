using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Generic;
namespace FocusGrowth {
static class Program {
 [STAThread]static void Main(string[] args){
  string root=AppDomain.CurrentDomain.BaseDirectory;
  if(args.Length>0&&args[0]=="--selftest"){Tests.Run(root);return;}
  if(args.Length>0&&args[0]=="--cloud-test"){Tests.CloudTest(root);return;}
  if(args.Length>0){MessageBox.Show("Production synchronization is locked. Synthetic mode only.");return;}
  Application.EnableVisualStyles();
  try{string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusGrowthSync","synthetic-v1");var disk=new Disk(folder);var config=disk.Load("workspace");if(config==null){config=new Dictionary<string,object>{{"id","workspace-"+Guid.NewGuid().ToString("N")}};disk.Save("workspace",config);}string path="sync/tests/"+(string)config["id"]+".json";
  using(var mutex=new Mutex(false,"Local\\FocusGrowthSync-Synthetic-18417")){if(!mutex.WaitOne(0)){MessageBox.Show("Focus Growth companion is already running.");return;}
  using(var cloud=new Cloud(path)){
   cloud.Verify();var engine=new Engine(cloud,disk);var pair=new Pairing();
   using(var form=new Form{Text="Focus & Growth - Synthetic Sync Companion",Width=730,Height=355,StartPosition=FormStartPosition.CenterScreen}){
    var note=new Label{Left=20,Top=15,Width=670,Height=62,Text="SYNTHETIC TEST MODE ONLY - production sync/state.json is locked.\nPair only a reviewed Focus & Growth page. Session lasts 15 minutes.\nA Windows approval will be required after entering the code in the page."};
    var code=new TextBox{Left=20,Top=85,Width=500,ReadOnly=true,Text=pair.Code};
    var rotate=new Button{Left=535,Top=83,Width=150,Text="New pairing code"};rotate.Click+=(s,e)=>{pair.Rotate();code.Text=pair.Code;};
    var open=new Button{Left=20,Top=130,Width=310,Text="Open local synthetic workbench"};open.Click+=(s,e)=>Process.Start(Bridge.Local+"/?syncTest=1");
    var status=new Label{Left=20,Top=180,Width=670,Height=50};
    var pathLabel=new TextBox{Left=20,Top=245,Width=665,ReadOnly=true,Text=path};
    var stop=new Button{Left=535,Top=130,Width=150,Text="Stop service"};stop.Click+=(s,e)=>form.Close();
    form.Controls.AddRange(new Control[]{note,code,rotate,open,status,pathLabel,stop});
    using(var bridge=new Bridge(engine,pair,Path.Combine(root,"web"),origin=>(bool)form.Invoke(new Func<bool>(()=>MessageBox.Show(form,"Allow "+origin+" to access SYNTHETIC task metadata for 15 minutes?\nNo GitHub token will be shared.\nNever pair an unreviewed or unexpected page.","Confirm local pairing",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes)))){
     bridge.Start();using(var poll=new System.Threading.Timer(_=>engine.Sync(false),null,0,30000))using(var timer=new System.Windows.Forms.Timer{Interval=1000}){timer.Tick+=(s,e)=>{status.Text=engine.Status+"  "+(engine.Error??"")+"\nLast successful sync: "+(engine.LastSuccess??"never");};timer.Start();Application.Run(form);}
    }
   }
  }mutex.ReleaseMutex();}
  }catch(Fault ex){MessageBox.Show("Companion stopped safely: "+ex.Code+". No production data was changed.");}catch{MessageBox.Show("Companion could not start. Check loopback port, Windows user and local files. No secrets were logged.");}
 }
}
}
