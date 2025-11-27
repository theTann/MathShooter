@echo off
setlocal

echo [1단계] CSV 다운로드중..
call "TableDownloader.exe"

REM 에러레벨 검사: 0이 아니면 종료
if not %ERRORLEVEL% EQU 0 (
    echo [오류] CSV 다운로드에 실패했습니다. 종료 코드: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo [2단계] CSV로 MemoryPack CSharp파일 생성중..
call "CsvToMemoryPack.exe"

if not %ERRORLEVEL% EQU 0 (
    echo [오류] 메모리팩 CSharp파일로 전환에 실패했습니다. 종료 코드: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo [3단계] CSV내용을 Byte파일로 변환중..
call "CsvToMemoryPackBinary.exe"

if not %ERRORLEVEL% EQU 0 (
    echo [오류] 메모리팩 Binary 생성에 실패했습니다. 종료 코드: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

call "copy_files_for_client.bat"

endlocal