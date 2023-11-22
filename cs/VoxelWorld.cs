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

		class BlockDef {
			public string texture { get; set; }
			public string sideTexture { get; set; }
			public string topTexture { get; set; }
			public string bottomTexture { get; set; }
		};

		private string[] intIdToString;
		private Dictionary<string, uint> stringIdToInt = new();
		private Texture2DArray textureArray;

		public Texture2DArray Texture2DArray { get => textureArray; }

		// parse block definitions
		public VoxelWorld()
		{
			var file = FileAccess.Open("res://blockdefs.json", FileAccess.ModeFlags.Read)
				?? throw new Exception("Block definitions file is missing");
			
            var jsonData = JsonSerializer.Deserialize<Dictionary<string, BlockDef>>(file.GetAsText(true));

			// var texArray = new Texture2DArray();
			
			var images = new List<Image>();
			var ids = new List<string>();

			ids.Add("air");
			stringIdToInt.Add("air", 0);

			uint curId = 1;
			foreach (var entry in jsonData)
			{
				var texturePath = entry.Value.texture ?? entry.Value.sideTexture;
				//var image = Image.LoadFromFile(texturePath);
				var image = GD.Load(texturePath) as Image;
				images.Add(image);

				var format = image.GetFormat();
				GD.Print($"{texturePath} has format {format}");

				ids.Add(entry.Key);
				stringIdToInt.Add(entry.Key, curId);
				curId++;
            }

			textureArray = new Texture2DArray();
			textureArray.CreateFromImages(new Godot.Collections.Array<Image>(images));

			GD.Print($"created array texture with {images.Count} images");

			intIdToString = ids.ToArray();
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

			for (int x = 0; x < CHUNK_WIDTH; x++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					int height = (int)(noise.GetNoise2D(x + cx * CHUNK_WIDTH, z + cz * CHUNK_DEPTH) * 20) + 64;

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
