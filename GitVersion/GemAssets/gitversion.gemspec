Gem::Specification.new do |spec|
  spec.platform    = Gem::Platform::RUBY
  spec.name        = 'gitversion'
  spec.licenses    = ['MIT']
  spec.version     = '0.14.0'
  spec.files = Dir['bin/**/*']
  spec.bindir = 'bin'
  spec.executables << 'gitversion'

  spec.summary     = 'Derives SemVer information from a repository following GitFlow or GitHubFlow.'
  spec.description = <<-EOF 
Derives SemVer information from a repository following GitFlow or GitHubFlow.
EOF
  
  spec.authors           = ['NServiceBus','Simon Cropp']
  spec.email             = 'info@nservicebus.com'
  spec.homepage          = 'http://github.com/Particular/GitVersion'
  spec.rubyforge_project = 'GitVersion'
end