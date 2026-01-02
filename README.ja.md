# クリップボード送信アプリ（Clipboard Sender）

Windows の **システムトレイ常駐型** クリップボード送信アプリです  
クリップボードの内容を、指定した宛先へすばやく送信できるようにすることを目的にしています  
Notemod-selfhosted との連携を想定して開発しました。  
https://github.com/StayHomeLabNet/Notemod-selfhosted

---

## 特長

- **システムトレイ常駐**（邪魔にならない運用）
- **有効/無効の状態が一目で分かるトレイアイコン**
- 配布時に Assets フォルダが不要になるよう、**アイコン等をアプリに内包（Resources化）** する運用に対応
- 多言語表示に対応（日本語/英語ほか）  
  - ※ Notemod の作者に敬意を表して **トルコ語（TR）** を追加しています

---

## 想定している利用方法

このアプリは、日常の「コピペ作業」を外部サービスに依存せずにもう一段速くするための補助ツールです  
開発の目的は README 冒頭に書いている通り、次の一点に集約されます

- Windows の **システムトレイ常駐型** クリップボード送信アプリです。  
- クリップボードの内容を、指定した宛先へすばやく送信できるようにすることを目的にしています。

Notemod-selfhosted と連携することで、次のような用途を想定しています

- PCでコピーしたテキストを、別端末（スマホ／別PC）へすぐ渡したい
- 定型の送信先（自分用メモ、作業用サーバー、チームの受け口など）へ素早く投げたい
- 「貼り付け先を探す」手間を減らして、思考を途切れさせずに作業したい

![](Notemod-selfhosted.png)

iPhoneショートカットを使用した例：

- iPhoneのショートカットを利用して、PC のクリップボードの内容を素早く取得
- iPhoneのショートカットを利用して、Notemod （PC）にクリップボードの内容を素早く送信

---

## 動作環境

- Windows 10 / 11
- .NET デスクトップ環境（WinForms）
  - 対象フレームワーク: .NET 8

---

## インストール

1. GitHub の Releases から最新版をダウンロード
 - [GitHub Releases](https://github.com/StayHomeLabNet/ClipboardSender/releases)  
2. 展開して `.exe` を実行

- 🇯🇵 [Why the exe file is large (日本語)](docs/exe_size_ja.md)
このアプリは .NET の自己完結型（self-contained）としてビルドされています。
.NET ランタイムを exe に同梱しているため、追加インストールは不要です。

そのため、ファイルサイズ（約150MB）は想定どおりのものです。

---

## 使い方

### 起動と基本動作

- 起動すると **システムトレイ** に常駐します
- トレイアイコンの状態で **送信が有効 / 無効** を判別できます

### 設定画面

![](ClipboardSender.png)

---

## ビルド（開発者向け）

### 必要なもの

- Visual Studio（例：Visual Studio 2026）
- 「.NET デスクトップ開発」ワークロード

### 手順

1. このリポジトリを clone
2. Visual Studio で `.sln` を開く
3. `Release` でビルド
4. 出力された `.exe` を実行

---

## アイコン（Resources 化）について

配布時に `Assets/` 等を同梱しなくても動くように、トレイアイコンは **Resources に内包**する運用を推奨しています
アプリアイコンは、本家Notemodのアイコンを利用させていただきました。

- 例：有効時 `tray_on.ico` / 無効時 `tray_off.ico`（名称はプロジェクトに合わせてOK）
- `Properties/Resources.resx` に追加して参照することで、`.exe` 単体配布がしやすくなります

---

## セキュリティ / プライバシー

- クリップボードには機密情報が含まれる場合があります
- 送信先の安全性（HTTPS、認証、保存方法）を確認した上で利用してください

---

## 謝辞

本プロジェクトは、Notemod から多くの学びを得ています  
その作者への敬意を表し、本アプリの表示言語に **トルコ語（TR）** を追加しています

---

## ライセンス

- MIT License
- `LICENSE` ファイルをリポジトリ直下に追加してください

---

## コントリビューション

Issue / Pull Request 大歓迎です

- バグ報告：再現手順、期待結果、実際の結果、OS・バージョンを添えてください
- 機能提案：ユースケース（なぜ必要か）を書くと議論が進みやすいです

---

## 連絡先 / リンク

- [Web](https://stayhomelab.net/)  
- [Help](https://stayhomelab.net/ClipboardSender)  
- [Notemod-selfhosted](https://github.com/StayHomeLabNet/Notemod-selfhosted)  
