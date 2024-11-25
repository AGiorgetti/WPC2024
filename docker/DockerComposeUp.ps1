$platform = $args[0]
if (!$platform) {
    Write-Host "Missing Platform: windows | linux"
    exit(1)
}

Write-Host "Run the Debug environment"

docker-compose -f docker-compose.${platform}.yml -p afsample up --detach