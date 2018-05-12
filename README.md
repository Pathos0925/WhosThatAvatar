# WhosThatAvatar
Find and view public VRChat avatars

## How To Use
Either: 
* Download the source and run it in the editor, or compile it in Unity 5.6.3p1 for Standalone or WebGL
* Use the web app here: http://whosthatavatar.com (with limited compatability, see below)
* Or download the compiled windows standalone player.

### Controls
* After loading avatar, Hold left mouse button and drag to rotate around the avatar.
* Hold right mouse button and drag to pan up and down.
* Use the scroll wheel to zoom in and out.

### Find and Browse Avatars
* Press the "Find and Avatar" Button in the top right.
* Currently only browsing is supported, but a search feature is planned soon.

### Web App Limitation
The Unity WebGL web application currently has the following restriction
* Some avatars do not work for known and unknown reasons.
* Limited custom shaders. Most shaders will be replaced with the standard shader (or whichever you select in the settings)
* 6MB file limit. The CORS proxy cannot handle anything larger. (In actuality I think its more like ~4MB)
* No copy and paste. 
#### To past an avatar ID into the web app, 
  1. set the avatar querystring to the avatar ID you want to find. After "Avatar=" add the avatar ID. 
      E.g http://whosthatavatar.com/?avatar=avtr_63340186-53cb-46e3-9a60-419da084c794



### Load an avatar from an avatar ID
Every avatar is identified with a unique ID, which usually starts with avtr_


## FAQ
### Is this allowed?
VRChat's stance on public use of the VRChat API, per Tupper on the VRChat discord: 
> *Regarding reverse engineering our API - Our stance here is don't be malicious.  This is unsupported and it might break.*
Along those lines, at preasent, only public avatars can be viewed. 

### I dont want my avatar viewable
Add an empty child gameobject to your avatar with the name "WTA_IGNORE".
Instead of being viewed your avatar will give an error.
