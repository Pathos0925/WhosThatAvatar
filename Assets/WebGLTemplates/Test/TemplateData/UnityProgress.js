var _progress = 0.0;
var _message = "Loading...";
  
function UnityProgress(gameInstance, progress) {
  if (!gameInstance.Module)
    return;
  _progres = progress;
  
  var length = 200 * Math.min(progress, 1);
    bar = document.getElementById("progressBar")
    bar.style.width = length + "px";
    document.getElementById("loadingInfo").innerHTML = _message;
  
  if (progress == 1)
  {
	  this.SetMessage("Preparing...");
      document.getElementById("bgBar").style.display = "none";
      document.getElementById("progressBar").style.display = "none";
  }
    //gameInstance.logo.style.display = gameInstance.progress.style.display = "none";

this.SetMessage = function (message) 
  { 
	  console.log("message: " + message);
		_message = message; 
		document.getElementById("loadingInfo").innerHTML = _message;
  }  
  this.Clear = function() 
  {
	  
	  console.log("Clear: ");
    document.getElementById("loadingBox").style.display = "none";
	document.getElementById("loadingInfo").innerHTML = "";
  }
}