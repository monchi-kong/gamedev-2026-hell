# 当たり判定のない壁

Assets/Prefabs/BackgroundBlock を Scene にドラッグして配置します。
見た目だけのブロックなので、プレイヤー・敵・火の玉は通り抜けます。
足場や壁キックの対象にはなりません。

複製して並べると背景の壁を作れます。Transform の Scale で大きさも変更できます。
通常のブロックと区別するため少し暗くし、Order in Layer を -1 に設定しています。
色は Sprite Renderer の Color、表示順は Order in Layer で変更できます。

通常の地形と同じ位置に配置した場合、元の地形の当たり判定は残ります。
タイルを大量に塗る場合は専用の背景Tilemapを用意し、Colliderを付けずに使用してください。

## 通常のブロックと同じマス目に配置する

Assets/Textures/BackgroundBlockTile.asset を Tile Palette にドラッグして登録します。
通常の地形と同じ Grid の子として背景用 Tilemap を作り、そのローカル位置・回転・スケールを
既存の地形 Tilemap とそろえてください。既存の地形に位置のオフセットがある場合も同じ値にします。
背景用 Tilemap には Collider を付けず、Tilemap Renderer の Order in Layer を -1 にします。
Tile Palette の描画先に背景用 Tilemap を選び、このタイルを塗ります。
タイル自身も Collider Type = None のため、当たり判定を生成しません。
通常の地形と背景を重ねられるよう、描画先は分けてください。
