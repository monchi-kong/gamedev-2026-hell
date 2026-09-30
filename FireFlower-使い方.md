# 火の玉を撃てるアイテム

Assets/Prefabs/FireFlowerBlock を Scene にドラッグすると、下から叩くと花が出るブロックを配置できます。
Assets/Prefabs/FireFlower はアイテムを直接配置するためのPrefabです。花はその場に留まります。
既存の ItemBlock の Item Prefab に FireFlower を指定する方法でも使えます。

花に触れて取得すると、X または J キーで向いている方向に火の玉を撃てます。
発射間隔は0.3秒です。火の玉は地面で跳ね、敵を倒し、壁や天井への接触または3秒経過で消えます。
能力は次のステージへ引き継ぎ、残機を失う・リトライ・新しいゲームを開始すると解除されます。
被弾時はこれまでどおり残機が減ります。

弾の Speed / Bounce Speed / Lifetime は Assets/Resources/Fireball の Inspector で変更できます。
このPrefabはコードから読み込むため、Resources 内の名前 Fireball を維持してください。

確認: 全スクリプトのコンパイル、Prefab内部参照、残機減少・リセット時の能力解除。
UnityのPlay Modeでの取得・弾の物理挙動は未確認です。
