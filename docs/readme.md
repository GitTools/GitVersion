# GitVersion Documentation

This is the directory in which the
[GitVersion documentation hosted on GitHub Pages](https://gitversion.net/)
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

To serve up the documentation locally, you need to run the following build
command:

```shell
.\build.ps1 -Target Preview-Documentation
```

After pressing enter, the documentation will be generated and then served under
a local web server.  Information about the URL that can be used to view the docs
will be shown in the output.  Copy/paste this URL into a browser window.
