using Godot;
using System.Collections;
using System.Collections.Generic;

public partial class ChunkGenerator : Node3D
{
	public static readonly int CHUNK_WIDTH = 16;
	public static readonly int CHUNK_HEIGHT = 128;
	public static readonly int CHUNK_DEPTH = 16;

	private class ChunkData
	{
		private readonly int[] data;

		public ChunkData()
		{
			int arrSize = CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH;
			data = new int[arrSize];

			for (int i = 0; i < arrSize; i++)
				data[i] = 0;
		}

		public int Get(int x, int y, int z)
		{
			// if coordinates are out of bounds, return air block
			if (x < 0 || y < 0 || z < 0 || x >= CHUNK_WIDTH || y >= CHUNK_HEIGHT || z >= CHUNK_DEPTH)
				return 0;
			return data[y * CHUNK_WIDTH*CHUNK_DEPTH + x * CHUNK_WIDTH + z];
		}

		public void Set(int x, int y, int z, int v)
		{
			if (x < 0 || y < 0 || z < 0 || x >= CHUNK_WIDTH || y >= CHUNK_HEIGHT || z >= CHUNK_DEPTH)
				return;
			data[y * CHUNK_WIDTH*CHUNK_DEPTH + x * CHUNK_WIDTH + z] = v;
		}
	}

	private FastNoiseLite noise;
	private readonly Dictionary<uint, ChunkData> chunks = new();
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

	private ChunkData GetChunk(int cx, int cz)
	{
		return chunks[Pair2s(cx, cz)];
	}

