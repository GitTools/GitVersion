Gem::Specification.new do |spec|
    spec.platform    = Gem::Platform::RUBY
    spec.name        = 'gitversion'
    spec.licenses    = ['MIT']
    spec.version     = '$version$'
    spec.summary     = 'Easy Semantic Versioning (http://semver.org) for projects using Git'
    spec.description = <<-EOF
    Versioning when using git, solved. GitVersion looks at your git history and works out the semantic version of the commit being built.
  EOF

    spec.authors  = ['GitTools and Contributors']
    spec.homepage = 'https://github.com/GitTools/GitVersion'

    spec.files         = Dir['bin/**/*', 'lib/**/*', '*.gemspec'].reject { |f| File.directory?(f) }
    spec.executables   = spec.files.grep(%r{^bin/}) { |f| File.basename(f) }.reject { |f| f =~ /\.(exe|pdb|dll|so|dylib|config)$/}
    spec.test_files    = spec.files.grep(%r{^(test|spec|features)/})
    spec.require_paths = ['lib']
  end
