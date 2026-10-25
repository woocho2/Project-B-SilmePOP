$ErrorActionPreference='Stop'
$stage=$PSScriptRoot
$project='C:\Users\user\Desktop\Programing\Project-B-SilmePOP'
$utf8=[Text.UTF8Encoding]::new($false)
$themes=@('Basic','Spring','Summer','Autumn','Winter','Sunny','Night','Cloudy')
$manifest=[Collections.Generic.List[object]]::new()
function Save($path,[string]$content){
  $dest=Join-Path $stage ('payload/'+$path);[IO.Directory]::CreateDirectory((Split-Path $dest)) | Out-Null
  [IO.File]::WriteAllText($dest,$content,$utf8)
}
function GuidOf($path){return [regex]::Match([IO.File]::ReadAllText((Join-Path $project ($path+'.meta'))),'(?m)^guid: (\w+)').Groups[1].Value}
function RefOf($path){
  $meta=[IO.File]::ReadAllText((Join-Path $project ($path+'.meta')))
  $id='21300000'
  if($meta -match '(?m)^  spriteMode: 2\s*$'){$id=[regex]::Match($meta,'(?m)^      internalID: (-?\d+)').Groups[1].Value}
  if(-not $id){throw ('Missing sprite ID: '+$path)}
  return ('{fileID: '+$id+', guid: '+(GuidOf $path)+', type: 3}')
}
$bindings=[ordered]@{}
$migrations=[ordered]@{}
function MapSprite($name,$oldGuid,$oldId,$newRef){$bindings[$name]=$newRef;$migrations[('{fileID: '+$oldId+', guid: '+$oldGuid+', type: 3}')]=$newRef}
$mapRoles=@{MenuPaper='Menu';Table='Table';Mascot='Slime';Bar='Bar'}
$atlasGuids=@{MenuPaper='ea2fffb35494d8022250fe965c5b70c3';Table='9da566edf21d79f2fe62b334fe023615';Mascot='4f27bb85d4049310773bf44972c12739';Bar='398f14b849b0429ae49c986158c83084'}
for($i=0;$i -lt 8;$i++){
  $theme=$themes[$i]
  foreach($role in @('MenuPaper','Table','Mascot','Bar')){
    $folder=$mapRoles[$role]
    $path=if($role -eq 'Mascot'){'Assets/4. SlimeAsset/Map/Slime/Slime_'+$theme+'.png'}else{'Assets/4. SlimeAsset/Map/'+$folder+'/'+$theme+'_'+$folder+'.png'}
    MapSprite ('Map_'+$theme+'_'+$role) $atlasGuids[$role] (21300001+$i) (RefOf $path)
  }
  $bindings['Map_'+$theme+'_Menu']=('{fileID: '+(21300001+$i)+', guid: be1f998f6310bd8dc6d1ba9ede610608, type: 3}')
}
$originals=@{Body='Slime/Slime.png';Face='Slime/Face_1.png';Crown='Slime/Costume_King.png';Horns='Slime/Costume_Demon.png';Angel='Slime/Costume_Angel.png';Magnifier='UI/Magnifier.png';Mix='UI/Mix.png';Eraser='UI/Destroy.png';Gear='UI/ButtonIcon_option.png';Close='UI/ButtonIcon_quit.png'}
$names=@('Body','Face','Crown','Horns','Angel','Magnifier','Mix','Eraser','Pause','Gear','Close','Home','Mascot','Star','Hourglass','Help')
for($i=0;$i -lt $names.Count;$i++){
  $name=$names[$i];$ref=('{fileID: '+(21300001+$i)+', guid: 0558184fb7f9dcf213b7a1d2a0969b6a, type: 3}')
  if($originals.ContainsKey($name)){$ref=RefOf ('Assets/4. SlimeAsset/'+$originals[$name])}
  MapSprite $name '0558184fb7f9dcf213b7a1d2a0969b6a' (21300001+$i) $ref
}
$bindings.Logo=RefOf 'Assets/4. SlimeAsset/UI/SLIME POP !!.png'
$bindings.Notebook=RefOf 'Assets/4. SlimeAsset/UI/BackGround_1.png'
foreach($name in @('Logo','Notebook')){$old=GuidOf ('Assets/Resources/DoodleLab/'+$name+'.png');$migrations[('{fileID: 21300000, guid: '+$old+', type: 3}')]=$bindings[$name]}
$frames=[IO.File]::ReadAllText((Join-Path $project 'Assets/Resources/DoodleLab/Frames.png.meta'))
foreach($m in [regex]::Matches($frames,'(?s)      name: ([^\r\n]+).*?      internalID: (-?\d+)')){$bindings[$m.Groups[1].Value]=('{fileID: '+$m.Groups[2].Value+', guid: '+(GuidOf 'Assets/Resources/DoodleLab/Frames.png')+', type: 3}')}
# Keep the purpose-specific atlases available in SlimeAsset, preserving GUIDs when relocating them.
foreach($file in Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Resources/DoodleLab') -File){
  $dest=Join-Path $stage ('payload/Assets/4. SlimeAsset/Atlases/'+$file.Name)
  [IO.Directory]::CreateDirectory((Split-Path $dest)) | Out-Null
  Copy-Item -LiteralPath $file.FullName -Destination $dest
}
Save 'Assets/4. SlimeAsset/Atlases.meta' "fileFormatVersion: 2`nguid: 95514733987f4051b4f1da86cde18fee`nfolderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
# Direct original bar/table sprites need nine-slice borders for the existing timer and panels.
foreach($theme in $themes){
  foreach($role in @('Bar','Table')){
    $path='Assets/4. SlimeAsset/Map/'+$role+'/'+$theme+'_'+$role+'.png.meta'
    $meta=[IO.File]::ReadAllText((Join-Path $project $path))
    $border=if($role -eq 'Bar'){'{x: 700, y: 200, z: 200, w: 200}'}else{'{x: 384, y: 384, z: 384, w: 384}'}
    $meta=[regex]::Replace($meta,'(?m)^      border: \{.*?\}',('      border: '+$border))
    $limit=if($role -eq 'Bar'){8192}else{4096}
    $meta=[regex]::Replace($meta,'(?m)^(\s*maxTextureSize: )\d+',('${1}'+$limit))
    $meta=[regex]::Replace($meta,'(?m)^(\s*textureCompression: )\d+','${1}0')
    $meta=[regex]::Replace($meta,'(?m)^  spriteMeshType: \d+','  spriteMeshType: 0')
    Save $path $meta
  }
}
foreach($name in @('Slime','Face_1','Costume_King','Costume_Demon','Costume_Angel')){
  $path='Assets/4. SlimeAsset/Slime/'+$name+'.png.meta'
  $meta=[IO.File]::ReadAllText((Join-Path $project $path))
  $meta=[regex]::Replace($meta,'(?m)^(\s*maxTextureSize: )\d+','${1}4096')
  $meta=[regex]::Replace($meta,'(?m)^(\s*textureCompression: )\d+','${1}0')
  Save $path $meta
}
# Repoint every serialized sprite reference to an original SlimeAsset sprite where one exists.
$extensions=@('.asset','.prefab','.unity','.mat','.anim','.controller','.overrideController','.playable')
foreach($file in Get-ChildItem -LiteralPath (Join-Path $project 'Assets') -Recurse -File){
  if($file.Extension -notin $extensions){continue}
  $before=[IO.File]::ReadAllText($file.FullName);$after=$before
  foreach($entry in $migrations.GetEnumerator()){$after=$after.Replace($entry.Key,$entry.Value)}
  $relative=$file.FullName.Substring($project.Length+1).Replace('\','/')
  if($after -ne $before){Save $relative $after}
}
for($i=0;$i -lt 8;$i++){
  $path='Assets/2. Prefab/'+($i+1)+'. '+$themes[$i]+'.asset'
  $dest=Join-Path $stage ('payload/'+$path)
  $text=[IO.File]::ReadAllText($dest)
  $text=$text.TrimEnd()+"`n  BarSprite: "+$bindings['Map_'+$themes[$i]+'_Bar']+"`n  OptionPanelSprite: "+$bindings['Map_'+$themes[$i]+'_Menu']+"`n"
  Save $path $text
}
$catalogGuid='54afbb273353449ea5326c939449d1c8';$catalogScript='6cfbc4aed25848f4a01e788095d15792';$bindingScript='4f410c214f244d19a9392f988ca604d4'
foreach($name in @('DoodleSkinAssets','DoodleSkinBindings')){
  $guid=if($name -eq 'DoodleSkinAssets'){$catalogScript}else{$bindingScript}
  Save ('Assets/3. Scripts/'+$name+'.cs') ([IO.File]::ReadAllText((Join-Path $stage ($name+'.cs'))))
  Save ('Assets/3. Scripts/'+$name+'.cs.meta') "fileFormatVersion: 2`nguid: $guid`nMonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {instanceID: 0}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
}
$catalog="%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!114 &11400000`nMonoBehaviour:`n  m_ObjectHideFlags: 0`n  m_CorrespondingSourceObject: {fileID: 0}`n  m_PrefabInstance: {fileID: 0}`n  m_PrefabAsset: {fileID: 0}`n  m_GameObject: {fileID: 0}`n  m_Enabled: 1`n  m_EditorHideFlags: 0`n  m_Script: {fileID: 11500000, guid: $catalogScript, type: 3}`n  m_Name: SlimeSkinAssets`n  m_EditorClassIdentifier: `n  sprites:`n"
foreach($binding in $bindings.GetEnumerator()){$catalog+='  - name: '+$binding.Key+"`n    sprite: "+$binding.Value+"`n"}
Save 'Assets/4. SlimeAsset/SlimeSkinAssets.asset' $catalog
Save 'Assets/4. SlimeAsset/SlimeSkinAssets.asset.meta' "fileFormatVersion: 2`nguid: $catalogGuid`nNativeFormatImporter:`n  externalObjects: {}`n  mainObjectFileID: 11400000`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
foreach($scene in Get-ChildItem -LiteralPath (Join-Path $project 'Assets/1. Scenes') -Filter '*.unity'){
  $path='Assets/1. Scenes/'+$scene.Name;$dest=Join-Path $stage ('payload/'+$path)
  $text=if(Test-Path -LiteralPath $dest){[IO.File]::ReadAllText($dest)}else{[IO.File]::ReadAllText($scene.FullName)}
  if($text.Contains('&900000000001')){throw 'Scene ID collision'}
  $text=[regex]::Replace($text,'(?m)^  m_Roots:\r?\n',"  m_Roots:`n  - {fileID: 900000000002}`n")
  $root=@"
--- !u!1 &900000000001
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 900000000002}
  - component: {fileID: 900000000003}
  m_Layer: 0
  m_Name: SlimeSkinBindings
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &900000000002
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000000001}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &900000000003
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 900000000001}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $bindingScript, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
  assets: {fileID: 11400000, guid: $catalogGuid, type: 2}
"@
  Save $path ($text+"`n"+$root+"`n")
}
Write-Output ('Prepared direct catalog: '+$bindings.Count+' aliases; original sprite mappings, theme assets, scenes and atlas relocation.')
