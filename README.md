# mdBookGitAutoBuild
A simple app for automatic [mdBook](https://github.com/rust-lang/mdBook) building from a git repository

Do you use mdBook ?  
Don't want to manually build and update files on a web host ?

This is a solution for that.

This app can pull a Git repository, rebuild mdBook and host the files.

For ease of use it comes as a [docker container](https://hub.docker.com/r/lukassoo/mdbook-git-auto-build) that you configure with the run command:

    docker run -d --name mdBookGitAutoBuilder \
    -e GIT_REPO_LINK=<YOUR_GIT_REPO_LINK> \
    -e USE_PULL_ON_INTERVAL='1' \
    -e REPO_PULL_INTERVAL_HOURS='24' \
    -v /root/.ssh:/root/.ssh \
    -p 80:80 \
    lukassoo/mdbook-git-auto-build:latest

1. (Optional) You can give the container a different name - If you have multiple different docs for example you can name it something like "mdBookGitAutoBuilder-Project1"
2. Replace **<YOUR_GIT_REPO_LINK>** with the **SSH** link to your mdBook git repo - the repo should contain the book.toml file and "src" folder
3. (Optional) Adjust environment variables:
   - USE_PULL_ON_INTERVAL='1' - Uses time intervals to pull the repo
   - REPO_PULL_INTERVAL_HOURS='24' - Time (in hours) between pulls - 24 by default
   - USE_WEB_HOOK='1' - Activates the web hook request listener on port 8080 that listens on the path "/hook" (that is inside the container, you can adjust the bindings outside)
4. (Optional) Adjust .ssh mount point - By default the SSH keys are stored in the default /root/.ssh directory on the host, if you keep other keys there you should change that.  
The keys are kept on the host to prevent constant re-generation and to allow easy access to read or swap them.
5. (Optional) Adjust port bindings - defaults are:
   - 80:80 for the web server hosting mdBook (-p 80:80)
   - 8080:8080 for the web hook - **add if you use it with USE_WEB_HOOK (-p 8080:8080)**
   - If you want to start multiple instances you will have to adjust host side bindings

Use with web hook:

    docker run -d --name mdBookGitAutoBuilder \
    -e GIT_REPO_LINK=<YOUR_GIT_REPO_LINK> \
    -e USE_WEB_HOOK='1' \
    -v /root/.ssh:/root/.ssh \
    -p 80:80 \
    -p 8080:8080 \
    lukassoo/mdbook-git-auto-build:latest

Either USE_PULL_ON_INTERVAL or USE_WEB_HOOK must be used, else there is no point in even running since there will never by any pulling/updating  
You can use both but using a web hook makes interval pulling not necessary

Using a web hook also opens up the possibility that anything on the web can just spam requests, not just GitHub since there is no validation/checks in place.  
However usually listening on a non-standard port that nothing common uses is enough to have close to no spam traffic so it is usually fine.  
Also after pulling there is a check for last commit time so if they are still the same then mdBook will not be rebuilt.

Using the time interval method doesn't listen to requests and only the port 80 web server that is serving mdBook is active.

## First start

On the first start a few things will happen:
1. Checks if git and ssh-keygen are available (just to validate that we can run)
2. Check if the SSH keys are generated - if not, we generate new ones and exit:

Output of "docker logs \<containerID\>":

![image](https://github.com/lukassoo/mdBookGitAutoBuild/assets/10761509/528d463a-147e-43c8-9ebc-6c1db80d6913)

If you want to access private repositories you have to go and add this public key to the account so that this auto builder can access the repository.  
The builder will only ever need read-only access so a dedicated account with read-only access for the target repo is recommended.

If you want to access a public repository this is not necessary.

Once the keys are generated and used where they have to be you can start the container again.  
This time it will proceed and attempt to clone the repository, build mdBook and start serving it.

Errors will be written to the log if anything goes wrong.
