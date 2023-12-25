import os
from dotenv import load_dotenv
load_dotenv()  # .envファイルから環境変数を読み込む

from fastapi import FastAPI
from utils import txtToDocs, chatExecute, chatHistoryToList
from VectoreStores.azure import azureAddDocuments, azureSearch, azureLoad

import traceback

app = FastAPI()

# ベクターストアへのドキュメントの追加
# 設定できるパラメータ
# content: str ドキュメントの内容
# metadata: dict ドキュメントのメタデータ
@app.post("/add_document")
def add_document(parameters: dict):
    content = parameters["content"]
    metadata = parameters["metadata"]
    docs = txtToDocs(content, metadata)
    return azureAddDocuments(documents=docs)

# ベクターストアへのベクトル検索
# 設定できるパラメータ
# query: str 検索クエリ
# doc_num: int 取得するドキュメント数
# filters: dict フィルタリングするmetadata。表記例：filters:"notion_id eq '714f3207737c4ee8b97ff4e0efcde6db'"
# search_type: str 検索タイプ（similarity, hybrid, semantic_hybridのいずれか）
@app.post(
        "/doc_search",
        description='''ベクターストアへのベクトル検索。request sample:{"query":"ドクターボイス","doc_num":2,"search_type":"hybrid","filters":"notion_id eq '714f3207737c4ee8b97ff4e0efcde6db'"}'''
        )
def search(parameters: dict):
    query = parameters["query"] # 検索クエリ
    
    # 取得するドキュメント数
    if "doc_num" in parameters:
        k = int(parameters["doc_num"])
    else:
        k = os.environ['DOCUMENT_NUM']

    filters = {}
    # フィルタリングするmetadataの取得
    if "filters" in parameters:
        filters = parameters["filters"]
    else:
        filters = None

    # 検索タイプの取得（similarity, hybrid, semantic_hybridのいずれか）
    search_type = "hybrid"
    if "search_type" in parameters:
        search_type = parameters["search_type"]
        
    return azureSearch(query=query, k=k, filters=filters, search_type=search_type)

# チャットボットのメッセージの生成
# 設定できるパラメータ
# messages: List[dict] チャット履歴。末尾の'user'キーの値がユーザーの質問として扱われる
# k: int 取得するドキュメント数
# filters: str フィルタリング。表記例：filters:"notion_id eq '714f3207737c4ee8b97ff4e0efcde6db'"
# search_type: str 検索タイプ（similarity, hybrid, semantic_hybridのいずれか）
# model_name: str LLMモデル
@app.post("/conversation_history", description='''
          request sample:{"messages": [{"user": "こんにちは"},{"bot": "こんにちは"},{"user": "元気ですか？"}, {"bot": "元気ですか？"}],"doc_num":4,"filters":"notion_id eq '714f3207737c4ee8b97ff4e0efcde6db'"}
''')
async def add_bot_message(conversation_history: dict):
    try:
        # チャット履歴の取得
        chat_history = conversation_history["messages"]

        if len(chat_history) == 0:
            return {"bot": "チャット履歴が空です", "metadata": {}}
        
        if "bot" in chat_history[-1]:
            return {"bot": "チャット履歴の末尾はユーザーの質問である必要があります", "metadata": {}}

        # ユーザーの質問を取得
        query = chat_history[-1]["user"]

        # chat_historyから末尾の値を削除
        if len(chat_history) > 0:
            chat_history.pop(-1)
            chat_history = chatHistoryToList(chat_history)
        
        search_kwargs = {}
        # filtersの取得
        if "filters" in conversation_history:
            filters = conversation_history["filters"]
            search_kwargs["filters"] = filters

        # 取得するドキュメント数
        if 'doc_num' in conversation_history:
            search_kwargs['k'] = conversation_history['doc_num']
        else:
            search_kwargs['k'] = os.environ['DOCUMENT_NUM']

        # 検索タイプの取得（similarity, hybrid, semantic_hybridのいずれか）
        if 'search_type' in conversation_history:
            search_kwargs['search_type'] = conversation_history['search_type']
        else:
            search_kwargs['search_type'] = 'hybrid'

        # モデル
        # model_name = "gpt-3.5-turbo"
        # if 'model_name' in conversation_history:
        #     model_name = conversation_history['model_name']
        
        # チャットボットのメッセージの生成
        (answer, res_chat_history, res_metadata) = chatExecute(
            query,
            vectore_stores=azureLoad(),
            search_kwargs=search_kwargs,
            chat_history=chat_history
        )

        return {"bot": answer, "metadata": res_metadata}
    
    except Exception as e:

        return {"error": traceback.format_exc()}