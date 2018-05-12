using System;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Collections.Generic;
using System.IO;


#if (UNITY_WSA_8_1 ||  UNITY_WP_8_1 || UNITY_WINRT_8_1) && !UNITY_EDITOR
 using File = UnityEngine.Windows.File;
 #else
 using File = System.IO.File;
 #endif
 
#if NETFX_CORE
	#if UNITY_WSA_10_0
		using System.IO.IsolatedStorage;
		using static System.IO.Directory;
		using static System.IO.File;
		using static System.IO.FileStream;
	#endif
#endif


public class lzma {
#if !UNITY_WEBPLAYER  || UNITY_EDITOR
	//if you want to be able to call the functions: get7zinfo, get7zSize, decode2Buffer from a thread set this string before to the Application.persistentDataPath !
	public static string persitentDataPath="";

	#if !(UNITY_WSA || UNITY_WP_8_1) || UNITY_EDITOR

    internal static int[] props = new int [7];
    internal static bool defaultsSet = false;

    //0 = level, /* 0 <= level <= 9, default = 5 */
	//1 = dictSize, /* use (1 << N) or (3 << N). 4 KB < dictSize <= 128 MB */
	//2 = lc, /* 0 <= lc <= 8, default = 3  */
	//3 = lp, /* 0 <= lp <= 4, default = 0  */
	//4 = pb, /* 0 <= pb <= 4, default = 2  */
	//5 = fb,  /* 5 <= fb <= 273, default = 32 */
	//6 = numThreads /* 1 or 2, default = 2 */

