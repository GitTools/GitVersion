FROM ubuntu:16.04

MAINTAINER GitTools Maintainers <jake@ginnivan.net>

# Wheezy doesn't support glibc 2.15 which libgit2sharp requires
# So we are going to install mono on ubuntu instead
RUN apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
RUN echo "deb http://download.mono-project.com/repo/debian wheezy main" | tee /etc/apt/sources.list.d/mono-xamarin.list
RUN apt-get update && apt-get install -y --no-install-recommends libcurl3 tzdata unzip curl git-all mono-complete && apt-get -yqq clean && rm -rf /var/lib/apt/lists/* /tmp/*
RUN cp /usr/share/zoneinfo/GMT /etc/localtime
