@echo off

set version=%1

		if exist "c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\15.0\Bin\MSBuild.exe" (
	        	echo will use VS 2019 Community build tools
        		set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\15.0\Bin"
		) else ( 
			if exist "c:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe" (
        			set msbuild="c:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin"
	        		echo will use VS 2019 Pro build tools
			) else (
				echo Unable to locate VS 2019 build tools, will use default build tools on path
			)
		)

if exist "c:\Program Files (x86)\Inno Setup 6\ISCC.exe" (
	set inno="c:\Program Files (x86)\Inno Setup 6\ISCC.exe"
) else (
	if exist "c:\Program Files (x86)\Inno Setup 5\ISCC.exe" (
		set inno="c:\Program Files (x86)\Inno Setup 5\ISCC.exe"
	) else (
		echo Can't Find INNO Setup Tools
		goto :eof
	)
)

set signtool="C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x64\signtool.exe"
set cwd=%cd%
set nuget="%cwd%\.nuget\nuget.exe"
echo Will build version %version%
echo Will use NUGET in %nuget%
echo Will use MSBUILD in %msbuild%


	%msbuild%\msbuild santedb-www.sln /t:clean
	%msbuild%\msbuild santedb-www.sln /t:restore
	%msbuild%\msbuild santedb-www.sln /t:rebuild /p:configuration=SignedRelease /m:1 /p:VersionNumber=%version%

	FOR /R "%cwd%\bin\SignedRelease" %%G IN (*.exe) DO (
		echo Signing %%G
		%signtool% sign /sha1 a11164321e30c84bd825ab20225421434622c52a /d "SanteDB dCDR" "%%G"
	)

	FOR /R "%cwd%\bin\SignedRelease" %%G IN (SanteDB*.dll) DO (
		echo Signing %%G
		%signtool% sign /sha1 a11164321e30c84bd825ab20225421434622c52a /d "SanteDB dCDR" "%%G"
	)
	
	%inno% "/o.\bin\dist" ".\santedb-www.iss" /d"MyAppVersion=%version%" 
	
	rem ################# TARBALLS 
	echo Building Linux Tarball

	mkdir santedb-www-%version%
	cd santedb-www-%version%
	xcopy /I /E "..\bin\SignedRelease\*.*" 
	cd ..
	"C:\program files\7-zip\7z" a -r -ttar .\bin\dist\santedb-www-%version%.tar .\santedb-www-%version%
	"C:\program files\7-zip\7z" a -r -tzip .\bin\dist\santedb-www-%version%.zip .\santedb-www-%version%
	"C:\program files\7-zip\7z" a -tbzip2 .\bin\dist\santedb-www-%version%.tar.bz2 .\bin\dist\santedb-www-%version%.tar
	"C:\program files\7-zip\7z" a -tgzip .\bin\dist\santedb-www-%version%.tar.gz .\bin\dist\santedb-www-%version%.tar
	rmdir /q /s .\santedb-www-%version%
	
	rem ####### Docker Container - SSL.COM intermediary certificate is not known to docker so we use SDB
	FOR /R "%cwd%\bin\SignedRelease" %%G IN (*.exe) DO (
		echo Signing %%G
		%signtool% sign /sha1 f3bea1ee156254656669f00c03eeafe8befc4441 /d "SanteDB dCDR" "%%G"
	)

	FOR /R "%cwd%\bin\SignedRelease" %%G IN (SanteDB*.dll) DO (
		echo Signing %%G
		%signtool% sign /sha1 f3bea1ee156254656669f00c03eeafe8befc4441 /d "SanteDB dCDR" "%%G"
	)
	docker build --no-cache -t santesuite/santedb-www:%version% .
	docker build --no-cache -t santesuite/santedb-www .
	


:eof