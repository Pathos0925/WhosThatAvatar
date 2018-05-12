# WhosThatAvatar
Find and view public VRChat avatars

## How To Use
Either: 
⋅⋅* Download the source and run it in the editor, or compile it in Unity 5.6.3p1 for Standalone or WebGL
⋅⋅* Use the web app here: http://whosthatavatar.com (with limited compatability, see below)
⋅⋅* Or download the compiled windows standalone player.

### Web App Limitation
The Unity WebGL web application currently has the following restriction
⋅⋅* Some avatars do not work for known and unknown reasons.
⋅⋅* Limited custom shaders. Most shaders will be replaced with the standard shader (or whichever you select in the settings)
⋅⋅* 6MB file limit. The CORS proxy cannot handle anything larger.


Every avatar is identified with a unique ID, which usually starts with avtr_
### Load an avatar from an avatar ID
Type the id in the search bar at the top of the application.
To copy and paste in the web application, as WebGL does not support copy and paste, after the web address, set the avatar querystring to the avatar ID you want to find. After "Avatar=" add the avatar ID. E.g http://whosthatavatar.com/?avatar=avtr_63340186-53cb-46e3-9a60-419da084c794


## FAQ
### Is this allowed?
VRChat's stance on public use of the VRChat API, per Tupper on the VRChat discord: 
> *Regarding reverse engineering our API - Our stance here is don't be malicious.  This is unsupported and it might break.*

