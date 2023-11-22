using Godot;
using System.Collections.Generic;

public partial class ChunkGenerator : Node3D
{
	public static readonly int CHUNK_WIDTH = 16;
	public static readonly int CHUNK_HEIGHT = 128;
	public static readonly int CHUNK_DEPTH = 16;

	private class ChunkData
	{
		private int[] data;

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

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// generate chunk data
		var chunkData = new ChunkData();

		var prevTime = Time.GetTicksMsec();

		for (int x = 0; x < CHUNK_WIDTH; x++)
		{
			for (int y = 0; y < CHUNK_HEIGHT; y++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					chunkData.Set(x, y, z, y < 64 ? 1 : 0);
				}
			}
		}

		GD.Print($"chunk gen took {(Time.GetTicksMsec() - prevTime)} ms");

		MeshChunk(chunkData);
	}

	// Return true if a given block id is transparent
	private static bool IsTransparent(int blockId)
	{
		return blockId == 0;
	}

	private void MeshChunk(ChunkData chunkData)
	{
		var startTime = Time.GetTicksMsec();

		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();

		// helper lambdas
		int indicesIndex = 0;

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

        void addVertex(Vector3 vertex)
        {
			verts.Add(vertex);
            //verts.Add(vertex.X);
            //verts.Add(vertex.Y);
            //verts.Add(vertex.Z);
        }

        for (int x = 0; x < CHUNK_WIDTH; x++)
		{
			for (int y = 0; y < CHUNK_HEIGHT; y++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					// air is invisible
					if (chunkData.Get(x, y, z) == 0) continue;

					Vector3I blockPos = new(x, y, z);

					// top face
					if (IsTransparent(chunkData.Get(x, y+1, z)))
					{
						addVertex(new Vector3(0, 1, 0) + blockPos);
						addVertex(new Vector3(1, 1, 0) + blockPos);
						addVertex(new Vector3(1, 1, 1) + blockPos);
						addVertex(new Vector3(0, 1, 1) + blockPos);
						addVertexData(new Vector3(0, 1, 0));
					}

					// bottom face
					if (IsTransparent(chunkData.Get(x, y-1, z)))
					{
						addVertex(new Vector3(0, 0, 1) + blockPos);
						addVertex(new Vector3(1, 0, 1) + blockPos);
						addVertex(new Vector3(1, 0, 0) + blockPos);
						addVertex(new Vector3(0, 0, 0) + blockPos);
						addVertexData(new Vector3(0, -1, 0));
					}

					// right face
					if (IsTransparent(chunkData.Get(x+1, y, z)))
					{
						addVertex(new Vector3(1, 0, 0) + blockPos);
						addVertex(new Vector3(1, 0, 1) + blockPos);
						addVertex(new Vector3(1, 1, 1) + blockPos);
						addVertex(new Vector3(1, 1, 0) + blockPos);
						addVertexData(new Vector3(1, 0, 0));
					}

					// left face
					if (IsTransparent(chunkData.Get(x-1, y, z)))
					{
						addVertex(new Vector3(0, 1, 0) + blockPos);
						addVertex(new Vector3(0, 1, 1) + blockPos);
						addVertex(new Vector3(0, 0, 1) + blockPos);
						addVertex(new Vector3(0, 0, 0) + blockPos);
						addVertexData(new Vector3(-1, 0, 0));
					}

					// back face
					if (IsTransparent(chunkData.Get(x, y, z-1)))
					{
						addVertex(new Vector3(0, 0, 0) + blockPos);
						addVertex(new Vector3(1, 0, 0) + blockPos);
						addVertex(new Vector3(1, 1, 0) + blockPos);
						addVertex(new Vector3(0, 1, 0) + blockPos);
						addVertexData(new Vector3(0, 0, -1));
					}

					// front face
					if (IsTransparent(chunkData.Get(x, y, z+1)))
					{
						addVertex(new Vector3(0, 1, 1) + blockPos);
						addVertex(new Vector3(1, 1, 1) + blockPos);
						addVertex(new Vector3(1, 0, 1) + blockPos);
						addVertex(new Vector3(0, 0, 1) + blockPos);
						addVertexData(new Vector3(0, 0, 1));
					}
				}
			}
		}

		// convert lists to arrays and assign to surface array
		surfaceArray[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
		surfaceArray[(int)Mesh.ArrayType.TexUV] = uvs.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Normal] = normals.ToArray();
		surfaceArray[(int)Mesh.ArrayType.Index] = indices.ToArray();

		// finalize mesh
		var arrayMesh = new ArrayMesh();
		arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, surfaceArray);
		GD.Print($"chunk gen took {Time.GetTicksMsec() - startTime} ms");

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
