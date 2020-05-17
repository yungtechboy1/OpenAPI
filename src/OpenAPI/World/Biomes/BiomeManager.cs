﻿using System.Collections.Generic;
using MiNET.Utils;
using MiNET.Worlds;
using OpenAPI.World.Biomes;
using OpenAPI.World.Populator;

namespace OpenAPI.World
{
    public class BiomeManager
    {
        public static List<AdvancedBiome> Biomes = new List<AdvancedBiome>();

        private static int N;
        private static readonly Dictionary<int, AdvancedBiome> BiomeDict = new Dictionary<int, AdvancedBiome>();

        public BiomeManager()
        {
            // AddBiome(new MainBiome());
            AddBiome(new ForestBiome());
            AddBiome(new SnowyIcyChunk());
            AddBiome(new Desert());
            AddBiome(new Mountains());
            AddBiome(new Plains());
            AddBiome(new HighPlains());
            AddBiome(new WaterBiome());
            AddBiome(new ForestBiome());
            AddBiome(new SnowForest());
            AddBiome(new SnowTundra());
            AddBiome(new SnowyIcyChunk());
            AddBiome(new TropicalRainForest());
            AddBiome(new TropicalSeasonalForest());
        }

        public static void AddBiome(AdvancedBiome biome)
        {
            biome.BorderChunk = false;
            Biomes.Add(biome);
            biome.LocalID = N;
            BiomeDict[N] = biome;
            N++;
        }

        public static AdvancedBiome GetBiome(int name)
        {
            foreach (var ab in Biomes)
                if (ab.LocalID == name)
                    return ab;

            return new SnowyIcyChunk();
        }

        public static AdvancedBiome GetBiome(string name)
        {
            foreach (var ab in Biomes)
                if (ab.name == name)
                    return ab;

            return new MainBiome();
        }
        
        
         public static float[] getChunkRTH(ChunkColumn chunk)
        {
            //CALCULATE RAIN
            var rainnoise = new FastNoise(123123);
            rainnoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            rainnoise.SetFrequency(.007f); //.015
            rainnoise.SetFractalType(FastNoise.FractalType.FBM);
            rainnoise.SetFractalOctaves(1);
            rainnoise.SetFractalLacunarity(.25f);
            rainnoise.SetFractalGain(1);
            //CALCULATE TEMP
            var tempnoise = new FastNoise(123123 + 1);
            tempnoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
            tempnoise.SetFrequency(.004f); //.015f
            tempnoise.SetFractalType(FastNoise.FractalType.FBM);
            tempnoise.SetFractalOctaves(1);
            tempnoise.SetFractalLacunarity(.25f);
            tempnoise.SetFractalGain(1);
            
            float rain = rainnoise.GetNoise(chunk.X, chunk.Z) + 1;
            float temp = tempnoise.GetNoise(chunk.X, chunk.Z) + 1;
            float height = GetNoise(chunk.X, chunk.Z, 0.015f,2);;
            return new []{rain, temp, height};
        }

         private static readonly OpenSimplexNoise OpenNoise = new OpenSimplexNoise("a-seed".GetHashCode());


         public static float GetNoise(int x, int z, float scale, int max)
         {//CALCULATE HEIGHT
             var heightnoise = new FastNoise(123123 + 2);
             heightnoise.SetNoiseType(FastNoise.NoiseType.SimplexFractal);
             heightnoise.SetFrequency(scale);
             heightnoise.SetFractalType(FastNoise.FractalType.FBM);
             heightnoise.SetFractalOctaves(1);
             heightnoise.SetFractalLacunarity(2);
             heightnoise.SetFractalGain(.5f);
             return (heightnoise.GetNoise(x, z)+1 )*(max/2f);
             return (float)(OpenNoise.Evaluate(x * scale, z * scale) + 1f) * (max / 2f);
         }
         
        //CHECKED 5/10 @ 5:23 And this works fine!
        public static AdvancedBiome GetBiome(ChunkColumn chunk)
        {
            var rth = getChunkRTH(new ChunkColumn()
                    {
                        X = chunk.X,
                        Z = chunk.Z
                    });
            foreach (var biome in Biomes)
                if (biome.check(rth))
                {
                    //CHEKC IF BOREDR CHUNK
                    //Top
                    var tb = BiomeManager.GetBiome2(getChunkRTH(new ChunkColumn()
                    {
                        X = chunk.X,
                        Z = chunk.Z+1
                    }));
                    var rb = BiomeManager.GetBiome2(getChunkRTH(new ChunkColumn()
                    {
                        X = chunk.X+1,
                        Z = chunk.Z
                    }));
                    var bb = BiomeManager.GetBiome2(getChunkRTH(new ChunkColumn()
                    {
                        X = chunk.X,
                        Z = chunk.Z-1
                    }));
                    var lb = BiomeManager.GetBiome2(getChunkRTH(new ChunkColumn()
                    {
                        X = chunk.X-1,
                        Z = chunk.Z
                    }));
                    if (tb.LocalID != biome.LocalID&& !tb.BorderChunk)
                    {
                        biome.BorderChunk = true;
                    }else if(rb.LocalID != biome.LocalID&& !rb.BorderChunk)
                    {
                        biome.BorderChunk = true;
                    }else if(bb.LocalID != biome.LocalID&& !bb.BorderChunk)
                    {
                        biome.BorderChunk = true;
                    }else if(lb.LocalID != biome.LocalID && !lb.BorderChunk)
                    {
                        biome.BorderChunk = true;
                    }
                    else
                    {
                        biome.BorderChunk = false;
                    }
                    
                    return biome;
                }

            // return new MainBiome();
            return new WaterBiome();
            // return new HighPlains();
        }

        public static AdvancedBiome GetBiome2(float[] rth)
        {
            foreach (var ab in Biomes)
                if (ab.check(rth))
                    return ab;

            // return new MainBiome();
            return new WaterBiome();
            // return new HighPlains();
        }
    }
}