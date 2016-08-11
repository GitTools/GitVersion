# GitVersion Documentation

This is the directory in which the
[GitVersion documentation hosted on ReadTheDocs](http://gitversion.readthedocs.io/en/latest/)
resides.

## Contributing

Improvements to the documentation is highly welcomed and is as easy
as finding the `.md` file you want to change and editing it directly within
GitHub's web interface.

If you want to do more elaborate changes, we would appreciate if you could test
the documentation locally before submitting a pull request. This involves
[forking](https://guides.github.com/activities/forking/) this repository and
then serving up the documentation locally on your machine, clicking around in
it to ensure that everything works as expected.

## Serving the documentation locally

To serve up the documentation locally, you need to
[install MkDocs](http://www.mkdocs.org/#installation) and then at the root of
the GitVersion project write the following in a command line window:

```shell
mkdocs serve
```

After pressing enter, something similar to the following lines should appear:

```
INFO    -  Building documentation...
INFO    -  Cleaning site directory
[I 160810 10:48:18 server:281] Serving on http://127.0.0.1:8000
```

If it says `Serving on http://127.0.0.1:8000`, you should be able to navigate
your favorite browser to `http://127.0.0.1:8000` and browse the documentation
there. If you have any problems with this process, please consult the
[MkDocs documentation](http://www.mkdocs.org/).