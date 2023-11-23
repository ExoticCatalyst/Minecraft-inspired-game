using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Voxel
{
	public class ChunkData
	{
		public const int SIZE_X = 16;
		public const int SIZE_Y = 128;
		public const int SIZE_Z = 16;

		private readonly uint[] data;

		public ChunkData()
		{
			int arrSize = SIZE_X * SIZE_Y * SIZE_Z;
			data = new uint[arrSize];

			for (int i = 0; i < arrSize; i++)
				data[i] = 0;
		}

		public bool IsInBounds(int x, int y, int z)
		{
			return !(x < 0 || y < 0 || z < 0 || x >= SIZE_X || y >= SIZE_Y || z >= SIZE_Z);
		}

		public uint Get(int x, int y, int z)
		{
			// if coordinates are out of bounds, return air block
			if (x < 0 || y < 0 || z < 0 || x >= SIZE_X || y >= SIZE_Y || z >= SIZE_Z)
				return 0;
			return data[y * SIZE_X*SIZE_Z + x * SIZE_X + z];
		}

		public void Set(int x, int y, int z, uint v)
		{
			if (x < 0 || y < 0 || z < 0 || x >= SIZE_X || y >= SIZE_Y || z >= SIZE_Z)
				return;
			data[y * SIZE_X*SIZE_Z + x * SIZE_X + z] = v;
		}
	}

	public partial class VoxelWorld : Node3D
	{
		private FastNoiseLite noise;
		private readonly Dictionary<uint, ChunkData> chunks = new();
		private readonly Dictionary<uint, ChunkMesh> chunkMeshes = new();
		private readonly Queue<Vector2I> chunkUpdateQueue = new();

		private static uint Pair2u(uint x, uint y) => 
			x >= y ? x*x + x + y : y*y + x;

		private static uint Pair2s(int x, int y) =>
			Pair2u(
				x >= 0 ? (uint)(x*2) : (uint)(-x*2+1),
				y >= 0 ? (uint)(y*2) : (uint)(-y*2+1)
			);

		private void AddChunk(int cx, int cz, ChunkData chunk)
		{
			chunks[Pair2s(cx, cz)] = chunk;
		}

		public ChunkData GetChunk(int cx, int cz)
		{
			return chunks[Pair2s(cx, cz)];
		}

		public bool TryGetChunk(int cx, int cz, out ChunkData chunk)
		{
			return chunks.TryGetValue(Pair2s(cx, cz), out chunk);
		}

		private void QueueChunkUpdate(int cx, int cz)
		{
			chunkUpdateQueue.Enqueue(new Vector2I(cx, cz));
		}		

		class JsonBlockDef {
			public string texture { get; set; }
			public string sideTexture { get; set; }
			public string topTexture { get; set; }
			public string bottomTexture { get; set; }
			public bool transparent { get; set; }
		};

		public struct BlockTextureDef {
			public uint sideIndex;
			public uint topIndex;
			public uint bottomIndex;
			public bool transparent;
		}

		private string[] intIdToString;
		private Dictionary<string, uint> stringIdToInt = new();

		// these are for chunk meshing
		private Texture2DArray textureArray;
		public Texture2DArray Texture2DArray { get => textureArray; }
		private BlockTextureDef[] blockTexDefs;
		public BlockTextureDef[] BlockTextureData { get => blockTexDefs; }

		// parse block definitions
		public VoxelWorld()
		{
			var file = FileAccess.Open("res://blockdefs.json", FileAccess.ModeFlags.Read)
				?? throw new Exception("Block definitions file is missing");
			
            var jsonData = JsonSerializer.Deserialize<Dictionary<string, JsonBlockDef>>(file.GetAsText(true));

			var images = new List<Image>();
			var ids = new List<string>();
			var texDefs = new List<BlockTextureDef>();

			ids.Add("air");
			stringIdToInt.Add("air", 0);

			uint nextBlockId = 1;
			uint nextTexId = 0;

			uint registerImage(string texturePath)
			{
				var image = GD.Load(texturePath) as Image;

				// format and size validation
				if (image.GetSize() != new Vector2I(32, 32))
				{
					throw new Exception($"{texturePath} is not a 32x32 image");
				}

				var format = image.GetFormat();
				if (format != Image.Format.Rgba8)
				{
					throw new Exception($"{texturePath} has invalid format {format}, expected Rgba8");
				}
				
				images.Add(image);
				return nextTexId++;
			}

			foreach (var entry in jsonData)
			{
				// register block
				ids.Add(entry.Key);
				stringIdToInt.Add(entry.Key, nextBlockId++);

				// register textures
				BlockTextureDef texDef;
				texDef.transparent = entry.Value.transparent;

				// if block has only one texture
				if (entry.Value.texture != null)
				{
					var texID = registerImage(entry.Value.texture);
					texDef.topIndex = texID;
					texDef.sideIndex = texID;
					texDef.bottomIndex = texID;
					
					GD.Print($"{entry.Key}: {texID}");
				}

				// if block has multiple textures
				else
				{
					texDef.topIndex = registerImage(entry.Value.topTexture);
					texDef.sideIndex = registerImage(entry.Value.sideTexture);
					texDef.bottomIndex = registerImage(entry.Value.bottomTexture);
				}

				texDefs.Add(texDef);
            }

			textureArray = new Texture2DArray();
			textureArray.CreateFromImages(new Godot.Collections.Array<Image>(images));

			GD.Print($"created array texture with {images.Count} images");

			intIdToString = ids.ToArray();
			blockTexDefs = texDefs.ToArray();
        }

		// Called when the node enters the scene tree for the first time.
		public override void _Ready()
		{
			noise = new() {
				NoiseType = FastNoiseLite.NoiseTypeEnum.Simplex,
			};

			for (int x = 0; x < 10; x++)
			{
				for (int z = 0; z < 10; z++)
				{
					GenerateChunk(x, z);
				}
			}

			for (int x = 0; x < 10; x++)
			{
				for (int z = 0; z < 10; z++)
				{
					chunkUpdateQueue.Enqueue(new Vector2I(x, z));
				}
			}
		}

		public override void _Process(double delta)
		{
			base._Process(delta);

			// process up to 8 chunk updates
			for (int i = 0; i < 8; i++)
			{
				if (chunkUpdateQueue.TryDequeue(out Vector2I chunkPos))
				{
					uint pair = Pair2s(chunkPos.X, chunkPos.Y);
					if (chunkMeshes.TryGetValue(pair, out ChunkMesh mesh))
					{
						mesh.GenerateMesh();
					}
					else
					{
						mesh = new ChunkMesh(this, chunkPos.X, chunkPos.Y);
						AddChild(mesh);
						chunkMeshes.Add(pair, mesh);
					}
				}
			}
		}

		public uint GetBlockIndex(string id)
		{
			return stringIdToInt[id];
		}

		public string GetBlockIDFromIndex(uint index)
		{
			return intIdToString[index];
		}

		private void GenerateChunk(int cx, int cz)
		{
			// if chunk at (cx, cz) already exists, do not regenerate chunk
			if (TryGetChunk(cx, cz, out _)) return;
			var prevTime = Time.GetTicksMsec();

			var chunkData = new ChunkData();

			const int CHUNK_WIDTH = ChunkData.SIZE_X;
			const int CHUNK_HEIGHT = ChunkData.SIZE_Y;
			const int CHUNK_DEPTH = ChunkData.SIZE_Z;

			var airBlock = GetBlockIndex("air");
			var grassBlock = GetBlockIndex("grass");
			var stoneBlock = GetBlockIndex("stone");
			var dirtBlock = GetBlockIndex("dirt");
			var logBlock = GetBlockIndex("log");
			var leavesBlock = GetBlockIndex("leaves");

			var rng = new RandomNumberGenerator() {
				Seed = Pair2s(cx, cz)
			};

			const int baseHeight = 64;
			const float amplitude = 20.0f;
			const float scale = 2.0f;
			const float treeDensity = 0.01f;

			int heightAt(int x, int z) =>
				(int)(noise.GetNoise2D(scale * (x + cx * CHUNK_WIDTH), scale * (z + cz * CHUNK_DEPTH)) * amplitude) + baseHeight;

			// stage 1: generate base terrain
			for (int x = 0; x < CHUNK_WIDTH; x++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					int height = heightAt(x, z);

					for (int y = 0; y < CHUNK_HEIGHT; y++)
					{
						var block = airBlock;

						if (y < height - 8)
						{
							block = stoneBlock;
						}
						else if (y < height)
						{
							block = dirtBlock;
						}
						else if (y == height)
							block = grassBlock;
						
						chunkData.Set(x, y, z, block);
					}
				}
			}

			// stage 2: generate trees
			for (int x = -5; x < CHUNK_WIDTH + 5; x++)
			{
				for (int z = -5; z < CHUNK_DEPTH + 5; z++)
				{
					int height = heightAt(x, z);

					rng.Seed = Pair2s(x + cx * CHUNK_WIDTH, z + cz * CHUNK_DEPTH);

					// tree chunk
					if (rng.Randf() < treeDensity)
					{
						int trunkHeight = rng.RandiRange(4, 6);

						for (int y = height+1; y <= height+trunkHeight; y++)
						{
							chunkData.Set(x, y, z, logBlock);
						}

						int treeTop = height + trunkHeight + 1;
						chunkData.Set(x, treeTop, z, leavesBlock);

						chunkData.Set(x + 1, treeTop - 1, z, leavesBlock);
						chunkData.Set(x - 1, treeTop - 1, z, leavesBlock);
						chunkData.Set(x, treeTop - 1, z + 1, leavesBlock);
						chunkData.Set(x, treeTop - 1, z - 1, leavesBlock);

						for (int dx = -1; dx <= 1; dx++)
						{
							for (int dz = -1; dz <= 1; dz++)
							{
								if (dx == 0 && dz == 0) continue;
								chunkData.Set(x + dx, treeTop - 2, z + dz, leavesBlock);
							}
						}
					}
				}
			}
			
			AddChunk(cx, cz, chunkData);

			// GD.Print($"chunk gen took {(Time.GetTicksMsec() - prevTime)} ms");
		}

		private static int Mod(int x, int m) {
			return (x%m + m)%m;
		}

		#region GDScript Interfaces

		public int get_block(Vector3I blockPos)
		{
			var chunkX = blockPos.X / ChunkData.SIZE_X;
			var chunkZ = blockPos.Z / ChunkData.SIZE_Z;

			if (TryGetChunk(chunkX, chunkZ, out ChunkData chunk))
			{
				// a chunk does exist at this position
				var localX = Mod(blockPos.X, ChunkData.SIZE_X);
				var localZ = Mod(blockPos.Z, ChunkData.SIZE_X); 

				return (int) chunk.Get(localX, blockPos.Y, localZ);
			}
			else
			{
				// chunk does not exist, return air
				return 0;
			}
		}

		public void set_block(Vector3I blockPos, int blockId)
		{
			var chunkX = blockPos.X / ChunkData.SIZE_X;
			var chunkZ = blockPos.Z / ChunkData.SIZE_Z;

			if (TryGetChunk(chunkX, chunkZ, out ChunkData chunk))
			{
				// a chunk does exist at this position
				var localX = Mod(blockPos.X, ChunkData.SIZE_X);
				var localZ = Mod(blockPos.Z, ChunkData.SIZE_X); 

				chunk.Set(localX, blockPos.Y, localZ, (uint) blockId);
				
				QueueChunkUpdate(chunkX, chunkZ);

				// queue updates on chunk borders
				if (localX == 0 && TryGetChunk(chunkX-1, chunkZ, out _))
					QueueChunkUpdate(chunkX-1, chunkZ);

				if (localZ == 0 && TryGetChunk(chunkX, chunkZ-1, out _))
					QueueChunkUpdate(chunkX, chunkZ-1);

				if (localX == ChunkData.SIZE_X - 1 && TryGetChunk(chunkX+1, chunkZ, out _))
					QueueChunkUpdate(chunkX+1, chunkZ);

				if (localZ == ChunkData.SIZE_Z - 1 && TryGetChunk(chunkX, chunkZ+1, out _))
					QueueChunkUpdate(chunkX, chunkZ+1);
			}
		}

		#endregion
	}
}
