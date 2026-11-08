# 放置廚房（暫名）— Unity / C#

像素風放置經營遊戲 prototype，目標平台 Steam（PC）。

## 文件（實作前只讀對應章節，不要整份讀）
- 企劃書（設計意圖）：design/gdd.md
- 技術文件（實作依據）：docs/technical-spec.md
- 兩份文件衝突、或需求不明確時，停下來問使用者，不要自行決定。

## 架構鐵律（技術文件 2.2）
- Game.Core：純 C#，不繼承 MonoBehaviour，不引用 View，不依賴 ScriptableObject
- Game.View：場景與 UI，單向依賴 Core
- 跨系統溝通走 EventBus，不互相抓引用
- 遊戲數值一律外部化，不寫死在程式裡
- 食材飛行等動畫只是表現層，動畫壞了不可影響數值正確

## 工作流程
1. 大型需求先交給 director，它只會產出「執行計畫」，不會自己派工。
2. 主 session 依計畫，逐步呼叫對應的 agent。
3. 每個程式 agent 完成後，呼叫 code-reviewer 檢查。
4. 計畫中標示「需要使用者決定」的項目，先問使用者，再繼續。

## 資料夾與負責人（各 agent 只改自己的資料夾）
- design/                                  → game-designer
（路徑皆在 Assets/Project/ 底下）
- Scripts/Core/、Scripts/Tests/                       → core-programmer
- Scripts/Runtime/Stations/、Scripts/Runtime/System/  → gameflow-programmer
- Scripts/Runtime/UI/、Prefabs/UI/                    → ui-programmer
- Scripts/Runtime/Fx/、Scripts/Runtime/Data/、Data/（定義資料 JSON）、Art/Fonts/、asmdef 與專案設定 → engine-programmer
- 只讀不改：director、code-reviewer

某個 agent 發現需要改別人的資料夾時，不要自己動手，回報給主 session 處理。

## 版本控制
- commit 訊息一律使用簡短英文（例：`update framework`）。
- commit 前先把訊息給使用者確認，確認後才 commit；每次 commit 都要重新確認。