	private bool TryGetChunk(int cx, int cz, out ChunkData chunk)
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
			MeshChunk(chunkPos.X, chunkPos.Y);
		}
    }

    private void GenerateChunk(int cx, int cz)
	{
		// if chunk at (cx, cz) already exists, do not regenerate chunk
		if (TryGetChunk(cx, cz, out _)) return;
		var prevTime = Time.GetTicksMsec();

		var chunkData = new ChunkData();

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

    // Return true if a given block id is transparent
    private static bool IsTransparent(int blockId)
	{
		return blockId == 0;
	}

	private void MeshChunk(int cx, int cz)
	{
		var startTime = Time.GetTicksMsec();

		var chunkData = GetChunk(cx, cz);

		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();

		int indicesIndex = 0;

		// helper functions
        void addVertexData(Vector3 normal)
        {
            /*uvs.Add(0); uvs.Add(0);
            uvs.Add(1); uvs.Add(0);
            uvs.Add(1); uvs.Add(1);
            uvs.Add(0); uvs.Add(1);
            normals.Add(normal.X); normals.Add(normal.Y); normals.Add(normal.Z);
            normals.Add(normal.X); normals.Add(normal.Y); normals.Add(normal.Z);
            normals.Add(normal.X); normals.Add(normal.Y); normals.Add(normal.Z);
            normals.Add(normal.X); normals.Add(normal.Y); normals.Add(normal.Z);*/
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(1, 0));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(0, 1));
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
            indices.Add(indicesIndex + 0);
            indices.Add(indicesIndex + 1);
            indices.Add(indicesIndex + 2);
            indices.Add(indicesIndex + 0);
            indices.Add(indicesIndex + 2);
            indices.Add(indicesIndex + 3);
            indicesIndex += 4;
        }

		// get voxels in neighbor chunk
		var neighborChunks = new ChunkData[4];
		TryGetChunk(cx-1, cz, out neighborChunks[0]);
		TryGetChunk(cx+1, cz, out neighborChunks[1]);
		TryGetChunk(cx, cz-1, out neighborChunks[2]);
		TryGetChunk(cx, cz+1, out neighborChunks[3]);
		
		bool isTransparent(int x, int y, int z)
		{
			if (x < 0 && neighborChunks[0] != null)
				return IsTransparent(neighborChunks[0].Get(x + CHUNK_WIDTH, y, z));

			else if (x >= CHUNK_WIDTH && neighborChunks[1] != null)
				return IsTransparent(neighborChunks[1].Get(x - CHUNK_WIDTH, y, z));

			if (z < 0 && neighborChunks[2] != null)
				return IsTransparent(neighborChunks[2].Get(x, y, z + CHUNK_DEPTH));

			else if (z >= CHUNK_DEPTH && neighborChunks[3] != null)
				return IsTransparent(neighborChunks[3].Get(x, y, z - CHUNK_DEPTH));

			return IsTransparent(chunkData.Get(x, y, z));
		};

		// loop through all blocks in the chunk
		// to generate mesh data
        for (int x = 0; x < CHUNK_WIDTH; x++)
		{
			for (int y = 0; y < CHUNK_HEIGHT; y++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					// air is invisible
					if (chunkData.Get(x, y, z) == 0) continue;

					Vector3I blockPos = new(x + cx * CHUNK_WIDTH, y, z + cz * CHUNK_DEPTH);

					// top face
					if (isTransparent(x, y+1, z))
					{
						verts.Add(new Vector3(0, 1, 0) + blockPos);
						verts.Add(new Vector3(1, 1, 0) + blockPos);
						verts.Add(new Vector3(1, 1, 1) + blockPos);
						verts.Add(new Vector3(0, 1, 1) + blockPos);
						addVertexData(new Vector3(0, 1, 0));
					}

					// bottom face
					if (isTransparent(x, y-1, z))
					{
						verts.Add(new Vector3(0, 0, 1) + blockPos);
						verts.Add(new Vector3(1, 0, 1) + blockPos);
						verts.Add(new Vector3(1, 0, 0) + blockPos);
						verts.Add(new Vector3(0, 0, 0) + blockPos);
						addVertexData(new Vector3(0, -1, 0));
					}

					// right face
					if (isTransparent(x+1, y, z))
					{
						verts.Add(new Vector3(1, 0, 0) + blockPos);
						verts.Add(new Vector3(1, 0, 1) + blockPos);
						verts.Add(new Vector3(1, 1, 1) + blockPos);
						verts.Add(new Vector3(1, 1, 0) + blockPos);
						addVertexData(new Vector3(1, 0, 0));
					}

					// left face
					if (isTransparent(x-1, y, z))
					{
						verts.Add(new Vector3(0, 1, 0) + blockPos);
						verts.Add(new Vector3(0, 1, 1) + blockPos);
						verts.Add(new Vector3(0, 0, 1) + blockPos);
						verts.Add(new Vector3(0, 0, 0) + blockPos);
						addVertexData(new Vector3(-1, 0, 0));
					}

					// back face
					if (isTransparent(x, y, z-1))
					{
						verts.Add(new Vector3(0, 0, 0) + blockPos);
						verts.Add(new Vector3(1, 0, 0) + blockPos);
						verts.Add(new Vector3(1, 1, 0) + blockPos);
						verts.Add(new Vector3(0, 1, 0) + blockPos);
						addVertexData(new Vector3(0, 0, -1));
					}

					// front face
					if (isTransparent(x, y, z+1))
					{
						verts.Add(new Vector3(0, 1, 1) + blockPos);
						verts.Add(new Vector3(1, 1, 1) + blockPos);
						verts.Add(new Vector3(1, 0, 1) + blockPos);
						verts.Add(new Vector3(0, 0, 1) + blockPos);
						addVertexData(new Vector3(0, 0, 1));
					}
				}
			}
		}

		// convert lists to arrays and assign to surface array
		GD.Print(verts.Count);

		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		// finalize mesh
		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		GD.Print($"chunk mesh took {Time.GetTicksMsec() - startTime} ms");

		var material = new StandardMaterial3D() {
			AlbedoTexture = GD.Load("res://stone.png") as Texture2D,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled
		};

		var meshInstance = new MeshInstance3D() {
			Name = "ChunkMesh",
			Mesh = arrayMesh,
			MaterialOverride = material
		};

		AddChild(meshInstance);
	}
}
