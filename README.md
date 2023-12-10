# Lyrics Repo

[![CodeFactor](https://www.codefactor.io/repository/github/youtubeclipplaylist/lyrics/badge?style=for-the-badge)](https://www.codefactor.io/repository/github/youtubeclipplaylist/lyrics)
![LICENSE](https://img.shields.io/github/license/YoutubeClipPlaylist/Lyrics?style=for-the-badge)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/YoutubeClipPlaylist/Lyrics/FetchLyrics.yml?branch=master&style=for-the-badge)

此專案是 **[Youtube影片截選播放清單](https://github.com/YoutubeClipPlaylist/YoutubeClipPlaylist)** 專案的 submodule，存放該專案的歌詞。  
歌詞來源為 [網易雲音樂](https://music.163.com/)，以 Github Workflow 定時將播放清單使用的歌詞轉存至此 Repo 的 [lyrics](https://github.com/YoutubeClipPlaylist/Lyrics/tree/lyrics) 分支，然後再讓客戶端存取此 Github Repo。  
經過這層轉存，避免客戶端直接存取網易雲音樂站台，降低資訊安全風險。

## 聲明

本專案的所有歌詞皆來自網易雲音樂，由程式自動搜尋並建立關聯。  
此專案不會對歌詞內容進行任何修改，也不會以歌詞進行任何商業行為。

## 歌詞錯誤回報

接受兩種錯誤回報

- 歌詞錯置，誤用到其它曲目的歌詞
- 歌詞偏移，太快或太慢

> 不接受歌詞內的單詞錯誤修正

由於歌詞皆為自動化搜尋取得，能預期會有大量的錯誤情況發生。  
若發現歌詞有錯誤，請在此 Repo 的 [Issues](/issues/new/choose) 頁面回報。  
請務必提供該歌曲的 Share 連結，以便我們能夠快速找到錯誤歌曲。

![2022-10-05 05 11 18](https://user-images.githubusercontent.com/16995691/193930111-36f83f34-bfc6-469d-ad87-a3ee57fc369b.png)

> 若是方便的話，也歡迎提供正確的網易雲音樂歌曲ID，以便我們能夠快速處理錯誤。  
> 請在 [網易雲音樂](https://music.163.com/#/search/) 搜尋正確的歌曲，將連結貼上至回報內容中。

## LICENSE

lrc歌詞來源為 [網易雲音樂](https://music.163.com/)，歌詞內容版權為原作者和網易雲音樂所有。  
本專案僅將歌詞轉存至 Github Repo，不會對歌詞內容進行任何修改。  
本專案程式部份採用 MIT License，詳細內容請參考 [LICENSE](/LICENSE) 檔案。  
[![LICENSE](https://img.shields.io/github/license/YoutubeClipPlaylist/Lyrics?style=for-the-badge)
](/LICENSE)
