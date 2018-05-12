var ImportExportPlugin = {
    ShowOverlay: function(stringPointer)
    {
 
      if (!document.getElementById('inputOverlay')) {
        var myoverlay = document.createElement('div');
        myoverlay.setAttribute('id', 'inputOverlay');
        document.body.appendChild(myoverlay);          
        document.getElementById('inputOverlay').innerHTML = "<p>Avatar Id: <input type='text' name='AvatarId' size='45' value='avtr_63340186-53cb-46e3-9a60-419da084c794' id='avatarIdInput' /> <button onclick='SendMessage(\&quot;GameManager\&quot;,\&quot;WebpageUtilities\&quot;,document.getElementById(\&quot;avatarIdInput\&quot;).value)'>Get</button></p>";
      }
     
      var exported = Pointer_stringify(stringPointer);
      document.getElementById('avatarIdInput').value = exported;
      //document.getElementById('inputOverlay').setAttribute('style','display:block;');
      //document.getElementById('inputOverlay').focus();
    },
    HideOverlay: function()
    {
        document.getElementById('inputOverlay').setAttribute('style','display:none;');
    }
};
mergeInto(LibraryManager.library, ImportExportPlugin);