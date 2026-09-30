# 下から通り抜けられる薄い足場

Project ウィンドウの Assets/Prefabs/OneWayPlatform を Scene にドラッグして配置します。
初期サイズは幅 2、高さ 0.25。横幅は Transform の Scale X で調整できます。
下からジャンプすると通り抜け、上から落ちると着地し、その上から再ジャンプできます。

Ground レイヤー、BoxCollider2D の Used By Effector、PlatformEffector2D の
Use One Way を設定済みです。通常の地面の Tilemap とは別オブジェクトとして配置してください。
足場のある場所に通常の固いブロックが重なると、そちらの衝突で通り抜けできなくなります。
