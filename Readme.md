# リポジトリクローン後の初期設定

## 1.動作環境
本プロジェクトは下記の環境での動作を前提とします。

* OS: Windows 11
* Python: 3.9.6
* Unity: 2021.3.25f1
* ML-Agents: release_20
* CUDA: 11.7.0

その他Pythonランタイムや、依存パッケージのバージョンについては、Pipfileやrequirements.txtを参照してください。


## 2.大容量ファイルの扱い
本プロジェクトでは、PLATEAUの大容量ファイルを扱うために、一部の依存ファイルをGoogle Driveに置いています。
当初はGit LFSを使用するはずでしたが、GitHubの制限により、Git LFSを使用することができませんでした。

以下のファイルに関して、clone後にGoogle Driveからダウンロードする必要があります。
全体の手順は、後述する「3.clone後の流れ」に記載しています。

* 別途ダウンロードが必要なファイル群
    * "./DroneSimulator/Assets/PLATEAU_TEST2.unity"
    * "./PLATEAU-SDK-for-Unity-v1.1.2.tgz"

## 3.clone後の流れ
【1.動作環境】の準備が整ったら、以下の手順でclone後の初期設定を行ってください。

## 3.1 Python仮想環境の構築
本プロジェクトでは、Windows環境を前提に、Pythonの仮想環境を使用しています。
なお、Pythonのバージョンは`3.9.6`(.python-versionに記載)を使用しています。

仮想環境の構築手順は各自行うことを推奨します。ここでは、
1. `venv`モジュール
2. `pipenv` モジュール

を使う場合の２通りの方法を記載します。

### `venv`を使用する場合

#### 1. 仮想環境の構築

以下のコマンドをお使いのターミナルで実行してください。
```bash
python -m venv .venv

or

python3 -m venv .venv
```
これにより、プロジェクトルートに`.venv`ディレクトリが作成されます。

#### 2. Python仮想環境の有効化

仮想環境を有効化するには、以下のコマンドを使用します。
```bash
.venv\Scripts\activate

# PowerShellの場合
.venv\Scripts\Activate.ps1

...after
(.venv) your\project\path> $
```
これにより仮想環境が有効化され、プロンプトの先頭に(.venv)が追加されます。
以後の作業は、仮想環境が有効化された状態で行ってください。

#### 3. PyTorchのインストール

本プロジェクトでは、CUDA版のPyTorchを使用しています。
そのため、通常の`pip install hoge` の形式ではインストールできないため、以下のコマンドを使用して、PyTorchをインストールしてください。
```bash
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu117
```
#### 4 その他の依存パッケージのインストール
依存パッケージのインストールには、以下のコマンドを使用します。
```bash
pip install -r requirements.txt
```
本プロジェクトで使用する依存パッケージは、すべてrequirements.txtに記載しています。
なお、今後の開発により、このパッケージリストは変更される可能性があります。その際は、上のコマンドを使用して、依存パッケージを更新してください。

なお、コマンド実行後は以下のエラーが発生する場合があります。
```bash
ERROR: Could not find a version that satisfies the requirement torch==2.0.1+cu117 (from versions: 1.7.1, 1.8.0, 1.8.1, 1.9.0, 1.9.1, 1.10.0, 1.10.1, 1.10.2, 1.11.0, 1.12.0, 1.12.1, 1.13.0, 1.13.1, 2.0.0, 2.0.1)
ERROR: No matching distribution found for torch==2.0.1+cu117
```
これは、PyTorchのインストールに失敗したことを意味しますが、PyTorchのインストールは前述の手順で行うので、このエラーは無視して手順3.3をもう一度実行して下さい。


### `pipenv` を使用する場合

#### 1. `pipenv`のインストール

`pipenv`は、Pythonの仮想環境を構築するためのモジュールです。標準にはインストールされていないため、以下のコマンドを使用してインストールしてください。
```bash
pip install pipenv
```
また環境によってはpyenvをインストールする必要があります。`pipenv`は内部で`pyenv`を使用し任意のPythonバージョンを使用します。そのため、`pyenv`がインストールされていない場合は、以下のコマンドを使用してインストールしてください。
```bash
pip install pyenv-win
```
#### 2. 仮想環境の構築

`pipenv`を使用して仮想環境を構築します。
以下のコマンドを実行してください。
```bash
pipenv --python 3.9.6
```
これにより`3.9.6`環境の仮想環境が構築されます。

次に、構築した仮想環境を有効化します。
```bash
pipenv shell
```
実行後はプロンプトの先頭に`(virtual env name)`が付与され、仮想環境が有効化されます。

#### 3. 依存パッケージのインストール
Pipfileに記載の依存関係を解決し、パッケージをインストールします。
```bash
pipenv install --verbose
```

以上で環境構築は完了です。

## 4.学習の再開
既存モデルに追加学習を行う場合は、以下のコマンドを使用してください。
```bash
mlagents-learn path/to/your/config.yaml --run-id=yourModelName --resume
```

## 5.学習結果の確認
TensorBoardを使用して、学習結果を確認することができます。
以下のコマンドを使用して、TensorBoardを起動してください。
```bash
tensorboard --logdir your/model/dir
```