	//A function that sets the compression properties for the lzma compressor. Will affect the lzma alone file and the lzma buffer compression.
	//A simple usage of this function is to call it only with the 1st parameter that sets the compression level: setProps(9);
	//
	//Multithread safe advice: call this function before starting any thread operations !!!
    public static void setProps(int level = 5, int dictSize = 16777216, int lc = 3, int lp = 0, int pb = 2, int fb = 32, int numThreads = 2) {
        defaultsSet = true;
        props[0] = level;
        props[1] = dictSize;
        props[2] = lc;
        props[3] = lp;
        props[4] = pb;
        props[5] = fb;
        props[6] = numThreads;
    }
	#endif


#if (UNITY_IOS || UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
	#if (UNITY_IOS || UNITY_IPHONE) && !UNITY_WEBGL
		[DllImport("__Internal")]
		public static extern int lsetPermissions(string filePath, string _user, string _group, string _other);
		[DllImport("__Internal")]
		private static extern int decompress7zip(string filePath, string exctractionPath, bool fullPaths,  string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("__Internal")]
		private static extern int decompress7zip2(string filePath, string exctractionPath, bool fullPaths, string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("__Internal")]
		private static extern int _getSize(string filePath, string tempPath, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("__Internal")]
		internal static extern int lzmaUtil(bool encode, string inPath, string outPath, IntPtr Props);
		[DllImport("__Internal")]
		internal static extern int decode2Buf(string filePath, string entry,  IntPtr buffer, IntPtr FileBuffer, int FileBufferLength);
	#endif
	#if (UNITY_IOS || UNITY_IPHONE || UNITY_WEBGL)
		[DllImport("__Internal")]
		internal static extern void _releaseBuffer(IntPtr buffer);	
		[DllImport("__Internal")]
		internal static extern IntPtr Lzma_Compress( IntPtr buffer, int bufferLength, bool makeHeader, ref int v, IntPtr Props);
		[DllImport("__Internal")]
		internal static extern int Lzma_Uncompress( IntPtr buffer, int bufferLength, int uncompressedSize,  IntPtr outbuffer,bool useHeader);
	#endif
#endif

#if UNITY_5_4_OR_NEWER
	#if (UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_WEBGL) && !UNITY_EDITOR || UNITY_EDITOR_LINUX
		private const string libname = "lzma";
	#elif UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
		private const string libname = "liblzma";
	#endif
#else
	#if (UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_WEBGL) && !UNITY_EDITOR 
		private const string libname = "lzma";
	#endif
	#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
		private const string libname = "liblzma";
	#endif
#endif

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX
	#if (!UNITY_WEBGL || UNITY_EDITOR)
		#if (UNITY_STANDALONE_OSX  || UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX)&& !UNITY_EDITOR_WIN
			//set permissions of a file in user, group, other. Each string should contain any or all chars of "rwx".
			//returns 0 on success
			[DllImport(libname, EntryPoint = "lsetPermissions")]
			internal static extern int lsetPermissions(string filePath, string _user, string _group, string _other);
		#endif
        [DllImport(libname, EntryPoint = "decompress7zip")]
		internal static extern int decompress7zip(string filePath, string exctractionPath, bool fullPaths,  string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport(libname, EntryPoint = "decompress7zip2")]
		internal static extern int decompress7zip2(string filePath, string exctractionPath, bool fullPaths, string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport(libname, EntryPoint = "_getSize")]
		internal static extern int _getSize(string filePath, string tempPath, IntPtr FileBuffer, int FileBufferLength);
		[DllImport(libname, EntryPoint = "lzmaUtil")]
		internal static extern int lzmaUtil(bool encode, string inPath, string outPath, IntPtr Props);
		[DllImport(libname, EntryPoint = "decode2Buf")]
		internal static extern int decode2Buf(string filePath, string entry,  IntPtr buffer, IntPtr FileBuffer, int FileBufferLength);
	#endif
		[DllImport(libname, EntryPoint = "_releaseBuffer")]
		internal static extern void _releaseBuffer(IntPtr buffer);
		[DllImport(libname, EntryPoint = "Lzma_Compress")]
		internal static extern IntPtr Lzma_Compress( IntPtr buffer, int bufferLength, bool makeHeader, ref int v, IntPtr Props);
		[DllImport(libname, EntryPoint = "Lzma_Uncompress")]
		internal static extern int Lzma_Uncompress( IntPtr buffer, int bufferLength, int uncompressedSize, IntPtr outbuffer,bool useHeader);
#endif

#if (UNITY_WP_8_1 || UNITY_WSA) && !UNITY_EDITOR
        #if UNITY_WSA_10_0
            [DllImport("liblzma", EntryPoint = "decompress7zip", CallingConvention = CallingConvention.Cdecl)]
			internal static extern int decompress7zip(string filePath, string exctractionPath, bool fullPaths,  string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
        #endif
		[DllImport("liblzma", EntryPoint = "decompress7zip2", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int decompress7zip2(string filePath, string exctractionPath, bool fullPaths, string entry, IntPtr progress, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("liblzma", EntryPoint = "_getSize", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int _getSize(string filePath, string tempPath, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("liblzma", EntryPoint = "decode2Buf", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int decode2Buf(string filePath, string entry,  IntPtr buffer, IntPtr FileBuffer, int FileBufferLength);
		[DllImport("liblzma", EntryPoint = "Lzma_Uncompress", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int Lzma_Uncompress( IntPtr buffer, int bufferLength, int uncompressedSize, IntPtr outbuffer,bool useHeader);
#endif

#if !UNITY_WEBGL || UNITY_EDITOR
	// set permissions of a file in user, group, other.
	// Each string should contain any or all chars of "rwx".
	// returns 0 on success
	public static int setFilePermissions(string filePath, string _user, string _group, string _other){
		#if (UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_ANDROID || UNITY_EDITOR_OSX || UNITY_EDITOR_LINUX || UNITY_IOS || UNITY_IPHONE) && !UNITY_EDITOR_WIN
			return lsetPermissions(filePath, _user, _group, _other);
		#else
			return -1;
		#endif
	}

    // An integer variable to store the total number of files in a 7z archive, excluding the folders.
    public static int trueTotalFiles = 0;

    //ERROR CODES:
    //  1 : OK
    //	2 : Could not find requested file in archive
    // -1 : Could not open input(7z) file
    // -2 : Decoder doesn't support this archive
    // -3 : Can not allocate memory
    // -4 : CRC error of 7z file
    // -5 : Unknown error
    // -6 : Can not open output file (usually when the path to write to, is invalid)
    // -7 : Can not write output file
    // -8 : Can not close output file

    //The most common use of this library is to download a 7z file in your Application.persistentDataPath directory
    //and decompress it in a folder that you want.

    //int lz=lzma.doDecompress7zip(Application.persistentDataPath+"/myCompresedFile.7z",Application.persistentDataPath+"/myUncompressedFiles/");

    //WSA8.1 does not support large files.

	//filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
	//exctractionPath	: the path in where you want your files to be extracted
    //progress          : a single item integer array to get the progress of the extracted files (use this function when calling from a separate thread, otherwise call the 2nd implementation)
    //                  : (for ios this integer is not properly updated. So we use the lzma.getProgressCount() function to get the progress. See example.)
	//largeFiles		: set this to true if you are extracting files larger then 30-40 Mb. It is slower though but prevents crashing your app when extracting large files!
	//fullPaths			: set this to true if you want to keep the folder structure of the 7z file.
	//entry				: set the name of a single file file you want to extract from your archive. If the file resides in a folder, the full path should be added.
	//					   (for example  game/meshes/small/table.mesh )
	//FileBuffer		: A buffer that holds a 7zip file. When assigned the function will decompress from this buffer and will ignore the filePath. (Linux, iOS, Android, MacOSX)
    //use this function from a separate thread to get the progress  of the extracted files in the referenced 'progress' integer.
    //
	public static int doDecompress7zip(string filePath, string exctractionPath,  int [] progress, bool largeFiles=false, bool fullPaths=false, string entry=null, byte[] FileBuffer=null){

		if (@exctractionPath.Substring(@exctractionPath.Length - 1, 1) != "/") { @exctractionPath += "/"; }

		int res = 0;
		GCHandle ibuf = GCHandle.Alloc(progress, GCHandleType.Pinned);

		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_EDITOR_WIN

		if(FileBuffer != null) {
			GCHandle fbuf = GCHandle.Alloc(FileBuffer, GCHandleType.Pinned);
			
				
			if (largeFiles){
				res = decompress7zip(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , fbuf.AddrOfPinnedObject(), FileBuffer.Length);
			}else{
				res =  decompress7zip2(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , fbuf.AddrOfPinnedObject(), FileBuffer.Length);
			}
				fbuf.Free(); ibuf.Free(); return res;
		} else {
			if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject() , IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}
		}
		
		#endif
		
 		#if (!UNITY_WSA && !UNITY_EDITOR_OSX && !UNITY_STANDALONE_LINUX && !UNITY_ANDROID && !UNITY_IOS)  || UNITY_EDITOR_WIN || UNITY_WSA_10_0
            if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}
		#endif

		#if (UNITY_WSA_8_1 ||  UNITY_WP_8_1 || UNITY_WINRT_8_1) && !UNITY_EDITOR
			res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
			ibuf.Free(); return res;
		#endif
    }

    //same as above only the progress integer is a local variable.
    //use this when you don't want to get the progress of the extracted files and when not calling the function from a separate thread.
    public static int doDecompress7zip(string filePath, string exctractionPath,  bool largeFiles = false, bool fullPaths = false, string entry = null, byte[] FileBuffer=null)
    {
        //make a check if the last '/' exists at the end of the exctractionPath and add it if it is missing
        if (@exctractionPath.Substring(@exctractionPath.Length - 1, 1) != "/") { @exctractionPath += "/"; }

        int[] progress = new int[1];
		GCHandle ibuf = GCHandle.Alloc(progress, GCHandleType.Pinned);
		int res = 0;
		
		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_EDITOR_WIN
		if(FileBuffer != null) {
			GCHandle fbuf = GCHandle.Alloc(FileBuffer, GCHandleType.Pinned);
			
			
			if (largeFiles){
				res = decompress7zip(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), fbuf.AddrOfPinnedObject(), FileBuffer.Length);
			}else{
				res = decompress7zip2(null, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), fbuf.AddrOfPinnedObject(), FileBuffer.Length);
			}
				fbuf.Free(); ibuf.Free(); return res;
		} else {
			if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
			}
		}
		#endif
		
		#if (!UNITY_WSA && !UNITY_EDITOR_OSX && !UNITY_STANDALONE_LINUX  && !UNITY_ANDROID && !UNITY_IOS) || UNITY_EDITOR_WIN || UNITY_WSA_10_0
			if (largeFiles){
				res = decompress7zip(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
            }
            else{
				res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
				ibuf.Free(); return res;
            }
		#endif

		#if (UNITY_WSA_8_1 ||  UNITY_WP_8_1 || UNITY_WINRT_8_1) && !UNITY_EDITOR
			res = decompress7zip2(@filePath, @exctractionPath, fullPaths, entry, ibuf.AddrOfPinnedObject(), IntPtr.Zero, 0);
			ibuf.Free(); return res;
		#endif
    }


	#if !(UNITY_WSA || UNITY_WP_8_1) || UNITY_EDITOR
		//ERROR CODES (for both encode/decode LzmaUtil functions):
		//   1 : OK
		// -10 : Can not read input file
		// -11 : Can not write output file
		// -12 : Can not allocate memory
		// -13 : Data error

		//This function encodes a single archive in lzma alone format.
		//inPath	: the file to be encoded. (use full path + file name)
		//outPath	: the .lzma file that will be produced. (use full path + file name)
		//
		//You can set the compression properties by calling the setProps function before.
		//setProps(9) for example will set compression evel to highest level.
		public static int LzmaUtilEncode(string inPath, string outPath){
			if (!defaultsSet) setProps();
			GCHandle prps = GCHandle.Alloc(props, GCHandleType.Pinned);
			int res = lzmaUtil(true, @inPath, @outPath, prps.AddrOfPinnedObject());
			prps.Free();
			return res;
		}


		//This function decodes a single archive in lzma alone format.
		//inPath	: the .lzma file that will be decoded. (use full path + file name)
		//outPath	: the decoded file. (use full path + file name)
		public static int LzmaUtilDecode(string inPath, string outPath){
			return lzmaUtil(false, @inPath, @outPath, IntPtr.Zero);
		}

	#endif

	//Lists get filled with filenames (including path if the file is in a folder) and uncompressed file sizes
	public static List <string> ninfo = new List<string>();//filenames
	public static List <long> sinfo = new List<long>();//file sizes

    //this function fills the ArrayLists with the filenames and file sizes that are in the 7zip file
    //returns			: the total size in bytes of the files in the 7z archive 
    //
    //filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
    //tempPath			: (optional) a temp path that will be used to write the files info (otherwise the path of the 7z archive will be used)
    //					: this is useful when your 7z archive resides in a read only location.
    //					: the tempPath should be in this form: 'dir/dir/myTempLog' with no slash in the end. The last name will be used as the log's filename.
	//FileBuffer		: A buffer that holds a 7zip file. When assigned the function will read from this buffer and will ignore the filePath. (Linux, iOS, Android, MacOSX)
    //
    //trueTotalFiles is an integer variable to store the total number of files in a 7z archive, excluding the folders.
    public static long get7zInfo(string filePath, string tempPath = null, byte[] FileBuffer = null){

        ninfo.Clear(); sinfo.Clear();
        trueTotalFiles = 0;
        int res = -1;
        string logPath = "";

		#if !NETFX_CORE
			if (@tempPath == null) {
				if(persitentDataPath.Length>0) logPath = @persitentDataPath + "/sevenZip.log"; else  logPath = @Application.persistentDataPath + "/sevenZip.log"; 
			 }else { logPath = @tempPath; }
		#endif
		//for WSA, logPath should always be: Application.persistentDataPath + "/sevenZip.log"; 
		#if NETFX_CORE
			#if UNITY_WSA_10_0
				if(persitentDataPath.Length>0) logPath = @persitentDataPath + "/sevenZip.log"; else  logPath = @Application.persistentDataPath + "/sevenZip.log";
			#endif
			#if UNITY_WSA_8_1 ||  UNITY_WP_8_1 || UNITY_WINRT_8_1
				if(persitentDataPath.Length>0) logPath = @persitentDataPath + "/sevenZip.log"; else  logPath = @UnityEngine.Windows.Directory.localFolder + "/sevenZip.log";
			#endif
		#endif

		if (File.Exists(logPath + ".txt")) File.Delete(logPath + ".txt");

		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_EDITOR_WIN
          if(FileBuffer != null) {
                GCHandle fbuf = GCHandle.Alloc(FileBuffer, GCHandleType.Pinned);
                res = _getSize(null, logPath,  fbuf.AddrOfPinnedObject(), FileBuffer.Length);
                fbuf.Free();
            }else {
                res = _getSize(@filePath, logPath,  IntPtr.Zero, 0);
            }
        #else
            res = _getSize(@filePath, logPath,  IntPtr.Zero, 0);     
        #endif   

        if (res == -1) { /*Debug.Log("Input file not found.");*/ return -1; }

		if (!File.Exists(logPath + ".txt")) {/* Debug.Log("Info file not found.");*/ return -3; }

		#if !NETFX_CORE
			StreamReader r = new StreamReader(logPath + ".txt");
		#endif
		#if NETFX_CORE
			#if UNITY_WSA_10_0
			IsolatedStorageFile ipath = IsolatedStorageFile.GetUserStoreForApplication();
			StreamReader r = new StreamReader(new IsolatedStorageFileStream("sevenZip.log.txt", FileMode.Open, ipath));
			#endif
			#if UNITY_WSA_8_1 ||  UNITY_WP_8_1 || UNITY_WINRT_8_1
			var data = UnityEngine.Windows.File.ReadAllBytes(logPath + ".txt");
			string ss = System.Text.Encoding.UTF8.GetString(data,0,data.Length);
			StringReader r = new StringReader(ss);
			#endif
		#endif

        string line;
        string[] rtt;
        long t = 0, sum = 0;
		
		while ((line = r.ReadLine()) != null)
		{
			rtt = line.Split('|');
			ninfo.Add(rtt[0]);
			long.TryParse(rtt[1], out t);
			sum += t;
			sinfo.Add(t);
			if (t > 0) trueTotalFiles++;
		}

		#if !NETFX_CORE
			r.Close();
		#endif
		r.Dispose();
		File.Delete(logPath + ".txt");

        return sum;
    }
	
	
	//this function returns the uncompressed file size of a given file in the 7z archive if specified,
	//otherwise it will return the total uncompressed size of all the files in the archive.
	//
	//If you don't fill the filePath parameter it will assume that the get7zInfo function has already been called.
	//
	//
	//filePath			: the full path to the archive, including the archives name. (/myPath/myArchive.7z)
	// 					: if you call the function with filePath as null, it will try to find file sizes from the last call.
	//fileName 			: the file name we want to get the file size (if it resides in a folder add the folder path also)
	//tempPath			: (optional) a temp path that will be used to write the files info (otherwise the path of the 7z archive will be used)
	//					: this is useful when your 7z archive resides in a read only location.
    //					: the tempPath should be in this form: 'dir/dir/myTempLog' with no slash in the end. The last name will be used as the log's filename.
	//FileBuffer		: A buffer that holds a 7zip file. When assigned the function will read from this buffer and will ignore the filePath. (Linux, iOS, Android, MacOSX)
	public static long get7zSize( string filePath=null, string fileName=null, string tempPath=null, byte[] FileBuffer=null){
		
		if(filePath!=null){
			if(get7zInfo(@filePath, @tempPath, FileBuffer) < 0){ return -1;}
		}

		if(ninfo == null){
			if(ninfo.Count==0) { return -1; }
		}

		long sum=0;

		if(fileName!=null){
			for(int i=0; i<ninfo.Count; i++){
				if(ninfo[i].ToString() == fileName){
					return (long)sinfo[i];
				}
			}
		}else{
			for(int i=0; i<ninfo.Count; i++){
				sum += (long)sinfo[i];
			}
			return sum;
		}
		return -1;//nothing was found
	}



	//A function to decode a specific archive in a 7z archive to a byte buffer
	//
	//filePath		: the full path to the 7z archive 
	//entry			: the file name to decode to a buffer. If the file resides in a folder, the full path should be used.
	//tempPath		: (optional) a temp path that will be used to write the files info (otherwise the path of the 7z archive will be used)
	//				: this is useful when your 7z archive resides in a read only location.
    //				: the tempPath should be in this form: 'dir/dir/myTempLog' with no slash in the end. The last name will be used as the log's filename.
	//FileBuffer	: A buffer that holds a 7zip file. When assigned the function will read from this buffer and will ignore the filePath. (Linux, iOS, Android, MacOSX)
	public static byte[] decode2Buffer(  string filePath, string entry, string tempPath=null, byte[] FileBuffer=null){
		
		int bufs = (int)get7zSize( @filePath, entry, @tempPath,FileBuffer );
        if (bufs <= 0) return null;//entry error or it does not exist
        byte[] nb = new byte[bufs];
        int res = 0;

        GCHandle dec2buf = GCHandle.Alloc(nb, GCHandleType.Pinned);

		#if (UNITY_IPHONE || UNITY_IOS || UNITY_STANDALONE_OSX || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_EDITOR) && !UNITY_EDITOR_WIN
          if(FileBuffer != null) {
                GCHandle fbuf = GCHandle.Alloc(FileBuffer, GCHandleType.Pinned);
                res = decode2Buf(null, entry, dec2buf.AddrOfPinnedObject(), fbuf.AddrOfPinnedObject(), FileBuffer.Length);
                fbuf.Free();
            }else {
                res = decode2Buf(@filePath, entry, dec2buf.AddrOfPinnedObject(), IntPtr.Zero, 0);
            }
        #else
            res = decode2Buf(@filePath, entry, dec2buf.AddrOfPinnedObject(), IntPtr.Zero, 0);    
        #endif

        dec2buf.Free();
		if(res==1){ return nb;}
		else {nb=null; return null; }

    }
#endif

#if !(UNITY_WSA || UNITY_WP_8_1) || UNITY_EDITOR

    //This function encodes inBuffer to lzma alone format into the outBuffer provided.
    //The buffer can be saved also into a file and can be opened by applications that opens the lzma alone format.
    //This buffer can be uncompressed by the decompressBuffer function.
    //Returns true if success
    //if makeHeader==false then the lzma 13 bytes header will not be added to the buffer.
	//
	//You can set the compression properties by calling the setProps function before.
	//setProps(9) for example will set compression level to the highest level.
	//
    public static  bool compressBuffer(byte[] inBuffer, ref byte[] outBuffer, bool makeHeader=true){

        if (!defaultsSet) setProps();
        GCHandle prps = GCHandle.Alloc(props, GCHandleType.Pinned);

		GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
		IntPtr ptr;
        
        int res = 0;

		ptr = Lzma_Compress(cbuf.AddrOfPinnedObject(), inBuffer.Length, makeHeader, ref res, prps.AddrOfPinnedObject());

		cbuf.Free(); prps.Free();

		if(res==0 || ptr==IntPtr.Zero){_releaseBuffer(ptr); return false;}

		Array.Resize(ref outBuffer,res);
		Marshal.Copy(ptr, outBuffer, 0, res);

		_releaseBuffer(ptr);

		return true;
	}


    //same as the above function, only it compresses a part of the input buffer.
	//
	//inBufferPartialLength: the size of the input buffer that should be compressed
	//inBufferPartialIndex:  the offset of the input buffer from where the compression will start
	//
	public static bool compressBufferPartial(byte[] inBuffer, int inBufferPartialIndex, int inBufferPartialLength, ref byte[] outBuffer, bool makeHeader = true)
    {
		if(inBufferPartialIndex + inBufferPartialLength > inBuffer.Length) return false;

        if (!defaultsSet) setProps();
        GCHandle prps = GCHandle.Alloc(props, GCHandleType.Pinned);
        GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);

        IntPtr ptr;
        IntPtr ptrPartial;

        int res = 0;

        ptrPartial = new IntPtr(cbuf.AddrOfPinnedObject().ToInt64() + inBufferPartialIndex);

        ptr = Lzma_Compress(ptrPartial, inBufferPartialLength, makeHeader, ref res, prps.AddrOfPinnedObject());

        cbuf.Free();

        if (res == 0 || ptr == IntPtr.Zero) { _releaseBuffer(ptr); return false; }

        Array.Resize(ref outBuffer, res);
        Marshal.Copy(ptr, outBuffer, 0, res);
		 
        _releaseBuffer(ptr);

        return true;
    }


	//same as compressBufferPartial, only this function will compress the data into a fixed size buffer
	//the compressed size is returned so you can manipulate it at will.
	public static int compressBufferPartialFixed(byte[] inBuffer, int inBufferPartialIndex, int inBufferPartialLength, ref byte[] outBuffer, bool safe = true,  bool makeHeader = true)
    {
		if(inBufferPartialIndex + inBufferPartialLength > inBuffer.Length) return 0;

        if (!defaultsSet) setProps();
        GCHandle prps = GCHandle.Alloc(props, GCHandleType.Pinned);
        GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);

        IntPtr ptr;
        IntPtr ptrPartial;

        int res = 0;

        ptrPartial = new IntPtr(cbuf.AddrOfPinnedObject().ToInt64() + inBufferPartialIndex);

        ptr = Lzma_Compress(ptrPartial, inBufferPartialLength, makeHeader, ref res, prps.AddrOfPinnedObject());

        cbuf.Free();

        if (res == 0 || ptr == IntPtr.Zero) { _releaseBuffer(ptr); return 0; }

		//if the compressed buffer is larger then the fixed size buffer we use:
		//1. then write only the data that fit in it.
		//2. or we return 0. 
		//It depends on if we set the safe flag to true or not.
		if(res>outBuffer.Length) {
			if(safe){ _releaseBuffer(ptr); return 0; } else {  res = outBuffer.Length; }
		}

        Marshal.Copy(ptr, outBuffer, 0, res);
		 
        _releaseBuffer(ptr);

        return res;
    }


	//same as the compressBuffer function, only this function will put the result in a fixed size buffer to avoid memory allocations.
	//the compressed size is returned so you can manipulate it at will.
	public static int compressBufferFixed(byte[] inBuffer, ref byte[] outBuffer, bool safe = true, bool makeHeader=true){

        if (!defaultsSet) setProps();
        GCHandle prps = GCHandle.Alloc(props, GCHandleType.Pinned);

		GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
		IntPtr ptr;
        
        int res = 0;

		ptr = Lzma_Compress(cbuf.AddrOfPinnedObject(), inBuffer.Length, makeHeader, ref res, prps.AddrOfPinnedObject());

		cbuf.Free(); prps.Free();
		if(res==0 || ptr==IntPtr.Zero){_releaseBuffer(ptr); return 0;}

		//if the compressed buffer is larger then the fixed size buffer we use:
		//1. then write only the data that fit in it.
		//2. or we return 0. 
		//It depends on if we set the safe flag to true or not.
		if(res>outBuffer.Length) {
			if(safe){ _releaseBuffer(ptr); return 0; } else {  res = outBuffer.Length; }
		}

		Marshal.Copy(ptr, outBuffer, 0, res);

		_releaseBuffer(ptr);

		return res;
	}

#endif





	//This function will decompress a compressed asset bundle.
	//It finds the magic number of the lzma format and extracts from there.
	//
	//inBuffer:		the buffer that stores a compressed asset bundle.
	//outBuffer:	a referenced buffer where the asset bundle will be uncompressed.
    //The error codes
    /*
        OK 0
		
        ERROR_DATA 1
        ERROR_MEM 2
        ERROR_UNSUPPORTED 4
        ERROR_PARAM 5
        ERROR_INPUT_EOF 6
        ERROR_OUTPUT_EOF 7
        ERROR_FAIL 11
        ERROR_THREAD 12
    */
	public static  int decompressAssetBundle(byte[] inBuffer,  ref byte[] outbuffer){

		int offset = 0;
		
		for(int i=0; i<inBuffer.Length; i++) {
			if(i>1024) break;
			if(inBuffer[i] == 0x5d) {
				if(inBuffer[i+1] == 0x00) {
					if(inBuffer[i+2] == 0x00) {
						if(inBuffer[i+3] == 0x08) {
							offset = i;  break; 
						}
					}
				}
			}
		}
		
		if(offset==0 || offset>1024) return 4;

		GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
		IntPtr ptrBundle = new IntPtr(cbuf.AddrOfPinnedObject().ToInt64() + offset);
		int uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,offset+5);
		if(uncompressedSize<0) { cbuf.Free(); return 4; }
		Array.Resize(ref outbuffer, uncompressedSize);
		GCHandle obuf = GCHandle.Alloc(outbuffer, GCHandleType.Pinned);
		
		int res = Lzma_Uncompress(ptrBundle, inBuffer.Length-offset, uncompressedSize, obuf.AddrOfPinnedObject(), true);

		cbuf.Free();
		obuf.Free();

		//if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return res; }
	
		return res;		
	}


	/*
	//this will decompress an lzma alone format file.
	public static  int decompressLzmaAlone(string inFile,  string outFile){

		if(File.Exists(inFile)) {
			var inBuffer = File.ReadAllBytes(inFile);

			int offset = 0;
		
			for(int i=0; i<inBuffer.Length; i++) {
				if(i>16) break;
				if(inBuffer[i] == 0x5d) {
					if(inBuffer[i+1] == 0x00) {
						if(inBuffer[i+2] == 0x00) {
							if(inBuffer[i+3] == 0x00) {
								offset = i;  break; 
							}
						}
					}
				}
			}
		
			if(offset>16) {  inBuffer=null; return 4; }

			GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
			IntPtr ptrBundle = new IntPtr(cbuf.AddrOfPinnedObject().ToInt64() + offset);
			int uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,offset+5);
			if(uncompressedSize<0) { cbuf.Free(); return 4; }
			byte[] outBuffer = new byte[uncompressedSize];
			GCHandle obuf = GCHandle.Alloc(outBuffer, GCHandleType.Pinned);
		
			int res = Lzma_Uncompress(ptrBundle, inBuffer.Length-offset, uncompressedSize, obuf.AddrOfPinnedObject(), true);

			cbuf.Free();
			obuf.Free();

			File.WriteAllBytes(outFile, outBuffer);

			Array.Resize(ref outBuffer, 0);
			Array.Resize(ref inBuffer, 0);

			outBuffer = null;
			inBuffer = null;

			GC.Collect();

			return res;	
		} else {
			return -1;
		}	
	}
	*/


    //This function decompresses an lzma compressed byte buffer.
    //If the useHeader flag is false you have to provide the uncompressed size of the buffer via the customLength integer.
    //if res==0 operation was successful
    //The error codes
    /*
        OK 0
		
        ERROR_DATA 1
        ERROR_MEM 2
        ERROR_UNSUPPORTED 4
        ERROR_PARAM 5
        ERROR_INPUT_EOF 6
        ERROR_OUTPUT_EOF 7
        ERROR_FAIL 11
        ERROR_THREAD 12
        */
    public static  int decompressBuffer(byte[] inBuffer,  ref byte[] outbuffer, bool useHeader=true, int customLength=0){
		
		GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
		int uncompressedSize = 0;
		
		//if the lzma header will be used to extract the uncompressed size of the buffer. If the buffer does not have a header 
		//provide the known uncompressed size through the customLength integer.
		if(useHeader) uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,5); else uncompressedSize = customLength;

		Array.Resize(ref outbuffer, uncompressedSize);

		GCHandle obuf = GCHandle.Alloc(outbuffer, GCHandleType.Pinned);
		
		int res = Lzma_Uncompress(cbuf.AddrOfPinnedObject(), inBuffer.Length, uncompressedSize, obuf.AddrOfPinnedObject(), useHeader);

		cbuf.Free();
		obuf.Free();

		//if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return res; }
	
		return res;		
	}

	public static  byte[] decompressBuffer(byte[] inBuffer, bool useHeader=true, int customLength=0){
		
		GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
		int uncompressedSize = 0;
		
		//if the lzma header will be used to extract the uncompressed size of the buffer. If the buffer does not have a header 
		//provide the known uncompressed size through the customLength integer.
		if(useHeader) uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,5); else uncompressedSize = customLength;

		byte[] outbuffer = new byte[uncompressedSize];

		GCHandle obuf = GCHandle.Alloc(outbuffer, GCHandleType.Pinned);
		
		int res = Lzma_Uncompress(cbuf.AddrOfPinnedObject(), inBuffer.Length, uncompressedSize, obuf.AddrOfPinnedObject(), useHeader);

		cbuf.Free();
		obuf.Free();

		if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return null; }
	
