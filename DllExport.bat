@echo off
:: Copyright (c) 2016-2017  Denis Kuzmin [ entry.reg@gmail.com ] :: github.com/3F
:: https://github.com/3F/DllExport
:: ---
:: Based on hMSBuild logic and includes GetNuTool core.
:: https://github.com/3F/hMSBuild
:: https://github.com/3F/GetNuTool
setlocal enableDelayedExpansion
set "aa=1.6.0-beta2"
set "wAction="
set "ab=DllExport"
set "ac=tools/net.r_eg.DllExport.Wizard.targets"
set "ad=packages"
set "ngserver=https://www.nuget.org/api/v2/package/"
set "ae=build_info.txt"
set "wRootPath=%cd%"
set /a af=0
set /a ag=0
set "ah="
set ai=0
set aj=2
set ak=3
set "al=%* "
set am=%al:"=%
set an=%am%
set an=%an:-help =%
set an=%an:-h =%
set an=%an:-? =%
if not "%am%"=="%an%" goto a2
goto a3
:a2
echo.
@echo DllExport - v1.6.0.62812 [ c897b99 ]
@echo Copyright (c) 2009-2015  Robert Giesecke
@echo Copyright (c) 2016-2017  Denis Kuzmin [ entry.reg@gmail.com :: github.com/3F ]
echo.
echo Distributed under the MIT license
@echo https://github.com/3F/DllExport
echo Wizard - based on hMSBuild logic and includes GetNuTool core - https://github.com/3F
echo.
@echo.
@echo Usage: DllExport [args to DllExport] [args to GetNuTool core]
echo ------
echo.
echo Arguments:
echo ----------
echo  -action {type}        - Specified action for Wizard. Where {type}:
echo                           * Configure - To configure DllExport for specific projects.
echo                           * Update    - To update pkg reference for already configured projects.
echo                           * Restore   - To restore configured DllExport.
echo.
echo  -sln-dir {path}       - Path to directory with .sln files to be processed.
echo  -sln-file {path}      - Optional predefined .sln file to process via the restore operations etc.
echo  -metalib {path}       - Relative path from PkgPath to DllExport meta library.
echo  -dxp-target {path}    - Relative path to .target file of the DllExport.
echo  -dxp-version {num}    - Specific version of DllExport. Where {num}:
echo                           * Versions: 1.6.0 ...
echo                           * Keywords: 
echo                             `actual` to use unspecified local version or to get latest available;
echo.
echo  -msb {path}           - Full path to specific msbuild.
echo  -packages {path}      - A common directory for packages.
echo  -server {url}         - Url for searching remote packages.
echo  -wz-target {path}     - Relative path to .target file of the Wizard.
echo  -eng                  - Try to use english language for all build messages.
echo  -GetNuTool {args}     - Access to GetNuTool core. https://github.com/3F/GetNuTool
echo  -debug                - To show additional information from hMSBuild.
echo  -version              - Display version of hMSBuild.
echo  -build-info           - Display build information of DllExport.
echo  -help                 - Display this help. Aliases: -help -h -?
echo.
echo. 
echo -------- 
echo Samples:
echo -------- 
echo  DllExport -action Configure
echo  DllExport -action Restore -sln-file "Conari.sln"
echo.
echo  DllExport -build-info
echo  DllExport -restore -sln-dir -sln-dir ..\ -debug
echo.
echo  DllExport -GetNuTool -unpack
echo  DllExport -GetNuTool /p:ngpackages="Conari;regXwild"
echo.
echo ---------------------
echo Possible Error Codes: ERROR_FILE_NOT_FOUND (0x2), ERROR_PATH_NOT_FOUND (0x3), ERROR_SUCCESS (0x0)
echo ---------------------
echo.
exit /B 0
:a3
call :a4 al _is
if [!_is!]==[1] (
if defined wAction goto a5
goto a2
)
set /a ao=1 & set ap=15
:a6
if "!al:~0,8!"=="-action " (
call :a7 %1 & shift
set "wAction=%2"
call :a7 %2 & shift
)
if "!al:~0,9!"=="-sln-dir " (
call :a7 %1 & shift
set wSlnDir=%2
call :a7 %2 & shift
)
if "!al:~0,10!"=="-sln-file " (
call :a7 %1 & shift
set wSlnFile=%2
call :a7 %2 & shift
)
if "!al:~0,9!"=="-metalib " (
call :a7 %1 & shift
set wMetaLib=%2
call :a7 %2 & shift
)
if "!al:~0,12!"=="-dxp-target " (
call :a7 %1 & shift
set wDxpTarget=%2
call :a7 %2 & shift
)
if "!al:~0,13!"=="-dxp-version " (
call :a7 %1 & shift
set aa=%2
echo Selected new DllExport version: !aa!
call :a7 %2 & shift
)
if "!al:~0,5!"=="-msb " (
call :a7 %1 & shift
set ah=%2
call :a7 %2 & shift
)
if "!al:~0,10!"=="-packages " (
call :a7 %1 & shift
set ad=%2
call :a7 %2 & shift
)
if "!al:~0,8!"=="-server " (
call :a7 %1 & shift
set ngserver=%2
call :a7 %2 & shift
)
if "!al:~0,11!"=="-wz-target " (
call :a7 %1 & shift
set ac=%2
call :a7 %2 & shift
)
if "!al:~0,5!"=="-eng " (
call :a7 %1 & shift
chcp 437 >nul
)
if "!al:~0,11!"=="-GetNuTool " (
call :a7 %1 & shift
goto a8
)
if "!al:~0,7!"=="-debug " (
call :a7 %1 & shift
set /a af=1
)
if "!al:~0,9!"=="-version " (
@echo DllExport - v1.6.0.62812 [ c897b99 ]
exit /B 0
)
if "!al:~0,12!"=="-build-info " (
call :a7 %1 & shift
set /a ag=1
)
set /a "ao+=1"
if !ao! LSS %ap% goto a6
goto a5
:a7
set al=!!al:%1 ^=!!
call :a9 al
set "al=!al! "
exit /B 0
:a5
call :a_ "dxpName = '%ab%'"
call :a_ "dxpVersion = '!aa!'"
if defined aa (
if "!aa!"=="actual" (
set "aa="
)
)
call :a9 ad
set "ad=!ad!\\"
set "aq=!ab!"
set "wPkgPath=!ad!!ab!"
if defined aa (
set "aq=!aq!/!aa!"
set "wPkgPath=!wPkgPath!.!aa!"
)
if "!ag!"=="1" (
call :a_ "buildInfo = '!wPkgPath!\\!ae!'"
if not exist "!wPkgPath!\\!ae!" (
echo information about build is not available.
exit /B %aj%
)
type "!wPkgPath!\\!ae!"
exit /B 0
)
set ar="!wPkgPath!\\!ac!"
call :a_ "dxpTarget = '!ar!'"
if not exist !ar! (
if exist "!wPkgPath!" (
call :a_ "Wizard was not found. Trying to replace obsolete version '!wPkgPath!' ..."
rmdir /S/Q "!wPkgPath!"
)
call :a_ "_remoteUrl = '!aq!'"
if "!af!"=="1" (
call :ba /p:ngpackages="!aq!" /p:ngpath="!ad!"
) else (
call :ba /p:ngpackages="!aq!" /p:ngpath="!ad!" >nul
)
)
if not exist !ar! (
echo Something went wrong. Try to use another keys.
exit /B %aj%
)
call :a_ "-sln-dir = '!wSlnDir!'"
call :a_ "-sln-file = '!wSlnFile!'"
call :a_ "-metalib = '!wMetaLib!'"
call :a_ "-dxp-target = '!wDxpTarget!'"
call :a_ "-server = '!ngserver!'"
call :a_ "wRootPath = !wRootPath!"
call :a_ "wAction = !wAction!"
if defined ah (
call :a_ "Use specific MSBuild tools '!ah!'"
set as=!ah!
goto bb
)
call :bc
if "!ERRORLEVEL!"=="0" goto bb
echo MSBuild tools was not found. Try with `-msb` key. `-help` for details.
exit /B %aj%
:bb
call :a4 as _is
if [!_is!]==[1] (
echo Something went wrong. Use `-debug` key for details.
exit /B %aj%
)
set at="!as!"
call :a_ "Target: !at! !ar!"
!at! /nologo /v:m /m:4 !ar!
exit /B 0
:bc
call :a_ "trying via MSBuild tools from .NET Framework - .net 4.0, ..."
for %%v in (4.0, 3.5, 2.0) do (
call :bd %%v Y & if [!Y!]==[1] exit /B 0
)
call :a_ "msbnetf: unfortunately we didn't find anything."
exit /B %aj%
:bd
call :a_ "checking of version: %1"
for /F "usebackq tokens=2* skip=2" %%a in (
`reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\%1" /v MSBuildToolsPath 2^> nul`
) do if exist %%b (
call :a_ "found: %%b"
set as=%%b
call :be
set /a %2=1
exit /B 0
)
set /a %2=0
exit /B 0
:a8
call :a_ "direct access to GetNuTool..."
call :ba !al!
exit /B 0
:be
set as=!as!\MSBuild.exe
exit /B 0
:a_
if "!af!"=="1" (
set au=%1
set au=!au:~0,-1! 
set au=!au:~1!
echo.[%TIME% ] !au!
)
exit /B 0
:a9
call :aw %%%1%%
set %1=%av%
exit /B 0
:aw
set "av=%*"
exit /B 0
:a4
setlocal enableDelayedExpansion
set "aw=!%1!"
if not defined aw endlocal & set /a %2=1 & exit /B 0
set aw=%aw: =%
set "aw= %aw%"
if [^%aw:~1,1%]==[] endlocal & set /a %2=1 & exit /B 0
endlocal & set /a %2=0
exit /B 0
:ba
setlocal disableDelayedExpansion 
@echo off
:: GetNuTool - Executable version
:: Copyright (c) 2015-2017  Denis Kuzmin [ entry.reg@gmail.com ]
:: https://github.com/3F/GetNuTool
set ax=gnt.core
set ay="%temp%\%random%%random%%ax%"
set "al=%* "
set a=%al:~0,30%
set a=%a:"=%
if "%a:~0,8%"=="-unpack " goto bf
if "%a:~0,9%"=="-msbuild " goto bg
for %%v in (4.0, 14.0, 12.0, 3.5, 2.0) do (
for /F "usebackq tokens=2* skip=2" %%a in (
`reg query "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\MSBuild\ToolsVersions\%%v" /v MSBuildToolsPath 2^> nul`
) do if exist %%b (
set az="%%b\MSBuild.exe"
goto bh
)
)
echo MSBuild was not found, try: gnt -msbuild "fullpath" args 1>&2
exit /B 2
:bg
call :bi %1
shift
set az=%1
call :bi %1
:bh
call :bj
%az% %ay% /nologo /p:wpath="%~dp0/" /v:m %al%
del /Q/F %ay%
exit /B 0
:bi
call set al=%%al:%1 ^=%%
exit /B 0
:bf
set ay="%~dp0\%ax%"
echo Generate minified version in %ay% ...
:bj
<nul set /P ="">%ay%
<nul set /P =^<!-- GetNuTool - github.com/3F/GetNuTool --^>^<!-- Copyright (c) 2015-2017  Denis Kuzmin [ entry.reg@gmail.com ] --^>^<Project ToolsVersion="4.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003"^>^<PropertyGroup^>^<ngconfig Condition="'$(ngconfig)' == ''"^>packages.config^</ngconfig^>^<ngserver Condition="'$(ngserver)' == ''"^>https://www.nuget.org/api/v2/package/^</ngserver^>^<ngpackages Condition="'$(ngpackages)' == ''"^>^</ngpackages^>^<ngpath Condition="'$(ngpath)' == ''"^>packages^</ngpath^>^</PropertyGroup^>^<Target Name="get" BeforeTargets="Build" DependsOnTargets="header"^>^<PrepareList config="$(ngconfig)" plist="$(ngpackages)" wpath="$(wpath)"^>^<Output PropertyName="plist" TaskParameter="Result"/^>^</PrepareList^>^<NGDownload plist="$(plist)" url="$(ngserver)" wpath="$(wpath)" defpath="$(ngpath)" debug="$(debug)"/^>^</Target^>^<Target Name="pack" DependsOnTargets="header"^>^<NGPack dir="$(ngin)" dout="$(ngout)" wpath="$(wpath)" vtool="$(GetNuTool)" debug="$(debug)"/^>^</Target^>^<PropertyGroup^>^<TaskCoreDllPath Condition="Exists('$(MSBuildToolsPath)\Microsoft.Build.Tasks.v$(MSBuildToolsVersion).dll')"^>$(MSBuildToolsPath)\Microsoft.Build.Tasks.v$(MSBuildToolsVersion).dll^</TaskCoreDllPath^>^<TaskCoreDllPath Condition="'$(TaskCoreDllPath)' == '' and Exists('$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll')"^>$(MSBuildToolsPath)\Microsoft.Build.Tasks.Core.dll^</TaskCoreDllPath^>^</PropertyGroup^>^<UsingTask TaskName="PrepareList" TaskFactory="CodeTaskFactory" AssemblyFile="$(TaskCoreDllPath)"^>^<ParameterGroup^>^<config Parame>> %ay%
<nul set /P =terType="System.String" Required="true"/^>^<plist ParameterType="System.String"/^>^<wpath ParameterType="System.String"/^>^<Result ParameterType="System.String" Output="true"/^>^</ParameterGroup^>^<Task^>^<Reference Include="System.Xml"/^>^<Reference Include="System.Xml.Linq"/^>^<Using Namespace="System"/^>^<Using Namespace="System.Collections.Generic"/^>^<Using Namespace="System.IO"/^>^<Using Namespace="System.Xml.Linq"/^>^<Code Type="Fragment" Language="cs"^>^<![CDATA[if(!String.IsNullOrEmpty(plist)){Result=plist;return true;}var _err=Console.Error;Action^<string,Queue^<string^>^> h=delegate(string cfg,Queue^<string^> list){foreach(var pkg in XDocument.Load(cfg).Descendants("package")){var id=pkg.Attribute("id");var version=pkg.Attribute("version");var output=pkg.Attribute("output");if(id==null){_err.WriteLine("Some 'id' does not exist in '{0}'",cfg);return;}var link=id.Value;if(version!=null){link+="/"+version.Value;}if(output!=null){list.Enqueue(link+":"+output.Value);continue;}list.Enqueue(link);}};var ret=new Queue^<string^>();foreach(var cfg in config.Split('^|',';')){var lcfg=Path.Combine(wpath,cfg??"");if(File.Exists(lcfg)){h(lcfg,ret);}else{_err.WriteLine(".config '{0}' was not found.",lcfg);}}if(ret.Count ^< 1){_err.WriteLine("List of packages is empty. Use .config or /p:ngpackages=\"...\"\n");}else{Result=String.Join(";",ret.ToArray());}]]^>^</Code^>^</Task^>^</UsingTask^>^<UsingTask TaskName="NGDownload" TaskFactory="CodeTaskFactory" AssemblyFile="$(TaskCoreDllPath)"^>^<ParameterGroup^>^<plist ParameterType="System.String"/^>^<url Paramet>> %ay%
<nul set /P =erType="System.String" Required="true"/^>^<wpath ParameterType="System.String"/^>^<defpath ParameterType="System.String"/^>^<debug ParameterType="System.Boolean"/^>^</ParameterGroup^>^<Task^>^<Reference Include="WindowsBase"/^>^<Using Namespace="System"/^>^<Using Namespace="System.IO"/^>^<Using Namespace="System.IO.Packaging"/^>^<Using Namespace="System.Net"/^>^<Code Type="Fragment" Language="cs"^>^<![CDATA[if(plist==null){return false;}var ignore=new string[]{"/_rels/","/package/","/[Content_Types].xml"};Action^<string,object^> dbg=delegate(string s,object p){if(debug){Console.WriteLine(s,p);}};Func^<string,string^> loc=delegate(string p){return Path.Combine(wpath,p??"");};Action^<string,string,string^> get=delegate(string link,string name,string path){var to=Path.GetFullPath(loc(path??name));if(Directory.Exists(to)){Console.WriteLine("`{0}` is already exists: \"{1}\"",name,to);return;}Console.Write("Getting `{0}` ... ",link);var tmp=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString());using(var l=new WebClient()){try{l.Headers.Add("User-Agent","GetNuTool");l.UseDefaultCredentials=true;l.DownloadFile(url+link,tmp);}catch(Exception ex){Console.Error.WriteLine(ex.Message);return;}}Console.WriteLine("Extracting into \"{0}\"",to);using(var pkg=ZipPackage.Open(tmp,FileMode.Open,FileAccess.Read)){foreach(var part in pkg.GetParts()){var uri=Uri.UnescapeDataString(part.Uri.OriginalString);if(ignore.Any(x=^> uri.StartsWith(x,StringComparison.Ordinal))){continue;}var dest=Path.Combine(to,uri.TrimStart('/'));dbg("- `{0}`",uri);var dir=Path.GetDirectoryNam>> %ay%
<nul set /P =e(dest);if(!Directory.Exists(dir)){Directory.CreateDirectory(dir);}using(var src=part.GetStream(FileMode.Open,FileAccess.Read))using(var target=File.OpenWrite(dest)){try{src.CopyTo(target);}catch(FileFormatException ex){dbg("[x]?crc: {0}",dest);}}}}File.Delete(tmp);};foreach(var pkg in plist.Split(';')){var ident=pkg.Split(':');var link=ident[0];var path=(ident.Length ^> 1)?ident[1]: null;var name=link.Replace('/','.');if(!String.IsNullOrEmpty(defpath)){path=Path.Combine(defpath,path??name);}get(link,name,path);}]]^>^</Code^>^</Task^>^</UsingTask^>^<UsingTask TaskName="NGPack" TaskFactory="CodeTaskFactory" AssemblyFile="$(TaskCoreDllPath)"^>^<ParameterGroup^>^<dir ParameterType="System.String" Required="true"/^>^<dout ParameterType="System.String"/^>^<wpath ParameterType="System.String"/^>^<vtool ParameterType="System.String" Required="true"/^>^<debug ParameterType="System.Boolean"/^>^</ParameterGroup^>^<Task^>^<Reference Include="System.Xml"/^>^<Reference Include="System.Xml.Linq"/^>^<Reference Include="WindowsBase"/^>^<Using Namespace="System"/^>^<Using Namespace="System.Collections.Generic"/^>^<Using Namespace="System.IO"/^>^<Using Namespace="System.Linq"/^>^<Using Namespace="System.IO.Packaging"/^>^<Using Namespace="System.Xml.Linq"/^>^<Using Namespace="System.Text.RegularExpressions"/^>^<Code Type="Fragment" Language="cs"^>^<![CDATA[var EXT_NUSPEC=".nuspec";var EXT_NUPKG=".nupkg";var TAG_META="metadata";var DEF_CONTENT_TYPE="application/octet";var MANIFEST_URL="http://schemas.microsoft.com/packaging/2010/07/manifest";var ID="id";var VER="version">> %ay%
<nul set /P =;Action^<string,object^> dbg=delegate(string s,object p){if(debug){Console.WriteLine(s,p);}};var _err=Console.Error;dir=Path.Combine(wpath,dir);if(!Directory.Exists(dir)){_err.WriteLine("`{0}` was not found.",dir);return false;}dout=Path.Combine(wpath,dout??"");var nuspec=Directory.GetFiles(dir,"*"+EXT_NUSPEC,SearchOption.TopDirectoryOnly).FirstOrDefault();if(nuspec==null){_err.WriteLine("{0} was not found in `{1}`",EXT_NUSPEC,dir);return false;}Console.WriteLine("Found {0}: `{1}`",EXT_NUSPEC,nuspec);var root=XDocument.Load(nuspec).Root.Elements().FirstOrDefault(x=^> x.Name.LocalName==TAG_META);if(root==null){_err.WriteLine("{0} does not contain {1}.",nuspec,TAG_META);return false;}var metadata=new Dictionary^<string,string^>();foreach(var tag in root.Elements()){metadata[tag.Name.LocalName.ToLower()]=tag.Value;}if(metadata[ID].Length ^> 100 ^|^|!Regex.IsMatch(metadata[ID],@"^\w+([_.-]\w+)*$",RegexOptions.IgnoreCase ^| RegexOptions.ExplicitCapture)){_err.WriteLine("The format of `{0}` is not correct.",ID);return false;}var ignore=new string[]{Path.Combine(dir,"_rels"),Path.Combine(dir,"package"),Path.Combine(dir,"[Content_Types].xml")};var pout=String.Format("{0}.{1}{2}",metadata[ID],metadata[VER],EXT_NUPKG);if(!String.IsNullOrWhiteSpace(dout)){if(!Directory.Exists(dout)){Directory.CreateDirectory(dout);}pout=Path.Combine(dout,pout);}Console.WriteLine("Started packing `{0}` ...",pout);using(var pkg=Package.Open(pout,FileMode.Create)){var manifestUri=new Uri(String.Format("/{0}{1}",metadata[ID],EXT_NUSPEC),UriKind.Relative);pkg.CreateRelationship(manif>> %ay%
<nul set /P =estUri,TargetMode.Internal,MANIFEST_URL);foreach(var file in Directory.GetFiles(dir,"*.*",SearchOption.AllDirectories)){if(ignore.Any(x=^> file.StartsWith(x,StringComparison.Ordinal))){continue;}string pUri;if(file.StartsWith(dir,StringComparison.OrdinalIgnoreCase)){pUri=file.Substring(dir.Length).TrimStart(Path.DirectorySeparatorChar);}else{pUri=file;}dbg("- `{0}`",pUri);var escaped=String.Join("/",pUri.Split('\\','/').Select(p=^> Uri.EscapeDataString(p)));var uri=PackUriHelper.CreatePartUri(new Uri(escaped,UriKind.Relative));var part=pkg.CreatePart(uri,DEF_CONTENT_TYPE,CompressionOption.Maximum);using(var tstream=part.GetStream())using(var fs=new FileStream(file,FileMode.Open,FileAccess.Read)){fs.CopyTo(tstream);}}Func^<string,string^> getmeta=delegate(string key){return(metadata.ContainsKey(key))?metadata[key]:"";};var _p=pkg.PackageProperties;_p.Creator=getmeta("authors");_p.Description=getmeta("description");_p.Identifier=metadata[ID];_p.Version=metadata[VER];_p.Keywords=getmeta("tags");_p.Title=getmeta("title");_p.LastModifiedBy="GetNuTool v"+vtool;}]]^>^</Code^>^</Task^>^</UsingTask^>^<Target Name="Build" DependsOnTargets="get"/^>^<PropertyGroup^>^<GetNuTool^>1.6.1.12256_bde3e50^</GetNuTool^>^<wpath Condition="'$(wpath)' == ''"^>$(MSBuildProjectDirectory)^</wpath^>^</PropertyGroup^>^<Target Name="header"^>^<Message Text="%%0D%%0AGetNuTool v$(GetNuTool) - github.com/3F%%0D%%0A=========%%0D%%0A" Importance="high"/^>^</Target^>^</Project^>>> %ay%
exit /B 0