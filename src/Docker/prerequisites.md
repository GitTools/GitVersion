# Run with qemu

## set buildx as the default docker builder

```bash
docker buildx install
```

## create and use a docker buildx context

```bash
docker buildx create --name gitversion --use
docker context use gitversion
```

## install qemu static

```bash
docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
```
