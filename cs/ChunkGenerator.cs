using Godot;
using System.Collections.Generic;

public partial class ChunkGenerator : Node3D
{
	public static readonly int CHUNK_WIDTH = 16;
	public static readonly int CHUNK_HEIGHT = 128;
	public static readonly int CHUNK_DEPTH = 16;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// generate chunk data
		var chunkData = new int[CHUNK_WIDTH,CHUNK_HEIGHT,CHUNK_DEPTH];

		for (int x = 0; x < CHUNK_WIDTH; x++)
		{
			for (int y = 0; y < CHUNK_HEIGHT; y++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					chunkData[x,y,z] = 1;
				}
			}
		}

		MeshChunk(chunkData);
	}

	// Return true if a given block id is transparent
	private static bool IsTransparent(int blockId)
	{
		return blockId == 0;
	}

	private static int GetBlock(int[,,] chunkData, int x, int y, int z)
	{
		// if coordinates are out of bounds, return air block
		if (x < 0 || y < 0 || z < 0 || x >= CHUNK_WIDTH || y >= CHUNK_HEIGHT || z >= CHUNK_DEPTH)
			return 0;

		return chunkData[x,y,z];
	}

	private void MeshChunk(int[,,] chunkData)
	{
		var surfaceArray = new Godot.Collections.Array();
		surfaceArray.Resize((int)Mesh.ArrayType.Max);

		var verts = new List<Vector3>();
		var uvs = new List<Vector2>();
		var normals = new List<Vector3>();
		var indices = new List<int>();

		// helper function
		static void AddVertexData(List<Vector2> uvs, List<Vector3> normals, List<int> indices, ref int i, Vector3 normal)
		{
			uvs.Add(new Vector2(0, 0));
			uvs.Add(new Vector2(1, 0));
			uvs.Add(new Vector2(1, 1));
			uvs.Add(new Vector2(0, 1));
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			normals.Add(normal);
			indices.Add(i + 0);
			indices.Add(i + 1);
			indices.Add(i + 2);
			indices.Add(i + 0);
			indices.Add(i + 2);
			indices.Add(i + 3);
			i += 4;
		}

		int indicesIndex = 0;

		for (int x = 0; x < CHUNK_WIDTH; x++)
		{
			for (int y = 0; y < CHUNK_HEIGHT; y++)
			{
				for (int z = 0; z < CHUNK_DEPTH; z++)
				{
					Vector3I blockPos = new(x, y, z);

					// top face
					if (IsTransparent(GetBlock(chunkData, x, y+1, z)))
					{
						verts.Add(new Vector3(0, 1, 0) + blockPos);
						verts.Add(new Vector3(1, 1, 0) + blockPos);
						verts.Add(new Vector3(1, 1, 1) + blockPos);
						verts.Add(new Vector3(0, 1, 1) + blockPos);
						AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, 1, 0));
					}

					// bottom face
					if (IsTransparent(GetBlock(chunkData, x, y-1, z)))
					{
						verts.Add(new Vector3(0, 0, 1) + blockPos);
						verts.Add(new Vector3(1, 0, 1) + blockPos);
						verts.Add(new Vector3(1, 0, 0) + blockPos);
						verts.Add(new Vector3(0, 0, 0) + blockPos);
						AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, -1, 0));
					}

					// right face
					if (IsTransparent(GetBlock(chunkData, x+1, y, z)))
					{
						verts.Add(new Vector3(1, 0, 0) + blockPos);
						verts.Add(new Vector3(1, 0, 1) + blockPos);
						verts.Add(new Vector3(1, 1, 1) + blockPos);
						verts.Add(new Vector3(1, 1, 0) + blockPos);
						AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(1, 0, 0));
					}

					// left face
					if (IsTransparent(GetBlock(chunkData, x-1, y, z)))
					{
						verts.Add(new Vector3(0, 1, 0) + blockPos);
						verts.Add(new Vector3(0, 1, 1) + blockPos);
						verts.Add(new Vector3(0, 0, 1) + blockPos);
						verts.Add(new Vector3(0, 0, 0) + blockPos);
						AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(-1, 0, 0));
					}

					// back face
					if (IsTransparent(GetBlock(chunkData, x, y, z-1)))
					{
						verts.Add(new Vector3(0, 0, 0) + blockPos);
						verts.Add(new Vector3(1, 0, 0) + blockPos);
						verts.Add(new Vector3(1, 1, 0) + blockPos);
						verts.Add(new Vector3(0, 1, 0) + blockPos);
						AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, 0, -1));
					}

					// front face
					if (IsTransparent(GetBlock(chunkData, x, y, z+1)))
					{
						verts.Add(new Vector3(0, 1, 1) + blockPos);
						verts.Add(new Vector3(1, 1, 1) + blockPos);
						verts.Add(new Vector3(1, 0, 1) + blockPos);
						verts.Add(new Vector3(0, 0, 1) + blockPos);
						AddVertexData(uvs, normals, indices, ref indicesIndex, new Vector3(0, 0, 1));
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
