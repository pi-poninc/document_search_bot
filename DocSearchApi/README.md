# DocSearchApi

`python -V 3.10.4`

## 初期設定 

ライブラリのインストール

```bash
pip install -r requirements.txt
```

環境変数の設定(.env.sampleを参考に.envを作成)

```bash
OpenAIのAPIキー
OPENAI_API_KEY=sk-OOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO

Azure cognitive searchのアドレス
AZURE_VECTORE_STORES_ADDRESES=https://OOOOOOOOOOOO.windows.net

Azure cognitive searchのキー
AZURE_VECTORE_STORES_PASSWORD=OOOOOOOOOOOOOOOOOOOOOO

インデックス名（自由に設定可）
AZURE_VECTORE_STORES_INDEX_NAME=test-index
```


## DocSearchAPIの起動

```bash
uvicorn DocSearchApi.server:app --reload 
```

[API実行のテストサイト(API起動後に参照可)](http://127.0.0.1:8000/docs#/)


## 参考

[非同期サーバーのFastAPIでバックグラウンドで重い処理を書く](https://qiita.com/juri-t/items/91e561509aa7ca6e7d38)

[LangChainでCognitive SearchのベクトルDBを構築する](https://qiita.com/tmiyata25/items/cf417c51aad2660f2c42#python%E3%81%AEpdfloader)

[Azure Cognitive Search](https://python.langchain.com/docs/integrations/vectorstores/azuresearch#import-required-libraries)

[azure cognitive searchのfilterの説明](https://learn.microsoft.com/ja-jp/rest/api/searchservice/search-documents#query-parameters)