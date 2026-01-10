using Quest_Data_Builder.Config;
using Quest_Data_Builder.Logger;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;


namespace Quest_Data_Builder.TES3.Serializer
{
    internal class MapImageBuilder
    {
        private readonly RecordDataHandler _dataHandler;

        private const int version = 2;

        public int ImageWidth { get; private set; } = 0;
        public int ImageHeight { get; private set; } = 0;

        public int MaxGridX { get; private set; } = 0;
        public int MaxGridY { get; private set; } = 0;
        public int MinGridX { get; private set; } = 0;
        public int MinGridY { get; private set; } = 0;

        public int PixelsPerCell => (int)(64.0 / MainConfig.HeightMapImageDownscaleFactor);


        public MapImageBuilder(RecordDataHandler dataHandler)
        {
            _dataHandler = dataHandler;
        }


        public void BuildImage(string directory)
        {
            CustomLogger.WriteLine(LogLevel.Text, "Building map image");

            MaxGridX = int.MinValue;
            MinGridX = int.MaxValue;
            MaxGridY = int.MinValue;
            MinGridY = int.MaxValue;

            foreach (var land in _dataHandler.Lands.Values)
            {
                MaxGridX = Math.Max(land.GridX, MaxGridX);
                MinGridX = Math.Min(land.GridX, MinGridX);
                MaxGridY = Math.Max(land.GridY, MaxGridY);
                MinGridY = Math.Min(land.GridY, MinGridY);
            }

            MaxGridX += 1;
            MaxGridY += 1;
            MinGridX -= 1;
            MinGridY -= 1;

            int imageWidth = (MaxGridX - MinGridX + 1) * 64;
            int imageHeight = (MaxGridY - MinGridY + 1) * 64;

            using var image = new Image<Rgb24>(imageWidth, imageHeight);
            using var mask = new Image<L8>(imageWidth, imageHeight);

            Rgb24 backgroundColor = GetColorForHeight(-8192);
            for (int y = 0; y < imageHeight; y++)
            {
                for (int x = 0; x < imageWidth; x++)
                {
                    image[x, y] = backgroundColor;
                    mask[x, y] = new L8(0);
                }
            }

            Dictionary<int, Dictionary<int, bool>> populatedBlocks = new();

            foreach (var land in _dataHandler.Lands.Values)
            {
                if (land.Heights is null || (land.DATA & 0x1) == 0) continue;

                int blockX = land.GridX < 0 ? (land.GridX + 1) / 16 - 1 : land.GridX / 16;
                int blockY = land.GridY < 0 ? (land.GridY + 1) / 16 - 1 : land.GridY / 16;
                populatedBlocks.TryAdd(blockX, new());
                populatedBlocks[blockX][blockY] = true;

                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        int imX = 64 * (-MinGridX + land.GridX) + x;
                        int imY = 64 * (-MinGridY + land.GridY) + y;
                        float h = land.Heights[x, y];
                        image[imX, imageHeight - imY - 1] = GetColorForHeight(h);

                        if (h >= 0f)
                            mask[imX, imageHeight - imY - 1] = new L8(255);
                    }
                }
            }

