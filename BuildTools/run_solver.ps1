<#
  솔버를 배치모드로 굴리고 결과를 json·csv 로 남긴다.
  Unity 로그는 파일로 받으면서 [SolverBatch] 줄만 콘솔에 흘린다 —
  전부 흘리면 임포트 로그에 묻혀 형편이 안 보인다.

  -All 이 없으면 이 파일 옆 targetStage.txt 의 첫 줄을 굴린다.
  csv 가 떨어질 자리는 CsvDirectory.txt 에 상대 경로로 적는다.
  비면 SolverReports\csv 로 간다.

  취소해도 헤드리스 Unity 가 남지 않는다 — 잡(Job) 에 넣어 둔다.
#>
param([switch]$All)

$unity = "C:\Program Files\Unity\Hub\Editor\6000.3.13f1\Editor\Unity.exe"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Split-Path -Parent $here
$out = Join-Path $root "SolverReports"

if (-not (Test-Path $unity)) {
    Write-Host "Unity 를 못 찾았다 - $unity"
    exit 1
}

<#
  잡 핸들을 이 프로세스가 들고 있으면, 어떻게 끝나든 —
  Ctrl+C, 콘솔 창 닫기, 강제 종료 — 윈도우가 잡에 든 것을
  전부 죽인다. try/finally 는 창을 닫는 경우를 못 막는다.
