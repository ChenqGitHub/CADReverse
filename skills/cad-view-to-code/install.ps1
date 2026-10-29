# 安装 / 更新 cad-view-to-code 到用户级 skill 目录
#
#   pwsh -File skills\cad-view-to-code\install.ps1
#
# 仓库里的这份是源，安装目录是它的镜像；改完源后重跑本脚本即可同步。
# 不用符号链接，是为了让安装结果自成一体（换机器、只拷安装目录也能用）。

param(
    [string]$Target = (Join-Path $env:USERPROFILE '.agents\skills\cad-view-to-code')
)

$ErrorActionPreference = 'Stop'
$src = Split-Path -Parent $MyInvocation.MyCommand.Path

if (-not (Test-Path (Join-Path $src 'SKILL.md'))) {
    throw "找不到 SKILL.md：$src"
}

$head = Get-Content (Join-Path $src 'SKILL.md') -TotalCount 6 -Encoding utf8
if (-not ($head -contains '---') -or -not ($head -match '^name:')) {
    throw 'SKILL.md 缺少 frontmatter（--- name: ... description: ... ---）'
}

New-Item -ItemType Directory -Force -Path $Target | Out-Null

# /MIR 让安装目录与源完全一致；排除字节码缓存
robocopy $src $Target /MIR /XD "$src\scripts\__pycache__" /XF '*.pyc' /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -ge 8) { throw "robocopy 失败，退出码 $LASTEXITCODE" }
$global:LASTEXITCODE = 0

# robocopy 的 /XD 对相对名不一定生效，落地后再清一遍缓存（自检也会重新生成）
Get-ChildItem $Target -Recurse -Directory -Filter '__pycache__' -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem $Target -Recurse -File -Filter '*.pyc' -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

$files = Get-ChildItem $Target -Recurse -File
Write-Host ("已安装 -> {0}" -f $Target)
Write-Host ("  {0} 个文件，{1:N1} KB" -f $files.Count, (($files | Measure-Object Length -Sum).Sum / 1KB))
foreach ($f in $files) { Write-Host ("  " + $f.FullName.Substring($Target.Length + 1)) }

# 自检：脚本能起来、frontmatter 能被解析
$py = Get-Command python -ErrorAction SilentlyContinue
if (-not $py) { $py = Get-Command py -ErrorAction SilentlyContinue }
if ($py) {
    $ok = & $py.Source (Join-Path $Target 'scripts\ocs_regions.py') --help 2>&1 | Select-String 'capture'
    if (-not $ok) { throw '安装后的 ocs_regions.py 无法运行' }
    Write-Host '  自检通过：ocs_regions.py --help'
} else {
    Write-Host '  跳过自检：PATH 上没有 python（使用时需要 python 3.10+）'
}
