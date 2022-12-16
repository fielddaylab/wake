# Field Day GitHub-CI Action Workflows

## Unity WebGL Build `WebGL.yml` 
This workflow will download Unity and create a WebGL build of the project, then capture the build as an artifact and deploy to a specificed host behind a GlobalProtect Firewall. 

This process requires various "secrets" to be configured.

Organizational Secrets:

* FIELDDAY_UNITY_LICENSE_2019 (see 'Unity Activiation' Below) 
* FIELDDAY_WISC_EDU_DEPLOY_HOST : The host to deploy to, like "fielddaylab.wisc.edu"
* FIELDDAY_WISC_EDU_DEPLOY_KEY : The ssh private key to login to the DEPLOY_HOST
* FIELDDAY_WISC_EDU_DEPLOY_USER : The ssh user to login to the DEPLOY_HOST 
* FIELDDAY_VPN_PASSWORD : The password to login to the VPN
* FIELDDAY_VPN_USERNAME : The user to login to the VPN

Project Secrets:

* DEPLOY_DIR : The directory to deploy to, like "/httpdocs/play/game/ci"

UNITY_VERSION is hardcoded into this file and is linked to the activation code. 

The trigger for this action is any push to any branch in the repository.

This build artifact is uploaded to a folder specific to this release version.
The WebGL build will be placed in DEPLOY_HOST:DEPLOY_DIR/BRANCH

## Unity Activation `unity-activation.yml`

[Should be disabled after first use]

This workflow shows how to grab the unity license for the GitHub action workflow.  The 
documentation for what to do with the license artifact that is produced from this workflow 
can be found on the [Unity CI Docs](https://unity-ci.com/docs/github/activation)

This worfklow only needs to be rerun when changing Unity versions 
