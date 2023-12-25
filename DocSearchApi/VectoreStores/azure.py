import os, traceback
import dotenv
dotenv.load_dotenv()  # .envファイルから環境変数を読み込む

from langchain.embeddings.openai import OpenAIEmbeddings
from langchain.vectorstores.azuresearch import AzureSearch
from azure.search.documents.indexes.models import (
    SearchableField,
    SearchField,
    SearchFieldDataType,
    SimpleField
)
from langchain.docstore.document import Document
from typing import List


# ベクターストアのロード
def azureLoad(fields=None):
    embeddings: OpenAIEmbeddings = OpenAIEmbeddings(
        deployment=os.environ['OPENAI_API_EMBEDDING_DEPLOYMENT_NAME'], # モデルのデプロイメント名
        model_name=os.environ['OPENAI_API_EMBEDDING_MODEL_NAME'] # モデル名
    ) # OpenAIのモデルを使用してベクトル化

    # ベクターストアのインスタンスを作成
    vectore_stores: AzureSearch = AzureSearch(
        azure_search_endpoint=os.environ['AZURE_VECTORE_STORES_ADDRESES'],  
        azure_search_key=os.environ['AZURE_VECTORE_STORES_PASSWORD'],  
        index_name=os.environ['AZURE_VECTORE_STORES_INDEX_NAME'],
        embedding_function=embeddings.embed_query,
        fields=fields
    )
    return vectore_stores


# ベクターストアへのドキュメントの追加
def azureAddDocuments(documents:list[Document]) -> dict:
    embeddings: OpenAIEmbeddings = OpenAIEmbeddings() # OpenAIのモデルを使用してベクトル化
    
    # ベクターストアの検索フィールドの設定
    # metadataでフィルタリングする場合は、filterable=Trueを設定する
    fields = [
        SimpleField(
            name="id",
            type=SearchFieldDataType.String,
            key=True,
            filterable=True,
        ),
        SearchableField(
            name="content",
            type=SearchFieldDataType.String,
            searchable=True,
        ),
        SearchField(
            name="content_vector",
            type=SearchFieldDataType.Collection(SearchFieldDataType.Single),
            searchable=True,
            vector_search_dimensions=len(embeddings.embed_query("Text")),
            vector_search_configuration="default",
        ),
        SearchableField(
            name="metadata",
            type=SearchFieldDataType.String,
            searchable=True,
        ),
        # フィルタリングしたいmetadataはここに追加する
        SimpleField(
            name="notion_id", # フィルタリングしたいmetadataのキー
            type=SearchFieldDataType.String, # フィルタリングしたいmetadataの型
            filterable=True, # フィルタリング可能にする
        ),
    ]

    vectore_stores = azureLoad(fields=fields) # ベクターストアのロード

    res = vectore_stores.add_documents(documents) # ベクターストアへのドキュメントの追加
    return res 

# ベクターストアへのベクトル検索
def azureSearch(query, k:int=None, filters={}, search_type="hybrid") -> List[Document]:
    try:
        vectore_stores = azureLoad() # ベクターストアのロード
        
        if k is None:
            k = int(os.environ['DOCUMENT_NUM'])

        # ベクターストアからドキュメントを検索
        docs = vectore_stores.similarity_search(query, k=k, filters=filters, search_type=search_type)

        return docs
    except Exception as e:
        return {"status": "ng", "error": traceback.format_exc()}