            imageHeight = PixelsPerCell * (MaxGridY - MinGridY + 1);
            imageWidth = PixelsPerCell * (MaxGridX - MinGridX + 1);

            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(imageWidth, imageHeight),
                Sampler = KnownResamplers.Triangle,
                Mode = ResizeMode.Max
            }));

            if (MainConfig.HeightMapImageDrawOutline)
            {
                mask.Mutate(ctx => ctx.Resize(new ResizeOptions
                {
                    Size = new Size(imageWidth, imageHeight),
                    Sampler = KnownResamplers.NearestNeighbor,
                    Mode = ResizeMode.Max
                }));


                int stroke = Math.Max(1, MainConfig.HeightMapImageOutlineThickness);
                var border = new bool[imageWidth * imageHeight];
                for (int y = 0; y < imageHeight; y++)
                {
                    for (int x = 0; x < imageWidth; x++)
                    {
                        if (mask[x, y].PackedValue == 0) continue;
                        bool isBorder = false;
                        if (x == 0 || mask[x - 1, y].PackedValue == 0) isBorder = true;
                        else if (x == imageWidth - 1 || mask[x + 1, y].PackedValue == 0) isBorder = true;
                        else if (y == 0 || mask[x, y - 1].PackedValue == 0) isBorder = true;
                        else if (y == imageHeight - 1 || mask[x, y + 1].PackedValue == 0) isBorder = true;

                        border[y * imageWidth + x] = isBorder;
                    }
                }

                Rgb24 borderColor = GetColorForHeight(16384);
                for (int y = 0; y < imageHeight; y++)
                {
                    for (int x = 0; x < imageWidth; x++)
                    {
                        if (!border[y * imageWidth + x]) continue;

                        int minX = Math.Max(0, x - stroke + 1);
                        int maxX = Math.Min(imageWidth - 1, x + stroke - 1);
                        int minY = Math.Max(0, y - stroke + 1);
                        int maxY = Math.Min(imageHeight - 1, y + stroke - 1);

                        for (int yy = minY; yy <= maxY; yy++)
                        {
                            for (int xx = minX; xx <= maxX; xx++)
                            {
                                image[xx, yy] = borderColor;
                            }
                        }
                    }
                }
            }

            ImageWidth = imageWidth;
            ImageHeight = imageHeight;

            CustomLogger.WriteLine(LogLevel.Text, $"Saving map image to {directory}");

            foreach (var blockX in populatedBlocks.Keys)
            {
                foreach (var blockY in populatedBlocks[blockX].Keys)
                {
                    int startX = (blockX * 16 - MinGridX) * PixelsPerCell;
                    int startY = (blockY * 16 - MinGridY) * PixelsPerCell;
                    int endX = startX + 16 * PixelsPerCell - 1;
                    int endY = startY + 16 * PixelsPerCell - 1;
                    
                    int blockWidth = endX - startX + 1;
                    int blockHeight = endY - startY + 1;

                    int topStartY = image.Height - startY - blockHeight;
                    var cropRect = new Rectangle(startX, topStartY, blockWidth, blockHeight);
                    var bounds = new Rectangle(0, 0, image.Width, image.Height);
                    var intersection = Rectangle.Intersect(cropRect, bounds);

                    using Image<Rgb24> blockImage = new Image<Rgb24>(blockWidth, blockHeight);
                    for (int x = 0; x < blockWidth; x++)
                    {
                        for (int y = 0; y < blockHeight; y++)
                        {
                            blockImage[x, y] = backgroundColor;
                        }
                    }

                    if (intersection.Width > 0 && intersection.Height > 0)
                    {
                        using var sourceCrop = image.Clone(ctx => ctx.Crop(intersection));
                        int destX = intersection.X - startX;
                        int destY = intersection.Y - topStartY;
                        blockImage.Mutate(ctx => ctx.DrawImage(sourceCrop, new Point(destX, destY), 1f));
                    }

                    blockImage.SaveAsPng(Path.Combine(directory, $"({blockX},{blockY}).png"));
                }
            }
        }


        public string GenerateInfo(SerializerType format = SerializerType.Json)
        {
            CustomSerializer serializer = new CustomSerializer(format);
            var table = serializer.NewTable();

            table.Add("version", version);
            table.Add("time", (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds);
            table.Add("width", ImageWidth);
            table.Add("height", ImageHeight);
            table.Add("pixelsPerCell", (int)PixelsPerCell);

            var gridXTable = serializer.NewTable();
            var gridYTable = serializer.NewTable();
            
            gridXTable.Add("max", MaxGridX);
            gridXTable.Add("min", MinGridX);
            gridYTable.Add("max", MaxGridY);
            gridYTable.Add("min", MinGridY);

            table.Add("gridX", gridXTable);
            table.Add("gridY", gridYTable);

            var colorArr = serializer.NewArray();
            var col = GetColorForHeight(-8192).ToScaledVector4();
            colorArr.Add(col.X);
            colorArr.Add(col.Y);
            colorArr.Add(col.Z);

            table.Add("bColor", colorArr);

            return serializer.GetResult(table);
        }


        private static Rgb24 GetColorForHeight(float height)
        {
            float r, g, b;
            float heightData = height / 8;
            float clippedData = heightData / 2048;
            // I don't know why, but negative heights need to be multiplied by 8 to match the game colors
            clippedData = Math.Clamp(heightData < 0f ? clippedData * 8f : clippedData, -1.0f, 1.0f);

            if (heightData >= 0.0f)
            {
                if (clippedData > 0.3f)
                {
                    float bs = (clippedData - 0.3f) * 1.428f;
                    r = 34.0f - bs * 29.0f;
                    g = 25.0f - bs * 20.0f;
                    b = 17.0f - bs * 12.0f;
                }
                else
                {
                    float bs = (clippedData > 0.1f) ? clippedData - 0.1f + 0.8f : clippedData * 8.0f;
                    r = 66.0f - bs * 32.0f;
                    g = 48.0f - bs * 23.0f;
                    b = 33.0f - bs * 16.0f;
                }
            }
            else
            {
                r = 38.0f + clippedData * 14.0f;
                g = 56.0f + clippedData * 20.0f;
                b = 51.0f + clippedData * 18.0f;
            }

            return new Rgb24((byte)r, (byte)g, (byte)b);
        }
    }
}
