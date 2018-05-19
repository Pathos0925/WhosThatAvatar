# WhosThatAvatar
* Find and view public VRChat avatars.
* Search public worlds for avatars by name, author, or description. (Experimental)

## How To Use
EITHER: 
* Download the source (compatable with Unity 5.6.3p1.) and run it in the Unity editor (Recommended. Highest compatability), or compile it for Standalone or WebGL.
* OR Use the web app here: https://whosthatavatar.com (with limited compatability, see below)
* OR download the compiled windows standalone player.

### Controls
* After loading an avatar, Hold left mouse button and drag to rotate around the avatar.
* Hold right mouse button and drag to pan up and down.
* Use the scroll wheel to zoom in and out.

### Find and Browse Avatars
* Press the "Find and Avatar" Button in the top right.
* Leave "Cached API" checked to search avatars with the worlds you can find them at. Uncheck to browse all avatars.
#### Load an avatar from an avatar ID
Every avatar is identified with a unique ID, which usually starts with avtr_.
Type or paste this into the search bar at the top.

### Web App Limitation
The Unity WebGL web application currently has the following restriction
* Limited shaders. Most shaders will be replaced with the standard shader.
* API requests are sent through a CORS proxy as VRChat's API does not include CORS headers. 
  * More information here:  https://docs.unity3d.com/560/Documentation/Manual/webgl-networking.html
* Loading may take up to 8 seconds or more, depending on your browser and specs. Firefox seems to work the best.
* Some avatars may not work.
* Large files may result in an out of memory error. 

#### To paste an avatar ID into the web app, 
  1. set the avatar querystring to the avatar ID you want to find. After "Avatar=" add the avatar ID. 
      E.g https://whosthatavatar.com/?avatar=avtr_e6b76bcf-ffab-45cc-85e2-c4ac1bd16e0f




## FAQ
### Is this allowed?
VRChat's stance on public use of the VRChat API, per Tupper on the VRChat discord: 
> *Regarding reverse engineering our API - Our stance here is don't be malicious.  This is unsupported and it might break.*

Along those lines, at preasent, only public avatars can be viewed. 

### I dont want my avatar viewable
* Add an empty child gameobject to your avatar with the name: `WTA_IGNORE`
Instead of being viewed your avatar will give an error.

### How does the search work?
Searching uses a cached DB of scanned worlds and allows:
* Searching by Name, Author, or Description.
* Faster search results
* Finding avatar locations
* Eases the load on VRChat's API

* Only public avatars that can be found in a public world are added to the cache. At preasent there are about +8000 indexed avatars.
* It's a serverless stack built on AWS to scan and track public worlds and avatars.
* Updated worlds are scanned periodically for avatars and added to the cache.
