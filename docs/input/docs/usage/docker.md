---
Order: 50
Title: Docker
Description: |
    Use GitVersion through one of its many published Docker containers.
---

GitVersion can be used through one of its many published Docker
containers. The list of available containers can be found on
[Docker Hub][docker-hub]. Once you've found the image you want to use,
you can run it like this:

```shell
docker run --rm --volume "$(pwd):/repo" gittools/gitversion:6.0.0-beta.3-fedora.36-6.0-arm64 /repo
```

The above command will run GitVersion with the current directory
mapped to `/repo` inside the container (the `--volume "$(pwd):/repo"`
part). The `/repo` directory is then passed in as the argument
GitVersion should use to calculate the version.

The `--rm` flag will remove the container after it has finished
running.

[Explore GitVersion on Docker Hub][docker-hub]{.btn .btn-primary}

[docker-hub]: https://hub.docker.com/r/gittools/gitversion
