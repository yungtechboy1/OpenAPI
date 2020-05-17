using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using MiNET.Blocks;
using MiNET.Utils;
using MiNET.Worlds;

namespace OpenAPI.World
{
    public class BiomeQualifications
    {
        public int baseheight = 70;

        public int heightvariation;
        public float startheight; //0-2
        public float startrain; //0 - 1
        public float starttemp; //0 - 2
        public float stopheight;
        public float stoprain;
        public float stoptemp;

        // public int baseheight = 20;
        public bool waterbiome = false;


        public BiomeQualifications(float startrain, float stoprain, float starttemp, float stoptemp, float startheight,
            float stopheight, int heightvariation, bool waterbiome = false)
        {
            this.startrain = startrain;
            this.stoprain = stoprain;
            this.starttemp = starttemp;
            this.stoptemp = stoptemp;
            this.startheight = startheight;
            this.stopheight = stopheight;
            this.waterbiome = waterbiome;
            this.heightvariation = heightvariation;
        }


        public bool check(float[] rth)
        {
            var rain = rth[0];
            var temp = rth[1];
            var height = rth[2];
            return startrain <= rain && stoprain >= rain && starttemp <= temp && stoptemp >= temp &&
                   startheight <= height && stopheight >= height;
        }
    }

    public abstract class AdvancedBiome
    {
       

        public static int max;
        public BiomeQualifications BiomeQualifications;
        /// <summary>
        /// </summary>
        public bool BorderChunk = false;

        public FastNoise HeightNoise = new FastNoise(121212);

        public int LocalID = -1;
        public string name;

        private readonly List<string> Ran = new List<string>();
        public Random RNDM = new Random();
        public int startheight = 80;

        public AdvancedBiome(string name, BiomeQualifications bq)
        {
            BiomeQualifications = bq;
            HeightNoise.SetGradientPerturbAmp(3);
            HeightNoise.SetFrequency(.24f);
            HeightNoise.SetNoiseType(FastNoise.NoiseType.CubicFractal);
            HeightNoise.SetFractalOctaves(2);
            HeightNoise.SetFractalLacunarity(.35f);
            HeightNoise.SetFractalGain(1);
            this.name = name;
        }


        public bool check(float[] rth)
        {
            return BiomeQualifications.check(rth);
        }

        public async Task<ChunkColumn> preSmooth(OpenExperimentalWorldProvider openExperimentalWorldProvider,
            ChunkColumn chunk,
            float[] rth)
        {
            var t = new Stopwatch();
            t.Start();
            SmoothChunk(openExperimentalWorldProvider, chunk, rth);
            t.Stop();

            return chunk;
            // Console.WriteLine($"CHUNK SMOOTHING OF {chunk.X} {chunk.Z} TOOK {t.Elapsed}");
        }

        public async Task<ChunkColumn> prePopulate(OpenExperimentalWorldProvider openExperimentalWorldProvider,
            ChunkColumn chunk,
            float[] rth)
        {
            var t = new Stopwatch();
            t.Start();
            PopulateChunk(openExperimentalWorldProvider, chunk, rth);
            t.Stop();
            Console.WriteLine($"CHUNK POPULATION OF {chunk.X} {chunk.Z} TOOK {t.Elapsed}");
            return chunk;
        }

        /// <summary>
        ///     Populate Chunk from Biome
        /// </summary>
        /// <param name="openExperimentalWorldProvider"></param>
        /// <param name="c"></param>
        /// <param name="rth"></param>
        public abstract void PopulateChunk(OpenExperimentalWorldProvider openExperimentalWorldProvider,
            ChunkColumn c,
            float[] rth);

        public void SetHeightMapToChunks(ChunkColumn c, ChunkColumn[] ca, int[,] map)
        {
            for (var x = 0; x < map.GetLength(0); x++)
            for (var z = 0; z < map.GetLength(1); z++)
            {
                var cnx = (int) Math.Floor(x / 16f);
                var cnz = (int) Math.Floor(z / 16f);
                var cn = cnx + cnz * 3;
                ChunkColumn cc;
                if (cn == 4) cc = c;
                else
                {
                    if (cn > 4) cn--;
                    cc = ca[cn];
                }

                var rx = x % 16;
                var rz = z % 16;
                var h = map[x, z];
                var rzz = 15 - rz;
                var rxx = rx;
                for (var y = 1; y < 255; y++)
                    if (y < h - 1)
                    {
                        if (cc.GetBlockId(rxx, y, rzz) == 0) cc.SetBlock(rxx, y, rzz, new Stone());
                    }
                    else if (y == h - 1)
                    {
                        if (x == 0 || z == map.GetLength(0) - 1 || z == 0 || z == map.GetLength(1) - 1)
                            cc.SetBlock(rxx, y, rzz, new EmeraldBlock());
                        else cc.SetBlock(rxx, y, rzz, new Grass());
                    }
                    else
                    {
                        if (cc.GetBlockId(rxx, y, rzz) == 0) break;
                        cc.SetBlock(rxx, y, rzz, new Air());
                    }

                cc.SetHeight(rxx, rzz, (short) h);
            }
        }

