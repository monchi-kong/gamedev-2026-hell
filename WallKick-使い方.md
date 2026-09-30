# 壁キック

空中で固い壁に触れているときにスペースキーを押すと、壁と反対方向へ跳びます。
着地している場合は通常ジャンプになります。押しっぱなしでは連続発動しません。
左右どちらの壁でも使えます。敵・天井・下から通り抜ける足場は壁キックの対象外です。
壁は Player Controller の Ground Layer に含まれるレイヤーに設定してください。

Player Controller > Wall Kick で調整できます。
- Wall Kick Speed: 横方向に跳び離れる速度。初期値7。
- Wall Kick Jump Multiplier: 通常ジャンプの上向き速度に対する倍率。初期値1。
- Wall Kick Control Lock: キック直後の横入力を抑える時間。初期値0.18秒。

コンパイルは確認済みです。UnityのPlay Modeで左右の壁・床との角・移動足場との組み合わせの動作確認は未実施です。
