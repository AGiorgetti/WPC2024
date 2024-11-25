$platform = $args[0]
if (!$platform) {
	Write-Host "Missing Platform: windows | linux"
	exit(1)
}

if ($platform -eq "linux") {
    # there's a bug on stopping/terminating LCOW
    # https://github.com/moby/moby/issues/37919
    # workaround is to kill the containers manually
    Write-Host "LCOW stopping bug, hack: killing the containes (https://github.com/moby/moby/issues/37919)."
    docker kill -s 9 afsample-mongo-1
}
# do not use -f docker-compose.debug.override.yml volumes should stay up.
# they are not dismounted, they are defined in the override file
# otherwise they can be pruned
docker-compose -f docker-compose.${platform}.yml -p afsample down

# do not clean up the volumes, this should be a persistent environment