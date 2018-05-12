using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UtinyRipper.BundleFiles;
using UnityEngine;
using LZ4;

//This is only needed for WebGL. Otherwise just use the assetbundle how you would any other.

//Unity 5.5 and later does not support extracting an assetbundle compressed with LZMA in WebGL: https://blogs.unity3d.com/cn/2016/09/20/understanding-memory-in-unity-webgl/
//What this does is extract it and update the metadata
//it also flags the assetbundle as a WebGL assetbundle, instead of once for windows standalone

namespace VRCAvatarAssetbundleDecompressor
{
    public class Decompressor : MonoBehaviour
    {
        public static byte[] Attempt(byte[] input)
        {
            using (Stream abInputStream = new MemoryStream(input))
            {
                //assetbundles are BigEndien
                using (BinaryReader abReader = new BinaryReader(abInputStream))
                {
                    //strings end with a zero byte
                    string signature = abReader.ReadZeroedString();
                    int generation = abReader.ReadInt32BE();
                    string PlayerVersion = abReader.ReadZeroedString();
                    string engineVersion = abReader.ReadZeroedString();

                    Int64 bundlesize = abReader.ReadInt64BE();
                    long metadataCompressedSizeLocation = abInputStream.Position;
                    Int32 MetadataCompressedSize = abReader.ReadInt32BE();
                    Int32 MetadataDecompressedSize = abReader.ReadInt32BE();

                    var Flags = (BundleFlag)abReader.ReadInt32BE();


                    long dataPosition = abInputStream.Position;
                    dataPosition += MetadataCompressedSize;

                    Int32 blockInfoLength;
                    UInt32 blockDecompressedSize;
                    UInt32 blockCompressedSize;
                    BundleFlag blockFlags;
                    Int32 metaDataCount;
                    Int64 mdoffset;
                    Int64 mdsize;
                    Int32 mdblobindex;
                    string mdname;

                    using (MemoryStream memStream = new MemoryStream(MetadataDecompressedSize))
                    {
                        byte[] compressedBytes = new byte[MetadataCompressedSize];
                        abInputStream.Read(compressedBytes, 0, MetadataCompressedSize);
                        abInputStream.Position -= MetadataCompressedSize;

                        byte[] LZ4decompressBuffer = new byte[MetadataDecompressedSize];
                        Debug.Log("LZ4 decompressing...");
                        LZ4Codec.Decode(compressedBytes, 0, compressedBytes.Length, LZ4decompressBuffer, 0, LZ4decompressBuffer.Length, true); //WebGL compatable LZMA

                        //LZ4decompressBuffer = LZ4.decompressBuffer(compressedBytes, false, MetadataDecompressedSize);
                        /*
                        using (Lz4Stream lzStream = new Lz4Stream(stream, MetadataCompressedSize))
                        {
                            long read = lzStream.Read(memStream, MetadataDecompressedSize);
                        }
                        */

                        byte[] shouldBe = new byte[MetadataDecompressedSize];
                        memStream.Position = 0;
                        memStream.Read(shouldBe, 0, MetadataDecompressedSize);

                        memStream.Position = 0;
                        memStream.Write(LZ4decompressBuffer, 0, MetadataDecompressedSize);


                        memStream.Position = 0;

                        using (BinaryReader metadataReader = new BinaryReader(memStream))
                        {
                            memStream.Position += 0x10;

                            blockInfoLength = metadataReader.ReadInt32BE();//1
                            blockDecompressedSize = metadataReader.ReadUInt32BE();
                            blockCompressedSize = metadataReader.ReadUInt32BE();
                            blockFlags = (BundleFlag)metadataReader.ReadUInt16BE();

                            metaDataCount = metadataReader.ReadInt32BE();//1
                            mdoffset = metadataReader.ReadInt64BE();//0
                            mdsize = metadataReader.ReadInt64BE();
                            mdblobindex = metadataReader.ReadInt32BE();//4
                            mdname = metadataReader.ReadZeroedString();//guid
                        }
                    }

                    MetadataCompressedSize = MetadataDecompressedSize; //update
                    blockFlags &= (BundleFlag)(~BundleCompressType.LZMA); //remove compression flag

                    using (MemoryStream outWriterStream = new MemoryStream())
                    {
                        using (BinaryReader outWriter = new BinaryReader(outWriterStream, new System.Text.UTF8Encoding(false)))
                        {
                            outWriter.WriteZeroedString(signature);

                            outWriter.Write((Int32)generation);
                            outWriter.WriteZeroedString(PlayerVersion);
                            
                            outWriter.WriteZeroedString(engineVersion);
                            var bundleSizePos = outWriterStream.Position;
                            outWriter.Write((long)bundlesize);

                            var metaDataCompressedSizePos = outWriterStream.Position;
                            outWriter.Write((Int32)MetadataDecompressedSize);//placeholder
                            outWriter.Write((Int32)MetadataDecompressedSize);

                            outWriter.Write((Int32)(Flags));


                            long preCompressLocation = 0;
                            using (MemoryStream mdStreamWriter = new MemoryStream())
                            {
                                using (BinaryReader mdwriter = new BinaryReader(mdStreamWriter, new System.Text.UTF8Encoding(false)))
                                {
                                    mdwriter.Write(new byte[16]);//unknown, presumed buffer
                                    mdwriter.Write((Int32)blockInfoLength);
                                    mdwriter.Write(blockDecompressedSize);//would have been compressed size
                                    mdwriter.Write(blockDecompressedSize);
                                    mdwriter.Write((UInt16)blockFlags);
                                    mdwriter.Write((Int32)metaDataCount);//metadata count //should be 1
                                    mdwriter.Write(mdoffset);
                                    mdwriter.Write(mdsize);
                                    mdwriter.Write(mdblobindex);
                                    mdwriter.WriteZeroedString(mdname);

                                    mdStreamWriter.Position = 0;

                                    preCompressLocation = outWriterStream.Position;

                                    byte[] mdUncompressed = new byte[mdStreamWriter.Length];
                                    byte[] LZ4CompressBuffer = new byte[mdUncompressed.Length];
                                    mdStreamWriter.Read(mdUncompressed, 0, mdUncompressed.Length);
                                    Debug.Log("LZ4 recompressing...");

                                    //LZ4CompressBuffer = LZ4.compressBuffer(mdUncompressed, 1, false);
                                    //var outputLength = LZ4Codec.Encode(inputBuffer, 0, inputLength,outputBuffer, 0, maximumLength);
                                    //recompress metadata. Appearently, it cannot be uncompressed.
                                    var outputLength = LZ4Codec.Encode(mdUncompressed, 0, mdUncompressed.Length, LZ4CompressBuffer, 0, mdUncompressed.Length);

                                    Array.Resize<byte>(ref LZ4CompressBuffer, outputLength); //care for heap size

                                    outWriterStream.Write(LZ4CompressBuffer, 0, LZ4CompressBuffer.Length);
                                }
                            }


                            long postCompressLocation = outWriterStream.Position;
                            var compressedSize = postCompressLocation - preCompressLocation;

                            outWriterStream.Position = metaDataCompressedSizePos;
                            outWriter.Write((Int32)compressedSize);//update compressed size
                            outWriterStream.Position = postCompressLocation;

                            long totalSize = 0;

                            abInputStream.Position = dataPosition;
                            var compressedBuffer = new byte[blockCompressedSize];
                            var lzmaProperties = new byte[5]; //probably not needed?
                            abInputStream.Read(lzmaProperties, 0, lzmaProperties.Length);

                            abInputStream.Read(compressedBuffer, 0, (int)blockCompressedSize - lzmaProperties.Length); // 

                            var decompressedBuffer = new byte[blockDecompressedSize];
                            Debug.Log("LZMA decompressing, good luck!");
                            var result = lzma.decompressBufferFixed(compressedBuffer, ref decompressedBuffer, false, false, (int)blockDecompressedSize);


                            //lzma.decompressAssetBundle(compressedBuffer, ref decompressedBuffer);
                            /*
                            using (MemoryStream memStream2 = new MemoryStream())
                            {
                                SevenZipHelper.DecompressLZMAStream(stream, blockCompressedSize, memStream2, blockDecompressedSize);
                                totalSize = memStream2.Length;
                                memStream2.Position = 0;
                                memStream2.CopyTo(memStream);
                            }
                            */

                            Debug.Log("Writing buffer to stream, size: " + blockDecompressedSize.ToString());
                            outWriterStream.Write(decompressedBuffer, 0, (int)blockDecompressedSize);
                            totalSize = outWriterStream.Length; //I dont think this is used? Should this be only the size of the decompressed block?

                            outWriterStream.Position = bundleSizePos;
                            outWriter.Write((long)totalSize);
                            outWriterStream.Position = 0;


                            Debug.Log("Allocating return bytes, size: " + totalSize.ToString());
                            byte[] bytes = new byte[totalSize];
                            Debug.Log("Reading stream to bytes...");
                            outWriterStream.Read(bytes, 0, (int)totalSize);
                            //bytes[138] = (byte)20; // I had hoped it was that easy


                            //WebGL can *probably* parse assetbundles made for standalone windows, so we'll just say that it is
                            //Appearently this doesnt work in later versions of unity. Interesting.
                            Debug.Log("Finding platform id...");
                            byte[] toFind = new byte[] { 0x35, 0x2E, 0x36, 0x2E, 0x33, 0x70, 0x31, 0x00 };
                            bool found = false;
                            int index = (int)postCompressLocation;
                            while (!found)
                            {
                                byte[] buffer = new byte[toFind.Length];
                                Array.Copy(bytes, index, buffer, 0, buffer.Length);
                                index++;
                                bool match = true;
                                for (int i = 0; i < buffer.Length; i++)
                                {
                                    //Look for: 5.*.*** 
                                    //consider using regex

                                    if (i == 0 || i == 1 || i == 3)
                                    {
                                        if (buffer[i] != toFind[i])
                                        {
                                            break;
                                        }
                                    }   
                                    
                                    if (i == buffer.Length - 1)
                                        found = true;
                                }
                                if (index > 200)
                                {
                                    Debug.Log("Couldent find platform id location."); //avatar will not load, and will probably crash the webgl player
                                    return null;
                                }
                            }
                            bytes[index + toFind.Length - 1] = (byte)20; // 20 is for WebGL compiled assetbundles.

                            Debug.Log("Returning bytes");
                            return bytes;
                        }
                    }
                }
            }

            return null;
        }
        
    }



