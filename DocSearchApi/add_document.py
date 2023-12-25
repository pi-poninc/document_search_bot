# NotionCSVフォルダ内のcsvファイルを読み込み、API経由でVectoreStoresにドキュメントを追加するスクリプト

import glob, os
import csv
import requests

def main():
    paths = glob.glob('NotionCSV/*.csv')
    for path in paths:
        notion_id = os.path.basename(path).split('.')[0]
        content = ''
        with open(path, 'r', encoding='utf-8') as f:
            reader = csv.reader(f)
            _ = next(reader)
            for row in reader:
                content += row[1] + '\n'
        
        metadata = {
            "notion_id": notion_id
        }

        # リクエストパラメータの設定
        parameters = {
            "content": content,
            "metadata": metadata
        }

        # リクエストの送信
        url = 'http://127.0.0.1:8000/add_document'
        response = requests.post(url, json=parameters)

        # レスポンスの表示
        # 200: 成功
        # 400: リクエストが不正
        # 500: サーバー内部エラー
        print(path, response.status_code, response.text)

        
if __name__ == '__main__':
    main()