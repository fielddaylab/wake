# GitHub Action Workflows


## Aqualab Build

The `main.yml` workflow is the main build for the project.  The `UNITY_VERSION` is currently 
hardcoded in this file.  The `UNITY_LICENSE` also needs to be added as a secret in the GitHub 
repository.  This can be done from 'Settings > Options > Secrets'.

This action currently outputs the WebGL Build folder as an artifact.

It also uploads the webgl build folder to a webserver via rsync.  The secrets that need to be 
created for this feature to work are:
* DEPLOY_DIR
* DEPLOY_HOST
* DEPLOY_KEY
* DEPLOY_USER
* VPN_PASSWORD
* VPN_USERNAME


The trigger for this action is a push. 
This build artifact is uploaded to a folder specific to this release version.
The WebGL build will be placed in DEPLOY_HOST:DEPLOY_DIR/BRANCH


## Unity Activation

This workflow shows how to grab the unity license for the GitHub action workflow.  The 
documentation for what to do with the license artifact that is produced from this workflow 
can be found on the [Unity CI Docs](https://unity-ci.com/docs/github/activation)

This worfklow only needs to be rerun when changing Unity versions 
