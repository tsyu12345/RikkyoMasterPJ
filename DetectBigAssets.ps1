# 検索するディレクトリのパスを設定
$directoryPath = "./MAEasySimulator/Assets"

# 30MBをバイト単位で計算
$sizeThreshold = 30MB

# ディレクトリ内の全ファイルとフォルダのサイズを取得し、サイズが閾値を超えるものをフィルタリング
Get-ChildItem -Path $directoryPath -Recurse | ForEach-Object {
    $item = $_
    $itemSize = (Get-ChildItem -Path $item.FullName -Recurse -File | Measure-Object -Property Length -Sum).Sum
    if ($itemSize -gt $sizeThreshold) {
        # サイズが閾値を超えるアイテムの詳細を表示
        [PSCustomObject]@{
            Path = $item.FullName
            Size = "{0:N2} MB" -f ($itemSize / 1MB)
        }
    }
} | Sort-Object Size -Descending | Format-Table -AutoSize
