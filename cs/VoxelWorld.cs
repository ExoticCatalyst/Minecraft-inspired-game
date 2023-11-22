using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Voxel
{
	public class ChunkData
	{
		public const int SIZE_X = 16;
		public const int SIZE_Y = 128;
		public const int SIZE_Z = 16;

		private readonly int[] data;

		public ChunkData()
		{
			int arrSize = SIZE_X * SIZE_Y * SIZE_Z;
			data = new int[arrSize];

			for (int i = 0; i < arrSize; i++)
				data[i] = 0;
		}

		public int Get(int x, int y, int z)
		{
			// if coordinates are out of bounds, return air block
			if (x < 0 || y < 0 || z < 0 || x >= SIZE_X || y >= SIZE_Y || z >= SIZE_Z)
				return 0;
			return data[y * SIZE_X*SIZE_Z + x * SIZE_X + z];
		}

		public void Set(int x, int y, int z, int v)
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

		private void GenerateChunk(int cx, int cz)
		{
			// if chunk at (cx, cz) already exists, do not regenerate chunk
			if (TryGetChunk(cx, cz, out _)) return;
			var prevTime = Time.GetTicksMsec();

			var chunkData = new ChunkData();

			const int CHUNK_WIDTH = ChunkData.SIZE_X;
			const int CHUNK_HEIGHT = ChunkData.SIZE_Y;
			const int CHUNK_DEPTH = ChunkData.SIZE_Z;

			for (int x = 0; x < CHUNK_WIDTH; x++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					int height = (int)(noise.GetNoise2D(x + cx * CHUNK_WIDTH, z + cz * CHUNK_DEPTH) * 20) + 64;

					for (int y = 0; y < CHUNK_HEIGHT; y++)
					{
						chunkData.Set(x, y, z, y >= height ? 0 : 1);
					}
				}
			}
			
			AddChunk(cx, cz, chunkData);

			GD.Print($"chunk gen took {(Time.GetTicksMsec() - prevTime)} ms");
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

				return chunk.Get(localX, blockPos.Y, localZ);
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

				chunk.Set(localX, blockPos.Y, localZ, blockId);
				chunkUpdateQueue.Enqueue(new Vector2I(chunkX, chunkZ));
			}
		}

		#endregion
	}
}
