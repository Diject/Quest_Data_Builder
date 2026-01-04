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

        private const int version = 1;

        public int ImageWidth { get; private set; } = 0;
        public int ImageHeight { get; private set; } = 0;

        public int MaxGridX { get; private set; } = 0;
        public int MaxGridY { get; private set; } = 0;
        public int MinGridX { get; private set; } = 0;
        public int MinGridY { get; private set; } = 0;

        public int PixelsPerCell => (int)(64.0 / MainConfig.HeightMapImageDownscaleFactor);

        public string? LastFilepath { get; private set; } = null;


        public MapImageBuilder(RecordDataHandler dataHandler)
        {
            _dataHandler = dataHandler;
        }


        public void BuildImage(string filepath)
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

            ImageWidth = (MaxGridX - MinGridX + 1) * 64;
            ImageHeight = (MaxGridY - MinGridY + 1) * 64;

            using var image = new Image<Rgb24>(ImageWidth, ImageHeight);

            Rgb24 defaultColor = GetColorForHeight(-8192);
            for (int y = 0; y < ImageHeight; y++)
                for (int x = 0; x < ImageWidth; x++)
                    image[x, y] = defaultColor;


            foreach (var land in _dataHandler.Lands.Values)
            {
                if (land.Heights is null || (land.DATA & 0x1) == 0) continue;

                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        int imX = 64 * (-MinGridX + land.GridX) + x;
                        int imY = 64 * (-MinGridY + land.GridY) + y;
                        image[imX, ImageHeight - imY - 1] = GetColorForHeight(land.Heights[x, y]);
                    }
                }
            }

            ImageHeight = PixelsPerCell * (MaxGridY - MinGridY + 1);
            ImageWidth = PixelsPerCell * (MaxGridX - MinGridX + 1);

            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new Size(ImageWidth, ImageHeight),
                Sampler = KnownResamplers.Triangle,
                Mode = ResizeMode.Max
            }));

            CustomLogger.WriteLine(LogLevel.Text, $"Saving map image to {filepath}");

            image.SaveAsPng(filepath);

            LastFilepath = filepath;
        }


        public string GenerateInfo(SerializerType format = SerializerType.Json)
        {
            if (LastFilepath is null) return "";

            CustomSerializer serializer = new CustomSerializer(format);
            var table = serializer.NewTable();

            table.Add("version", version);
            table.Add("time", (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds);
            string? fileName = Path.GetFileName(LastFilepath);
            table.Add("file", fileName);
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
