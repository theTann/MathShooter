0. 솔루션을 빌드한다. generate된 cs파일들이 있으면 실패 할 수 있음. 삭제해 줘야함.
1. sheet.config를 설정한다.
2. copy_files_for_client.bat을 프로젝트 폴더에 맞게 수정한다.(컨버팅완료된 테이블 데이터를 클라이언트 폴더에 복사하는 과정이고, 이는 run.bat에서 실행함)
3. run.bat을 실행한다.
4. 현재는 enum을 따로 데이터에서 읽어서 만드는건 없고 predefine_enum.cs에 직접사용하고 있다.
5. 컬럼명에 *을 붙이면 Dictionary로 만들고, **을 붙이면 DictionaryList인데, 이건 확인좀 해보자 뭐였지
todo

predefine_enum을 테이블로 부터 읽기 해야할까?
배열에 대한 처리를 해야할까?