    public static class BEHelpers
    {
        // Note this MODIFIES THE GIVEN ARRAY then returns a reference to the modified array.
        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        public static string ReadZeroedString(this BinaryReader binRdr)
        {
            byte[] stringByteBuffer = new byte[2000]; //keep memeory as low as possible in webgl
            int i;

            for (i = 0; i < stringByteBuffer.Length; i++)
            {
                byte character = binRdr.ReadByte();
                if (character == 0)
                {
                    break;
                }
                stringByteBuffer[i] = character;

                var tempt = Encoding.UTF8.GetString(stringByteBuffer, 0, i);
            }
            if (i == stringByteBuffer.Length)
            {
                throw new Exception("Can't find end of string");
            }
            return Encoding.UTF8.GetString(stringByteBuffer, 0, i);
        }
        
        public static void WriteZeroedString(this BinaryReader binRdr, string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
            binRdr.BaseStream.WriteByte(0);
        }

        public static UInt16 ReadUInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(UInt16)).Reverse(), 0);
        }

        public static Int16 ReadInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt16(binRdr.ReadBytesRequired(sizeof(Int16)).Reverse(), 0);
        }

        public static UInt32 ReadUInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt32(binRdr.ReadBytesRequired(sizeof(UInt32)).Reverse(), 0);
        }

        public static Int32 ReadInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt32(binRdr.ReadBytesRequired(sizeof(Int32)).Reverse(), 0);
        }

        public static UInt64 ReadUInt64BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt64(binRdr.ReadBytesRequired(sizeof(UInt64)).Reverse(), 0);
        }

        public static Int64 ReadInt64BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt64(binRdr.ReadBytesRequired(sizeof(Int64)).Reverse(), 0);
        }

        public static void Write(this BinaryReader binRdr, UInt16 value)
        {
            var bytes = BitConverter.GetBytes(value).Reverse();
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this BinaryReader binRdr, Int16 value)
        {
            var bytes = BitConverter.GetBytes(value).Reverse();
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this BinaryReader binRdr, UInt32 value)
        {
            var bytes = BitConverter.GetBytes(value).Reverse();
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this BinaryReader binRdr, Int32 value)
        {
            var bytes = BitConverter.GetBytes(value).Reverse();
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this BinaryReader binRdr, UInt64 value)
        {
            var bytes = BitConverter.GetBytes(value).Reverse();
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this BinaryReader binRdr, Int64 value)
        {
            var bytes = BitConverter.GetBytes(value).Reverse();
            binRdr.BaseStream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this BinaryReader binRdr, byte[] value)
        {
            binRdr.BaseStream.Write(value, 0, value.Length);
        }

        public static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
        {
            var result = binRdr.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }
    }

}
