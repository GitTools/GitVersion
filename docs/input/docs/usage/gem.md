---
Order: 50
Title: Gem
---

Just a gem wrapper around the command line to make it easier to consume from Rake.

:::{.alert .alert-info}
**Note**

This is currently not being pushed. Please get in touch if you are
using this.
:::

If you want a Ruby gem version installed on your machine then you can use
[Bundler](http://bundler.io/) or [Gem](http://rubygems.org/) to install the
`gitversion` gem.

```ruby
gem install gitversion
```

The gem comes with a module to include in your Rakefile:

```ruby
require 'git_version'

include GitVersion

puts git_version.sha
```

Internally, this will call the `GitVersion.exe` that is bundled with the Ruby
gem, parse its JSON output and make all the JSON keys available through Ruby
methods. You can either use Pascal case (`git_version.InformationalVersion`) or
Ruby-style snake case (`git_version.informational_version`) to access the JSON
properties.

gitversion internally caches the JSON output, so `GitVersion.exe` is only called
once.

Any arguments passed to `git_version` will be passed to `GitVersion.exe`:

```ruby
require 'git_version'

include GitVersion

puts git_version('C:/read/info/from/another/repository').sha
```

:::{.alert .alert-info}
**Note**

Mono is not currently supported due to downstream dependencies on
libgit2. The Gem can only be used with the .NET framework
:::
