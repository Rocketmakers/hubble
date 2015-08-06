param (
    [string]$environment = "local"
)

npm install --silent
gulp --env $environment
