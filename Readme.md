# リポジトリクローン後の初期設定

## 1.動作環境
本プロジェクトは下記の環境での動作を前提とします。

* OS: Windows 11
* Python: 3.9.6
* Unity: 2021.3.25f1
* ML-Agents: release_20
* CUDA: 11.7.0

その他Pythonランタイムや、依存パッケージのバージョンについては、後述のrequirements.txtなどを参照してください。


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

### 3.1 Python仮想環境の構築
本プロジェクトでは、Windows環境を前提に、Pythonの仮想環境を使用しています。
なお、Pythonのバージョンは3.9.6(.python-versionに記載)を使用しています。

仮想環境の構築には、以下のコマンドを使用します。
```bash
python -m venv .venv
```
これにより、.venvディレクトリが作成されます。

### 3.2 Python仮想環境の有効化
仮想環境を有効化するには、以下のコマンドを使用します。
```bash
.venv\Scripts\activate

# PowerShellの場合
.venv\Scripts\Activate.ps1
```
これにより仮想環境が有効化され、プロンプトの先頭に(.venv)が追加されます。
以後の作業は、仮想環境が有効化された状態で行ってください。

### 3.3 依存パッケージのインストール
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
これは、PyTorchのインストールに失敗したことを意味しますが、PyTorchのインストールは後述の手順で行うので、このエラーは無視してください。

### 3.3.1 PyTorchのインストール
本プロジェクトでは、CUDA版のPyTorchを使用しています。
そのため、通常のpip installではインストールできないため、以下のコマンドを使用して、PyTorchをインストールしてください。
```bash
pip install torch torchvision torchaudio --index-url https://download.pytorch.org/whl/cu117
```
### 3.4 大容量ファイルのダウンロード
本プロジェクトでは、PLATEAUの大容量ファイルを扱うために、一部の依存ファイルをGoogle Driveに置いています。
該当のファイルをダウンロードし、【2.大容量ファイルの扱い】に記載のディレクトリに配置してください。

当該のファイルは、以下のGoogle Driveからダウンロードできます。
* https://drive.google.com/drive/folders/11aYBX--Aq5o3j_lfgYaOccpdMPw6VTEe?usp=sharing








