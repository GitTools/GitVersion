---
Order: 10
Title: Manual Deployment
Description: |
    The Manual Deployment mode can be used to remain on the same pre-released
	version until it has been deployed dedicatedly.
	
RedirectFrom: docs/reference/versioning-modes/manual-deployment
---

Having not the necessity to deploy the build artifacts on every commit is an
indecation of using the __Manual Deployment__ mode. This mode can be used to
remain on the same pre-released version until it has been deployed dedicatedly.

## How Manual Deployment affects GitVersion

The thing about manual deployment is that there will be _multiple_ candidates
to deploy on testing and it is a human choice to deploy. This means that
GitVersion will build **the same semantic version** until that version is
deployed. For instance:

* 1.1.0-2+1
* 1.1.0-1+2 (tag: 1.1.0-1) <-- This is the version which has been deployed on testing
* 1.1.0-1+1
* 1.1.1-1+0

Tags are required in this mode to communicate when the release is done as it's
an external manual process.

## Resources

* [Configuration][configuration]
* [Continuous Delivery on Wikipedia][wikipedia]
* [Continuous Delivery, the book][book]

[configuration]: /docs/reference/configuration
[book]: https://www.amazon.com/Continuous-Delivery-Deployment-Automation-Addison-Wesley/dp/0321601912
[wikipedia]: https://en.wikipedia.org/wiki/Continuous_delivery
