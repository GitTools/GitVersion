# set buildx as the default docker builder
docker buildx install

# create and use a docker buildx context
docker buildx create --name gitversion --use
docker context use gitversion

# install qemu static
docker run --rm --privileged multiarch/qemu-user-static --reset -p yes
