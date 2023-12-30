﻿using DynamicData;
using DynamicData.Binding;
using Microsoft.WindowsAPICodePack.Dialogs;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Wabbajack.Common;
using Wabbajack.Hashing.xxHash64;
using Wabbajack.Extensions;
using Wabbajack.Models;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;
using System.Drawing;
using Catel.IO;
using System.Drawing.Imaging;
using System.Linq;
using System.Data;

namespace Wabbajack
{
    public static class UIUtils
    {
        public static BitmapImage BitmapImageFromResource(string name) => BitmapImageFromStream(System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Wabbajack;component/" + name)).Stream);

        public static BitmapImage BitmapImageFromStream(Stream stream)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.StreamSource = stream;
            img.EndInit();
            img.Freeze();
            return img;
        }

        public static BitmapImage BitmapImageFromWebp(byte[] bytes, bool getThumbnail = false)
        {
            using(WebP webp = new())
            {
                Bitmap bitmap;
                if (getThumbnail)
                    bitmap = webp.GetThumbnailFast(bytes, 640, 360);
                else
                    bitmap = webp.Decode(bytes);

                using(var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    ms.Position = 0;

                    var img = new BitmapImage();
                    img.BeginInit();
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.StreamSource = ms;
                    img.EndInit();
                    img.Freeze();
                    return img;
                }
            }
        }

        public static bool TryGetBitmapImageFromFile(AbsolutePath path, out BitmapImage bitmapImage)
        {
            try
            {
                if (!path.FileExists())
                {
                    bitmapImage = default;
                    return false;
                }
                bitmapImage = new BitmapImage(new Uri(path.ToString(), UriKind.RelativeOrAbsolute));
                return true;
            }
            catch (Exception)
            {
                bitmapImage = default;
                return false;
            }
        }
        
        
        public static void OpenWebsite(Uri url)
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c start {url}")
            {
                CreateNoWindow = true,
            });
        }
        
        public static void OpenFolder(AbsolutePath path)
        {
            Process.Start(new ProcessStartInfo(KnownFolders.Windows.Combine("explorer.exe").ToString(), path.ToString())
            {
                CreateNoWindow = true,
            });
        }


        public static AbsolutePath OpenFileDialog(string filter, string initialDirectory = null)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = filter;
            ofd.InitialDirectory = initialDirectory;
            if (ofd.ShowDialog() == DialogResult.OK)
                return (AbsolutePath)ofd.FileName;
            return default;
        }

        public static IObservable<BitmapImage> DownloadBitmapImage(this IObservable<string> obs, Action<Exception> exceptionHandler,
            LoadingLock loadingLock)
        {
            return obs
                .ObserveOn(RxApp.TaskpoolScheduler)
                .SelectTask(async url =>
                {
                    var ll = loadingLock.WithLoading();
                    try
                    {
                        var (found, mstream) = await FindCachedImage(url);
                        if (found) return (ll, mstream, url);
                        
                        var ret = new MemoryStream();
                        using (var client = new HttpClient())
                        await using (var stream = await client.GetStreamAsync(url))
                        {
                            await stream.CopyToAsync(ret);
                        }

                        ret.Seek(0, SeekOrigin.Begin);

                        await WriteCachedImage(url, ret.ToArray());
                        return (ll, ret, url);
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler(ex);
                        return (ll, default, url);
                    }
                })
                .Select(x =>
                {
                    var (ll, memStream, url) = x;
                    if (memStream == null) return default;
                    try
                    {
                        // System.Windows.Media.Imaging does not include WebP support by default, it falls back onto Windows Imaging Components (WIC) if it's a format that's not supported.
                        // Only the latest Windows versions seem to include a new version of WIC that has WebP support, so fallback on libwebp to support all Windows installations
                        // Also the Nexus image CDN has files ending with PNG/JPEG but they're actually encoded as WebP, so use this method for Nexus aswell
                        bool isWebp = url.EndsWith("webp", StringComparison.InvariantCultureIgnoreCase) || url.Contains("staticdelivery.nexusmods.com");
                        try
                        {
                            return BitmapImageFromStream(memStream);
                        }
                        catch(NotSupportedException)
                        {
                            if (isWebp)
                                return BitmapImageFromWebp(memStream.ToArray());
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        exceptionHandler(ex);
                        return default;
                    }
                    finally
                    {
                        ll.Dispose();
                        memStream.Dispose();
                    }
                })
                .ObserveOnGuiThread();
        }

        private static async Task WriteCachedImage(string url, byte[] data)
        {
            var folder = KnownFolders.WabbajackAppLocal.Combine("ModListImages");
            if (!folder.DirectoryExists()) folder.CreateDirectory();
            
            var path = folder.Combine((await Encoding.UTF8.GetBytes(url).Hash()).ToHex());
            await path.WriteAllBytesAsync(data);
        }

        private static async Task<(bool Found, MemoryStream data)> FindCachedImage(string uri)
        {
            var folder = KnownFolders.WabbajackAppLocal.Combine("ModListImages");
            if (!folder.DirectoryExists()) folder.CreateDirectory();
            
            var path = folder.Combine((await Encoding.UTF8.GetBytes(uri).Hash()).ToHex());
            return path.FileExists() ? (true, new MemoryStream(await path.ReadAllBytesAsync())) : (false, default);
        }

        /// <summary>
        /// Format bytes to a greater unit
        /// </summary>
        /// <param name="bytes">number of bytes</param>
        /// <returns></returns>
        public static string FormatBytes(long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        public static void OpenFile(AbsolutePath file)
        {
            Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"\" \"{file}\"")
            {
                CreateNoWindow = true,
            });
        }
    }
}
