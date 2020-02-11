---
Order: 60
Title: Converting to GitFlow
---

Converting to GitFlow is simple. Whenever you need to convert, simply do the
following

```shell
git checkout master
git checkout -b develop
git push upstream develop
```

Afterwards you need to set `develop` to be your default branch. And now all
development happens on the `develop` branch
