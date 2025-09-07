
del *.exe

csc ^
/reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\PresentationCore.dll" ^
/reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\PresentationFramework.dll" ^
/reference:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\WindowsBase.dll" ^
/target:exe ^
%*
