using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Uploader
{
	public static GameObject uploadManager;

	public static Amazon.S3.Model.PostObjectRequest UploadFile(string fileName, string s3FolderName = "other", Action<string> onSuccess = null)
	{
		if(uploadManager == null) 
		{
			uploadManager = new GameObject();
			uploadManager.name = "UploadManager";
		}
		
		S3Manager s3Manager = uploadManager.GetOrAddComponent<S3Manager>();
		return s3Manager.PostObject(fileName, s3FolderName, onSuccess);
    }

}

