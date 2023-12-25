# azure cognitive searchのインデックスを削除するスクリプト

import os, json
from time import sleep
from datetime import datetime
from dotenv import load_dotenv
load_dotenv()  # .envファイルから環境変数を読み込む

from azure.search.documents.indexes import SearchIndexClient
from azure.core.credentials import AzureKeyCredential

si_client = SearchIndexClient(
    endpoint=os.environ['AZURE_VECTORE_STORES_ADDRESES'],
    credential=AzureKeyCredential(os.environ['AZURE_VECTORE_STORES_PASSWORD'])
    )

si_client.delete_index(os.environ['AZURE_VECTORE_STORES_INDEX_NAME'])