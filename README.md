# Lyrics Repo

![LICENSE](https://img.shields.io/github/license/jim60105/Lyrics?style=for-the-badge)
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/jim60105/Lyrics/Fetch%20Lyrics?style=for-the-badge)

## 特殊 SongId

為了記錄歌詞搜尋狀態，專案內使用負數和0做為特殊的 SongId

| SongId |                  意義                   |
|:------:|:-------------------------------------:|
|   0    |          手動禁用歌詞搜尋功能           |
|   -1   |              歌曲搜尋失敗               |
|   <0   | 有找到歌曲但歌詞搜尋失敗，記錄為 -SongId |

## 同名歌曲

由於歌詞是以曲名做匹配搜尋，因此在同名歌曲上會出錯\
此表格記錄目前專案內發現的同名歌曲

|  曲目   |                                                                                                         說明                                                                                                          |
|:-------:|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
|   you   |                         [You/雪野五月](https://music.163.com/#/song?id=672188)<br> [you/癒月](https://music.163.com/#/song?id=33579507)<br> [YOU/YUI](https://music.163.com/#/song?id=668376)                         |
| Realize |                                              [Realize/鈴木このみ](https://music.163.com/#/song?id=1474120993)<br>[Realize!/i☆Ris](https://music.163.com/#/song?id=31062384)                                              |
|  オレンジ   | [オレンジ/流田Project(釘宮理恵,堀江由衣,喜多村英梨)](https://music.163.com/#/song?id=448741128)<br> [オレンジ/7!!](https://music.163.com/#/song?id=458725210)<br>[オレンジ/トーマ(初音ミク)](https://music.163.com/#/song?id=26310273) |
|  S・K・Y  |                                              [S・K・Y/ライブP(鏡音リン)](https://music.163.com/#/song?id=1398679779)<br>[S•K•Y/さかな](https://music.163.com/#/song?id=1376649008)                                              |
|  WILL   |                                               [WILL/米倉千尋](https://music.163.com/#/song?id=669130)<br>[WILL/TRUE](http://music.163.com/api/song/media?id=1479561919)                                               |

## 特殊狀況

一般來說是優先使用網易雲音樂上原曲的歌詞，下表記錄例外狀況

|      曲目      |                                                                                             說明                                                                                              |
|:--------------:|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------:|
| INITIUM/久遠たま |                                                Cover專輯，[網易有頁面但沒有歌詞](https://music.163.com/#/album?id=149898107)，所以使用各原曲歌詞                                                |
|  もってけ！セーラーふく   | [原曲](https://music.163.com/#/song?id=1440363252)、[第一搜尋結果](https://music.163.com/#/song?id=4919429)的歌詞都是錯誤的，使用此[正確歌詞](https://music.163.com/api/song/media?id=28892268) |
|      メリッサ      |                            [第一搜尋結果](https://music.163.com/#/song?id=28272046)的歌詞是錯誤的，使用此[正確歌詞](https://music.163.com/api/song/media?id=799457)                            |
|      U&I       |                          [第一搜尋結果](https://music.163.com/#/song?id=22803891)的歌詞是錯誤的，使用此[正確歌詞](https://music.163.com/api/song/media?id=1317091851)                          |
|       さぁ       |                               [原曲](https://music.163.com/#/song?id=32288465)的歌詞是錯誤的，使用此[正確歌詞](https://music.163.com/api/song/media?id=29191482)                               |
|  凛々咲原創曲   |                                                                        在網易上有頁面但沒有歌詞，執行時一定會跳invalid                                                                         |

## LICENSE

lrc歌詞來源為 [網易雲音樂](https://music.163.com/)，歌詞內容版權為原作者和網易雲音樂所有。\
本專案僅將歌詞轉存至 Github Repo，不會對歌詞內容進行任何修改。\
本專案程式部份採用 MIT License，詳細內容請參考 [LICENSE](/LICENSE) 檔案。\
[![LICENSE](https://img.shields.io/github/license/jim60105/Lyrics?style=for-the-badge)
](/LICENSE)