        public int[,] CreateMapFrom8Chunks(ChunkColumn c, ChunkColumn[] ca)
        {
            var map = new int[16 * 3, 16 * 3];
            for (var z = 0; z < map.GetLength(1); z++)
            for (var x = 0; x < map.GetLength(0); x++)
            {
                var rx = x % 16;
                var rz = 15 - z % 16;
                var cnx = (int) Math.Floor(x / 16f);
                var cnz = (int) Math.Floor(z / 16f);
                var cn = cnx + cnz * 3;
                ChunkColumn cc;
                if (cn == 4)
                {
                    cc = c;
                }
                else
                {
                    if (cn > 4) cn--;
                    cc = ca[cn];
                }

                map[x, z] = cc.GetHeight(rx, rz);
            }


            return map;
        }

        public int[,] SmoothMapV3(int[,] map)
        {
            var newmap = new int[map.GetLength(0), map.GetLength(1)];
            // int[,]  newmap = map;

            //SMooth BORDER
            for (var x = 0; x < map.GetLength(0); x++)
            for (var z = 0; z < map.GetLength(1); z++)
                if (x == 0 || x == map.GetLength(0) - 1 || z == 0 || z == map.GetLength(1) - 1)
                {
                    if ((x == 0 || x == map.GetLength(0) - 1) && (z == 0 || z == map.GetLength(1) - 1)) continue;

                    var lv = -1;
                    var nv = -1;
                    if (z == 0)
                    {
                        lv = map[x - 1, z];
                        nv = map[x + 1, z];
                    }
                    else if (z == map.GetLength(1) - 1)
                    {
                        lv = map[x - 1, z];
                        nv = map[x + 1, z];
                    }
                    else if (x == 0)
                    {
                        lv = map[x, z - 1];
                        nv = map[x, z + 1];
                    }
                    else if (x == map.GetLength(0) - 1)
                    {
                        lv = map[x, z - 1];
                        nv = map[x, z + 1];
                    }

                    var cv = map[x, z];
                    var a = (lv + nv) / 2;
                    var dv = a - cv;
                    if (dv > 1)
                    {
                        if (lv > nv)
                            a = lv - 1;
                        else
                            a = lv + 1;
                    }
                    else if (dv < -1)
                    {
                        if (lv < nv)
                            a = lv + 1;
                        else
                            a = lv - 1;
                    }

                    var fv = a;
                    map[x, z] = fv;
                }

            for (var x = 0; x < map.GetLength(0); x++)
            for (var z = 0; z < map.GetLength(1); z++)
            {
                if (x == 0 || x == map.GetLength(0) - 1 || z == 0 || z == map.GetLength(1) - 1)
                {
                    newmap[x, z] = map[x, z];
                    continue;
                }

                var cv = map[x, z];
                var lvx = map[x - 1, z];
                var lvz = map[x, z - 1];
                var nvx = map[x + 1, z];
                var nvz = map[x, z + 1];
                var lnax = (nvx + lvx) / 2;
                var lnaz = (nvz + lvz + lnax) / 3;
                newmap[x, z] = lnaz;
            }

            return newmap;
        }
        
        public void SmoothChunk(OpenExperimentalWorldProvider o, ChunkColumn chunk, float[] rth)
        {
            //Smooth Biome

            if (BorderChunk && max < 255)
            {
                max++;
                chunk.SetBlock(8, 110, 8, new EmeraldBlock());
                var pos = 0;
                int[,] h = null;
                var i = -1;

                var chunks = new ChunkColumn[8];
                var ab = 0;
                chunks[0] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X - 1, Z = chunk.Z + 1}, false,
                    false);
                chunks[1] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X, Z = chunk.Z + 1}, false,
                    false);
                chunks[2] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X + 1, Z = chunk.Z + 1}, false,
                    false);
                chunks[3] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X - 1, Z = chunk.Z}, false,
                    false);
                chunks[4] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X + 1, Z = chunk.Z}, false,
                    false);
                chunks[5] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X - 1, Z = chunk.Z - 1}, false,
                    false);
                chunks[6] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X, Z = chunk.Z - 1}, false,
                    false);
                chunks[7] = o.GenerateChunkColumn(new ChunkCoordinates {X = chunk.X + 1, Z = chunk.Z - 1}, false,
                    false);

                SetHeightMapToChunks(chunk, chunks, SmoothMapV3(CreateMapFrom8Chunks(chunk, chunks)));
            }
        }
        
        public static AdvancedBiome GetBiome(int biomeId)
        {
            return BiomeManager.GetBiome(biomeId);
        }


        public static float GetNoise(int x, int z, float scale, int max)
        {
            var heightnoise = new FastNoise(123123 + 2);
            heightnoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            heightnoise.SetFrequency(scale);
            heightnoise.SetFractalType(FastNoise.FractalType.FBM);
            heightnoise.SetFractalOctaves(1);
            heightnoise.SetFractalLacunarity(2);
            heightnoise.SetFractalGain(.5f);
            return (heightnoise.GetNoise(x, z) + 1) * (max / 2f);
            // return (float) ((OpenNoise.Evaluate(x * scale, z * scale) + 1f) * (max / 2f));
        }
        
    }
}