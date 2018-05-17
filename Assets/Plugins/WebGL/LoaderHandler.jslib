var LoaderHandler = {
    FinishLoading: function()
	{
		try
		{
			document.getElementById("loadingInfo").innerHTML = "";
			document.getElementById("loadingBox").style.display = "none";
		}
		catch(e)
		{
			return;
		}
        
    }
};
mergeInto(LibraryManager.library, LoaderHandler);