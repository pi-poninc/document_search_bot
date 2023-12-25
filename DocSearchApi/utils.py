import os

from langchain.docstore.document import Document
from langchain.text_splitter import CharacterTextSplitter

from langchain.chat_models import ChatOpenAI
from langchain.chains import ConversationalRetrievalChain
from langchain.vectorstores.azuresearch import AzureSearch

# 文章とメタデータを受け取り、Documentのリストを返す
def txtToDocs(content:str, metadata:dict):
    docs = [Document(page_content=content, metadata=metadata)] # Documentのリストを作成
    chunk_size=1000 # Documentの文字数の設定
    separator='' # Documentの区切り文字の設定(''の場合、chunk_sizeで指定した文字数で区切る)
    chunk_overlap=50 # 分割した文章のオーバーラップする文字数の設定

    # 文章を分割するためのインスタンスを作成
    text_splitter = CharacterTextSplitter(
            chunk_size=chunk_size, 
            separator=separator, 
            chunk_overlap=chunk_overlap
            )
    # Documentのリストを設定したパラメータで分割
    splitted_docs = text_splitter.split_documents(docs)
    return splitted_docs

# チャットボットの実行
def chatExecute(
        query: str, # ユーザーの質問
        vectore_stores: AzureSearch, # ベクターストアのインスタンス
        chat_history:list[tuple]=[], # 会話履歴
        search_type="similarity", # 検索タイプ
        search_kwargs={} 
):
    # ベクターストアから検索結果を取得するためのインスタンスを作成
    retriver = vectore_stores.as_retriever(
        search_type=search_type,
        search_kwargs=search_kwargs
    )

    # LLMのインスタンスを作成
    model = ChatOpenAI(
        engine=os.environ['OPENAI_API_CHAT_ENGINE'], # Azure OpenAI を使用する場合は、デプロイメント名を指定する
        temperature=0
    )
    
    # チャットボットのインスタンスを作成
    qa = ConversationalRetrievalChain.from_llm(
        llm = model,
        retriever = retriver, 
        return_source_documents=True, # 検索結果のDocumentを返す
        chain_type='stuff'
        )

    # チャットボットの実行
    result = qa({'question': query, "chat_history": chat_history})
    answer = result['answer'] # チャットボットの回答
    metadatas = [doc.metadata for doc in result['source_documents']] # チャットボットの回答に対応するメタデータのリスト

    chat_history.append((query, result['answer'])) # 会話履歴に追加
    return (answer, chat_history, metadatas)

# chat_historyをリストに変換
def chatHistoryToList(chat_history:list[dict]) -> list[tuple]:
    chat_history_list = []
    # chat_historyをuserとbotの組み合わせのタプルのリストに変換
    # chat_history = [
    #     { "user": "Hello" },
    #     { "bot": "echo : Hello" },
    #     { "user": "How are you?" }
    # ]
    # chat_history_list = [
    #     ("Hello", "echo : Hello"),
    #     ("How are you?", "")
    
    chat_history_list = []
    skip = False
    for i, chat in enumerate(chat_history):
        if skip:
            skip = False
            continue
        if "user" in chat:
            if i+1 < len(chat_history):
                if "bot" in chat_history[i+1]:
                    chat_history_list.append((chat["user"], chat_history[i+1]["bot"]))
                    skip = True
                else:
                    chat_history_list.append((chat["user"], ""))
                    skip = False
            else:
                chat_history_list.append((chat["user"], ""))
                skip = False
            
    return chat_history_list
