Gem::Specification.new do |spec|
  spec.platform    = Gem::Platform::RUBY
  spec.name        = 'gitversion'
  spec.licenses    = ['MIT']
  spec.version     = '$version$'
  spec.summary     = 'Derives SemVer information from a repository following GitFlow or GitHubFlow.'
  spec.description = <<-EOF
Derives SemVer information from a repository following GitFlow or GitHubFlow.
EOF

  spec.authors           = ['NServiceBus','Simon Cropp']
  spec.email             = 'info@nservicebus.com'
  spec.homepage          = 'http://github.com/Particular/GitVersion'
  spec.rubyforge_project = 'GitVersion'

  spec.files         = Dir['bin/**/*', 'lib/**/*', '*.gemspec'].reject { |f| File.directory?(f) }
  spec.executables   = spec.files.grep(%r{^bin/}) { |f| File.basename(f) }.reject { |f| f =~ /\.(exe|pdb|dll)$/}
  spec.test_files    = spec.files.grep(%r{^(test|spec|features)/})
  spec.require_paths = ['lib']
end
