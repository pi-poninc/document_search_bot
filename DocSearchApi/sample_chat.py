# チャットボットとの対話をシミュレートするサンプルコード
import requests
import json

def main():
    chat_history = []
    for i in range(10):
        url = 'http://127.0.0.1:8000/conversation_history'
        query = input('query: ')
        
        chat_history.append({'user': query})
        
        parameters = {
            'messages': chat_history
        }
        
        response = requests.post(url, json=parameters)
        res = json.loads(response.text)
        print('res:', res)
        bot_message = res['bot_message']
        chat_history[-1]['bot_message'] = bot_message
        print('res_answer:', bot_message)
        print('res_metadata',res['metadata'])
        
        print('-----------------------------')

if __name__ == '__main__':
    main()