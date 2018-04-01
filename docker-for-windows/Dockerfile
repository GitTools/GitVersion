# escape=`
ARG OS_IMAGE_TAG=1709
FROM microsoft/windowsservercore:$OS_IMAGE_TAG AS downloader
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $InformationPreference = 'Continue'; $ProgressPreference = 'SilentlyContinue';"]

ARG GV_VERSION=3.6.5
ARG GV_VERSION_ZIP=$GV_VERSION

# Download GitVersion from github release url

RUN $zipFile = 'GitVersion_{0}.zip' -f $env:GV_VERSION_ZIP; `
    $url = 'https://github.com/GitTools/GitVersion/releases/download/v{0}/GitVersion_{1}.zip' -f $env:GV_VERSION, $env:GV_VERSION_ZIP; `
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; `
    Invoke-WebRequest $url -OutFile $zipFile -UseBasicParsing; `
    Expand-Archive $zipFile -DestinationPath C:/GitVersion;


# app image
ARG OS_IMAGE_TAG=1709
FROM microsoft/windowsservercore:$OS_IMAGE_TAG

COPY --from=downloader C:/GitVersion/ C:/GitVersion/

WORKDIR C:/repo

# Make git repo on host available in container
VOLUME C:/repo

ENTRYPOINT ["powershell", "C:/GitVersion/GitVersion.exe"]