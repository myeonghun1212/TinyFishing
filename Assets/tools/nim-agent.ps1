param(
    [Parameter(Mandatory=$true)]
    [string]$Prompt
)

codex exec `
    -p nemotron `
    --skip-git-repo-check `
    $Prompt