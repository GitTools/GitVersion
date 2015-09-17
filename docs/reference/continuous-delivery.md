# Continuous Delivery
Continuous delivery is the practice of having a deployment pipeline. Each stage of the pipeline gets the code going through the pipeline closer to production.

The topic itself is rather large, here we will just focus on the building and creation of *releasable* artifacts. This is only a part of continuous delivery as a whole, with the hard part being the ability to measure the impacts of what you have deployed into production.

In essence continuous delivery means:

 - Your code is automatically built and tested
 - If any of the automated tests fail, the teams #1 priority is to fix the build
 - If the build is green, the application can be deployed at any time
   - Ideally the business should make that decision
   - The same artifacts which were built and tested should be deployed
   - That means no rebuilding everything when you are deploying

Continuous delivery does not work well with GitFlow. The reason is that you are required to *merge* to master to do a release, triggering a rebuild and a new set of artifacts to go through your pipeline. Depending on how long your pipeline is, this could be a while.

GitHubFlow is a better fit for Continuous delivery, the [mainline development](mainline-development.md) model means that every merged feature branch will be built as a *stable* version and if the build/builds go green then you are free to deploy to production at any time.

## How continuous delivery affects GitVersion
The thing about continuous delivery is that there will be *multiple* candidates to deploy to production and it is a human choice to deploy. This means that GitVersion will build **the same semantic version** until that version is deployed.

GitVersion assumes Continuous Delivery by default in it's [configuration](../configuration.md), if you want to do [continuous deployment](continuous-deployment.md) then just change the configuration setting.

## Resources
 - [Continuous Delivery on Wikipedia](https://en.wikipedia.org/wiki/Continuous_delivery)
 - [Continuous Delivery, the book](http://www.amazon.com/Continuous-Delivery-Deployment-Automation-Addison-Wesley/dp/0321601912)