#>
Add-Type -Namespace PPS -Name Job -MemberDefinition @'
[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
static extern IntPtr CreateJobObject(IntPtr attr, string name);

[DllImport("kernel32.dll")]
static extern bool SetInformationJobObject(IntPtr job, int cls, IntPtr info, uint len);

[DllImport("kernel32.dll")]
static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

[StructLayout(LayoutKind.Sequential)]
struct Basic
{
    public long PerProcessUserTimeLimit;
    public long PerJobUserTimeLimit;
    public uint LimitFlags;
    public UIntPtr MinimumWorkingSetSize;
    public UIntPtr MaximumWorkingSetSize;
    public uint ActiveProcessLimit;
    public UIntPtr Affinity;
    public uint PriorityClass;
    public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
struct Io
{
    public ulong R, W, O, RT, WT, OT;
}

[StructLayout(LayoutKind.Sequential)]
struct Extended
{
    public Basic Basic;
    public Io Io;
    public UIntPtr ProcessMemoryLimit;
    public UIntPtr JobMemoryLimit;
    public UIntPtr PeakProcessMemoryUsed;
    public UIntPtr PeakJobMemoryUsed;
}

/// JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE.
const uint KillOnClose = 0x2000;

/// JobObjectExtendedLimitInformation.
const int ExtendedLimit = 9;

public static IntPtr Create()
{
    IntPtr job = CreateJobObject(IntPtr.Zero, null);
    if (job == IntPtr.Zero) return IntPtr.Zero;

    var ext = new Extended();
    ext.Basic.LimitFlags = KillOnClose;

    int size = Marshal.SizeOf(ext);
    IntPtr info = Marshal.AllocHGlobal(size);

    try
    {
        Marshal.StructureToPtr(ext, info, false);
        if (!SetInformationJobObject(job, ExtendedLimit, info, (uint)size))
            return IntPtr.Zero;
    }
    finally { Marshal.FreeHGlobal(info); }

    return job;
}

public static bool Add(IntPtr job, IntPtr process)
{
    return job != IntPtr.Zero && AssignProcessToJobObject(job, process);
}
'@

<#
  빈 줄과 # 로 시작하는 줄을 건너뛰고 첫 줄만 돌려준다.
  파일이 없거나 쓸 줄이 없으면 빈 문자열이다.
#>
function Read-First([string]$path) {
    if (-not (Test-Path $path)) { return "" }

    $line = Get-Content $path |
        Where-Object { $_.Trim() -ne "" -and -not $_.Trim().StartsWith("#") } |
        Select-Object -First 1

    if ($line) { return $line.Trim() }
    return ""
}

# 굴릴 스테이지. 전체면 이름만 All 로 두고 -stage 를 안 준다.
if ($All) {
    $name = "All"
    $stage = @()
}
else {
    $list = Join-Path $here "targetStage.txt"

    if (-not (Test-Path $list)) {
        Write-Host "targetStage.txt 가 없다 - $list"
        exit 1
    }

    $name = Read-First $list

    if (-not $name) {
        Write-Host "targetStage.txt 에 스테이지 이름이 없다."
        exit 1
    }

    $stage = @("-stage", $name)
}

# 저장소 뿌리에서 본 상대 경로다 — 절대 경로로 적으면
# 사람마다 갈려 파일을 함께 둘 수 없다.
$csvDir = Read-First (Join-Path $here "CsvDirectory.txt")

if ($csvDir) { $csvDir = Join-Path $root $csvDir }
else { $csvDir = Join-Path $out "csv" }

foreach ($dir in @("json", "log")) {
    $path = Join-Path $out $dir
    if (-not (Test-Path $path)) { New-Item -ItemType Directory -Path $path | Out-Null }
}

# 손으로 적은 경로라 오타가 나도 몇 분 굴린 뒤에야 드러난다. 먼저 본다.
if (-not (Test-Path $csvDir)) {
    New-Item -ItemType Directory -Path $csvDir -ErrorAction SilentlyContinue | Out-Null
}

if (-not (Test-Path $csvDir)) {
    Write-Host "csv 폴더를 만들지 못했다 - $csvDir"
    Write-Host "CsvDirectory.txt 의 경로를 본다."
    exit 1
}

$json = Join-Path $out "json\$name.json"
$log = Join-Path $out "log\$name.log"
$csv = Join-Path $csvDir $name

# 지난 로그가 남아 있으면 이번 것과 섞여 보인다.
if (Test-Path $log) { Remove-Item $log }

<#
  로그에 새로 붙은 줄을 읽어 필요한 것만 찍고, 어디까지 읽었는지 돌려준다.
  Unity 가 쥐고 있는 파일이라 공유해서 연다.
#>
function Show-New([string]$path, [long]$pos) {
    if (-not (Test-Path $path)) { return $pos }

    $file = [System.IO.File]::Open($path, "Open", "Read", "ReadWrite")

    try {
        if ($file.Length -lt $pos) { $pos = 0 }
        $null = $file.Seek($pos, "Begin")

        $reader = New-Object System.IO.StreamReader($file)
        $text = $reader.ReadToEnd()
        $pos = $file.Position
    }
    finally {
        $file.Dispose()
    }

    foreach ($line in ($text -split "`r?`n")) {
        # 컴파일 오류는 [SolverBatch] 가 안 붙는다. 그것도 보여야 한다.
        if ($line.Contains("[SolverBatch]") -or $line.Contains("error CS")) {
            Write-Host $line
        }
    }

    return $pos
}

Write-Host "[1/2] 솔버를 굴린다 - $name"
Write-Host "      로그 전체는 $log"

$unityArgs = @(
    "-batchmode", "-nographics",
    "-projectPath", $root,
    "-executeMethod", "PPS.Solver.Batch.SolverBatch.Run"
) + $stage + @(
    "-out", $json,
    "-logFile", $log
)

$job = [PPS.Job]::Create()
$unityProcess = Start-Process -FilePath $unity -ArgumentList $unityArgs -PassThru -NoNewWindow

if (-not [PPS.Job]::Add($job, $unityProcess.Handle)) {
    Write-Host "경고: 잡에 넣지 못했다. 취소하면 Unity 가 남을 수 있다."
}

$pos = 0

while (-not $unityProcess.HasExited) {
    Start-Sleep -Milliseconds 400
    $pos = Show-New $log $pos
}

$unityProcess.WaitForExit()
$pos = Show-New $log $pos

if ($unityProcess.ExitCode -ne 0) {
    Write-Host "솔버가 실패했다 (코드 $($unityProcess.ExitCode)). 로그를 본다 - $log"
    exit 1
}

Write-Host "[2/2] csv 로 바꾼다 - $csvDir"
& python (Join-Path $here "solver_report_csv.py") $json -o $csv

if ($LASTEXITCODE -ne 0) {
    Write-Host "csv 변환이 실패했다."
    exit 1
}

Write-Host ""
Write-Host "끝. json - $json"
exit 0
