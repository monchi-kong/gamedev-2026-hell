# 明るい森の背景レイヤー

元絵 BrightForest-v1.png に合わせて再構成した3枚のPNGです。
元画像を機械的に分解したものではなく、隠れていた箇所を生成で補っています。

全画像: 1208 × 1302 px / 同一キャンバス / 中央ピボット / 100 Pixels Per Unit
Unity設定: Sprite (2D and UI)、Single、Point、Mip Mapsなし。

奥から順に、同じPosition・Scaleで重ねてください。
- 01_Sky.png: 空と雲。不透明。Order in Layer = -30
- 02_DistantForest.png: 遠景の森。透過PNG。Order in Layer = -20
- 03_Foreground.png: 手前の木・茂み・草・土。透過PNG。Order in Layer = -10

プレイヤーより背景側になるSorting Layerを使用してください。
既存のParallaxBackgroundを付ける場合、Parallax Factorが大きいほどカメラに追従するため遠景向けです。
初期調整例: 空0.9 / 遠景0.6 / 手前0.3。
左右端のシームレス接続は保証していないため、Loopはオフから確認してください。
ジャンプ時の上下の余白はカメラの可動範囲に合わせてScaleとPositionを調整してください。
既存ステージの差し替えは行っていません。