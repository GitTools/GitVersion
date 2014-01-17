Gem::Specification.new do |spec|
  spec.platform    = Gem::Platform::RUBY
  spec.name        = 'gitflowversion'
  spec.licenses    = ['MIT']
  spec.version     = '0.14.0'
  spec.files = Dir['bin/**/*']
  spec.bindir = 'bin'
  spec.executables << 'gitflowversion'

  spec.summary     = 'Derives SemVer information from a repository following GitFlow.'
  spec.description = <<-EOF 
Derives SemVer information from a repository following GitFlow.
EOF
  
  spec.authors           = ['NServiceBus','Simon Cropp']
  spec.email             = 'info@nservicebus.com'
  spec.homepage          = 'http://github.com/Particular/GitFlowVersion'
  spec.rubyforge_project = 'GitFlowVersion'
end