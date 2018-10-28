using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web;
using System.Reactive.Linq;
using System.IO;

using ImageMagick;
using SmartFormat;
using NLog;
using Newtonsoft.Json;

using MeepLib;
using MeepLib.MeepLang;
using MeepLib.Messages;

using MeepMagick.Messages;

namespace MeepMagick
{
    /// <summary>
    /// Load an image and calculate basic properties and (optional) PHash
    /// </summary>
    [MeepNamespace(Extensions.PluginNamespace)]
    public class Info : AMessageModule
    {
        /// <summary>
        /// Base64 encoded image, or local path to image file in {Smart.Format}
        /// </summary>
        /// <value></value>
        /// <remarks>If a path, must be a local path. If it doesn't look like
        /// a path, we will try to read it as a Base64 encoded binary.
        /// 
        /// <para>This is ignored for LocalisedResource, BinaryResource and 
        /// IStreamMessages, which already contain either a local path or byte 
        /// stream/array.</para>
        /// 
        /// <para>If all you have is a URL, just use the Localise module 
        /// upstream to download it.</para>
        /// </remarks>
        public string From { get; set; }

        /// <summary>
        /// Calculate the Perceptual Hash of the image
        /// </summary>
        /// <value></value>
        /// <remarks>Calculating the phash is slow, so set this to false if you
        /// don't need it.</remarks>
        public bool IncludePHash { get; set; } = true;

        public override async Task<Message> HandleMessage(Message msg)
        {
            try
            {
                var image = await ImageFromMessage(msg);

                if (image is null)
                    return null;

                // Calculating the PHash can take some time
                return await Task.Run<Message>(() =>
                {
                    var imgMsg = new ImageMessage
                    {
                        DerivedFrom = msg,
                        Name = this.Name,
                        Image = image,
                        PHash = IncludePHash ? image.PerceptualHash() : null
                    };

                    return imgMsg;
                });
            }
            catch (MagickMissingDelegateErrorException dex)
            {
                logger.Debug(dex, "ImageMagick may not be configured for this file format");
                return null;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"{ex.GetType().Name} thrown when calculating image identity: {ex.Message}");
                return null;
            }
        }

        private async Task<MagickImage> ImageFromMessage(Message msg)
        {
            // ToDo: See if we can tuck all this into Message or a generic helper

            var streamable = msg as IStreamMessage;
            if (streamable != null)
                return new MagickImage(await streamable.Stream);

            var binary = msg as BinaryResource;
            if (binary != null)
                return new MagickImage(binary.Bytes);

            var localised = msg as LocalisedResource;
            if (localised != null)
                return new MagickImage(localised.Local);

            var context = new MessageContext(msg, this);
            string sfFrom = Smart.Format(From, context);

            if (Uri.IsWellFormedUriString(sfFrom, UriKind.RelativeOrAbsolute)
               && File.Exists(sfFrom))
                return new MagickImage(sfFrom);

            try
            {
                // Last attempt: see if it works as a Base64 encoded binary
                return MagickImage.FromBase64(sfFrom) as MagickImage;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
