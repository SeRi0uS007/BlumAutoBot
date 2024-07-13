# BlumAutoBot

Play Blum games automatically

1. Enable Telegram WebView (`Telegram settings => Advanced => Experimental settings => Enable webview inspecting`)
2. Press **F12** and go to the Network tab ![Network Tab Screenshot](https://i.imgur.com/2sOxffx.png)
3. Make some moves between tabs in blum game to get network logs ![Network Logs Screenshot](https://i.imgur.com/ISSQU3o.png)
4. Select one log and copy **Authorization** Request Header (without Bearer text) ![Authorization Example Screenshot](https://i.imgur.com/jtUcZaR.png)
5. Paste **AccessToken** into .json file in *accounts* folder ![JSON File Example](https://i.imgur.com/bg2kc8y.png)
