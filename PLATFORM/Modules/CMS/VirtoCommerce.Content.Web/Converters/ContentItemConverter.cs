﻿using Omu.ValueInjecter;
using VirtoCommerce.Content.Web.Models;
using VirtoCommerce.Platform.Core.Asset;

namespace VirtoCommerce.Content.Web.Converters
{
    public static class ContentItemConverter
    {
        public static ContentFolder ToContentModel(this BlobFolder blobFolder)
        {
            var retVal = new ContentFolder();
            retVal.InjectFrom(blobFolder);
            return retVal;
        }

        public static BlobFolder ToBlobModel(this ContentFolder folder)
        {
            var retVal = new BlobFolder();
            retVal.InjectFrom(folder);
            return retVal;
        }

        public static ContentFile ToContentModel(this BlobInfo blobInfo)
        {
            var retVal = new ContentFile();
            retVal.InjectFrom(blobInfo);
            return retVal;
        }

        public static BlobInfo ToBlobModel(this ContentFile file)
        {
            var retVal = new BlobInfo();
            retVal.InjectFrom(file);
            return retVal;
        }
    }
}