		return outbuffer;		
	}


	//same as above function. Only this one outputs to a buffer of fixed which size isn't resized to avoid memory allocations.
	//The fixed buffer should have a size that will be able to hold the incoming decompressed data.
	//returns the uncompressed size.
	public static  int decompressBufferFixed(byte[] inBuffer,  ref byte[] outbuffer, bool safe = true, bool useHeader=true, int customLength=0){

		int uncompressedSize = 0;
		
		//if the lzma header will be used to extract the uncompressed size of the buffer. If the buffer does not have a header 
		//provide the known uncompressed size through the customLength integer.
		if(useHeader) uncompressedSize = (int)BitConverter.ToUInt64(inBuffer,5); else uncompressedSize = customLength;

		//Check if the uncompressed size is bigger then the size of the fixed buffer. Then:
		//1. write only the data that fit in it.
		//2. or return a negative number. 
		//It depends on if we set the safe flag to true or not.
		if(uncompressedSize > outbuffer.Length) {
			if(safe) return -101;  else  uncompressedSize = outbuffer.Length;
		 }

		GCHandle cbuf = GCHandle.Alloc(inBuffer, GCHandleType.Pinned);
		GCHandle obuf = GCHandle.Alloc(outbuffer, GCHandleType.Pinned);
		
		int res = Lzma_Uncompress(cbuf.AddrOfPinnedObject(), inBuffer.Length, uncompressedSize, obuf.AddrOfPinnedObject(), useHeader);

		cbuf.Free();
		obuf.Free();

		if(res!=0){/*Debug.Log("ERROR: "+res.ToString());*/ return -res; }
	
		return uncompressedSize;		
	}
#endif
}

