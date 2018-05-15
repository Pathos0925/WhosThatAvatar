var QueryHandler = {
    GetParam: function()
	{
		try
		{
			var level = "";
			var queryString = window.location.search.substring(1);
			var params = queryString.split("&");

			for (var i=0; i<params.length; i++) {
				var param = params[i].split("=");
				if(param[0] == "avatar"){ level = param[1]; }
			}

			var buffer = _malloc(lengthBytesUTF8(level) + 1);
			stringToUTF8(level, buffer, lengthBytesUTF8(level) + 1);
			return buffer;
		}
		catch(e)
		{
			return;
		}
        
    },
    SetParam: function(param){
        //window.location.search = "?avatar=" + Pointer_stringify(param) // this forces the browser to navigate, and reloads the game
		history.pushState({}, "Avatar", "?avatar=" + Pointer_stringify(param));
    },
	SetParamReplace: function(param){
        
		window.history.replaceState({}, "Avatar", "?avatar=" + Pointer_stringify(param));
    }
};
mergeInto(LibraryManager.library, QueryHandler);