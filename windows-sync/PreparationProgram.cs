using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
namespace FocusGrowth {
public static class PreparationProgram {
 public static void BackupExport(string root,string path){
  // Explicit export from the already observed normal Pages tab. Never changes the source file.
  byte[] bytes=File.ReadAllBytes(path);var vault=new LocalVault(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusGrowthSync","preparation-v031","backups"));
  var backup=vault.SaveVerified("browser-export",bytes);var restored=LocalVault.Read(backup);
  try{var db=J.Obj(LocalJson.Parse(System.Text.Encoding.UTF8.GetString(restored)));PreparationModel.ValidateFull(db);Schema.Require(!(J.Get(db,"demo") is bool&&(bool)db["demo"]),"demo_workspace_not_production");backup["jsonAndAppValidation"]="PASS";backup["sourceFile"]=Path.GetFullPath(path);backup["sourceFileModifiedUtc"]=File.GetLastWriteTimeUtc(path).ToString("o");backup["source"]=new{origin=Bridge.Pages,pathname="/focus-growth/",storageKey="start-workbench-v1",method="explicit export from observed normal browser tab; live-key comparison pending"};backup["counts"]=Schema.Collections.ToDictionary(c=>c,c=>J.Arr(db[c]).Count);backup["activityCount"]=J.Arr(db["activity"]).Count;backup["notesPresent"]=J.Arr(db["notes"]).Count>0;File.WriteAllText(Path.Combine(root,"PRODUCTION-BROWSER-BACKUP.json"),LocalJson.Dump(backup),new System.Text.UTF8Encoding(false));}
  finally{Array.Clear(bytes,0,bytes.Length);Array.Clear(restored,0,restored.Length);}
 }
 public static void CaptureMetadata(string root){var v=new LocalVault(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusGrowthSync","preparation-v031","backups"));var result=v.CaptureMetadata(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"FocusGrowthSync","synthetic-v1"),root);string previous=Path.GetFullPath(Path.Combine(root,"..","FocusGrowthSync-synthetic.zip"));if(File.Exists(previous))result["previousSourceBundle"]=v.SaveVerified("v03-source-bundle",File.ReadAllBytes(previous));File.WriteAllText(Path.Combine(root,"PRODUCTION-METADATA-BACKUP.json"),LocalJson.Dump(result),new System.Text.UTF8Encoding(false));}
 public static void Run(string root){Application.EnableVisualStyles();try{using(var service=new PreparationService(root))using(var f=new Form{Text="Focus & Growth V0.3.1 — Preparation ONLY",Width=820,Height=345,StartPosition=FormStartPosition.CenterScreen}){
  var pairing=new Pairing();var notice=new Label{Left=20,Top=12,Width=760,Height=70,Text=ProductionPolicy.Notice+"\nRead-only production preparation on 127.0.0.1:18418.\nPair only the NORMAL Pages workspace. A second consent has not been granted."};
  var code=new TextBox{Left=20,Top=90,Width=545,ReadOnly=true,Text=pairing.Code};var rotate=new Button{Left=580,Top=88,Width=190,Text="New preparation code"};rotate.Click+=(s,e)=>{pairing.Rotate();code.Text=pairing.Code;};
  var open=new Button{Left=20,Top=135,Width=320,Text="Open normal Pages workspace"};open.Click+=(s,e)=>Process.Start("https://tang0709.github.io/focus-growth/");
  var details=new Button{Left=360,Top=135,Width=200,Text="View local difference preview"};details.Click+=(s,e)=>service.ShowDetails(f);
  var stop=new Button{Left=580,Top=135,Width=190,Text="Stop preparation service"};stop.Click+=(s,e)=>f.Close();
  var status=new Label{Left=20,Top=185,Width=760,Height=85,Text="Waiting for an explicit backup and preview request from the normal workspace.\nNo production upload endpoint is enabled."};
  f.Controls.AddRange(new Control[]{notice,code,rotate,open,details,stop,status});
  service.Ready=()=>f.BeginInvoke(new Action(()=>{status.Text=J.Str(service.LastSummary["conclusion"])+"\nPreview digest: "+service.LastSummary["previewDigest"]+"\n"+ProductionPolicy.Notice;service.ShowDetails(f);}));
  using(var bridge=new Bridge(null,pairing,Path.Combine(root,"web"),origin=>(bool)f.Invoke(new Func<bool>(()=>MessageBox.Show(f,"Allow preparation-only pairing for "+origin+"?\nOnly local encrypted backup and read-only comparison are allowed.\nNo production data will be uploaded.","Read-only preparation pairing",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes)),service)){bridge.Start();Application.Run(f);}
 }}catch(Fault e){MessageBox.Show("Preparation stopped safely: "+e.Code+"\n"+ProductionPolicy.Notice);}catch{MessageBox.Show("Preparation could not start. Check port 18418, current Windows user and local files.\n"+ProductionPolicy.Notice);}}
}
}
