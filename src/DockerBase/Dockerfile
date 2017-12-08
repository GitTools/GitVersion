FROM ubuntu:16.04

MAINTAINER GitTools Maintainers <jake@ginnivan.net>

# moviong the timezone setting at the top, as this may be the most infrequent change in this file
RUN ln -sfn /usr/share/zoneinfo/GMT /etc/localtime

# Following current install guide from
# http://www.mono-project.com/download/#download-lin on 2017-12-08
# regarding to the repository sources
RUN echo "deb http://download.mono-project.com/repo/ubuntu xenial main" |\
    tee /etc/apt/sources.list.d/mono-official.list
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF &&\
    apt-get update &&\
    apt-get  -y --no-install-recommends install libcurl3 tzdata unzip curl git-all mono-complete &&\
    apt-get -yqq clean &&\
    rm -rf /var/lib/apt/lists/* /tmp/*
