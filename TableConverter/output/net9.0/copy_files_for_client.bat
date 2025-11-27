@echo off
setlocal

echo [4단계] 기존 파일 삭제..
del .\..\..\..\MathTowerDef\Assets\Scripts\Base\Datatable\Generated\ /q
del .\..\..\..\MathTowerDef\Assets\StreamingAssets\DataTables /q

echo [5단계] 결과 파일 복사중..
copy .\cs\*.cs .\..\..\..\MathTowerDef\Assets\Scripts\Base\Datatable\Generated\
copy .\predefine_enums\*.cs .\..\..\..\MathTowerDef\Assets\Scripts\Base\Datatable\Generated\
copy .\binary\*.* .\..\..\..\MathTowerDef\Assets\StreamingAssets\DataTables

pause

endlocal